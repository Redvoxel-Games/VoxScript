using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using VoxScript.Runtime;

namespace VoxScript.Tree;

public static class ExpressionMath
{
    public static bool CanSimplify(Expression expression)
    {
        return expression switch
        {
            BinaryExpression binary => binary is { Left: LiteralExpression, Right: LiteralExpression } || CanSimplify(binary.Left) || CanSimplify(binary.Right),
            UnaryExpression unary => CanSimplify(unary.Operand),
            _ => false
        };
    }

    public static VoxValue EvaluateIdentifier(IdentifierExpression identifier, Scope scope, bool secondLast = false)
    {
        var current = scope.GetValue(EvaluateValue(identifier.Path.First(), scope));
        for (var i=1; i<identifier.Path.Count; i++)
        {
            if (i == identifier.Path.Count - 1 && secondLast)
                return current;
            
            var expr = identifier.Path[i];
            
            if (expr is PostfixFunctionExpression postfix)
            {
                // Prev current should be function type
                if (current.Type == VoxValueType.Function)
                {
                    var func = current.Reference as VoxFunctionBase;

                    List<VoxValue> inputs = [];
                    foreach (var inputExpr in postfix.Inputs)
                    {
                        inputs.Add(EvaluateValue(inputExpr, scope));
                    }
                    
                    current = func.Invoke(inputs);
                    continue;
                }
                
                throw new Exception("Attempt to call non-function value.");
            }
            else
            {
                var indexer = EvaluateValue(expr, scope);

                if (current.Type == VoxValueType.String && indexer.Type == VoxValueType.Number)
                {
                    current = current.Value.StringValue[(int)indexer.Value.NumberValue];
                    continue;
                }
                if (current.Type == VoxValueType.Object && indexer.Type != VoxValueType.Null)
                {
                    var obj = current.Reference as ScriptObject;

                    current = obj.GetValue(indexer);
                    continue;
                }
                
                throw new Exception("Attempt to index " + current.Type + " with " + indexer.Type);
            }
        }
        
        return current;
    }

    /// <summary>
    /// Recursive simplification function to avoid dramatic overhead evaluating entire expressions on the fly.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns>Simplified expression, if possible to simplify.</returns>
    public static Expression Simplify(Expression expression)
    {
        if (!CanSimplify(expression)) return expression;

        if (expression is BinaryExpression binary)
        {
            var left = CanSimplify(binary.Left) ? Simplify(binary.Left) : binary.Left;
            var right = CanSimplify(binary.Right) ? Simplify(binary.Right) : binary.Right;

            if (left is LiteralExpression literal1)
            {
                if (right is LiteralExpression literal2)
                {
                    return new LiteralExpression(EvaluateBinary(literal1.Value, literal2.Value, binary.Operator));
                }
            }
        }

        if (expression is UnaryExpression unary)
        {
            var operation = unary.Op;
            var operand = CanSimplify(unary.Operand) ? Simplify(unary.Operand) : unary.Operand;

            return operation switch
            {
                "-" when operand is LiteralExpression { Value.Type: VoxValueType.Number } literal =>
                    new LiteralExpression(literal.Value.Value.NumberValue * -1),
                "!" when operand is LiteralExpression { Value.Type: VoxValueType.Boolean } literal2 =>
                    new LiteralExpression(!literal2.Value.Value.BooleanValue),
                _ => operand
            };
        }
        
        return expression;
    }

    public static VoxValue EvaluateBinary(VoxValue left, VoxValue right, string operation)
    {
        switch(operation) {
            // Math/String operations
            case "+":
            {
                if (left.Type == VoxValueType.String || right.Type == VoxValueType.String)
                {
                    return left.ToString() + right.ToString();
                }
                return (double)left + (double)right;
            }
            case "-": return left - right;
            case "*":
            {
                if (left.Type == VoxValueType.String)
                {
                    return string.Concat(Enumerable.Repeat(left.Value.StringValue, (int)Math.Floor(right)));
                }
                return left * right;
            }
            case "/": return left / right;
            case "^": return Math.Pow(left, right);
                
            // Bool operations
            case "==": return left.Equals(right);
            case "!=": return !left.Equals(right);
            case ">" : return left > right;
            case "<" : return left < right;
            case ">=": return left >= right;
            case "<=": return left <= right;
            case "&&": return left && right;
            case "||": return left || right;
        }
        
        return VoxValue.Null;
    }
    
    public static VoxValue EvaluateValue(Expression expression, Scope scope)
    {
        if (expression is IdentifierExpression identifier)
        {
            return EvaluateIdentifier(identifier, scope);
        }

        if (expression is LiteralExpression literal)
        {
            return literal.Value;
        }

        if (expression is UnaryExpression unary)
        {
            var operation = unary.Op;
            var value = EvaluateValue(unary.Operand, scope);
            
            if (value.Type == VoxValueType.Number && operation == "-") return VoxValue.Number(value.Value.NumberValue * -1);
            if (value.Type == VoxValueType.Boolean && operation == "!") return VoxValue.Boolean(!value.Value.BooleanValue);
        }
        
        if (expression is ObjectExpression objectExpression)
        {
            VoxObject obj = new();

            for (int index = 0; index < objectExpression.Keys.Count; index++)
            {
                var key = objectExpression.Keys[index];
                var value = objectExpression.Values[index];
                obj[EvaluateValue(key, scope)] = EvaluateValue(value, scope);
            }
            
            return obj;
        }
        if (expression is ArrayExpression arrayExpression)
        {
            VoxObject obj = new();

            for (int index = 1; index <= arrayExpression.Values.Count; index++)
            {
                obj[index] = EvaluateValue(arrayExpression.Values[index-1], scope);
            }
            
            return obj;
        }

        if (expression is BinaryExpression)
        {
            var expr = EvaluateExpression(expression, scope);
            return VoxValue.FromObject(expr);
        }
        
        if (expression is CallExpression call)
        {
            if (call.Target is FunctionExpression function)
            {
                return function.Body.Execute(scope);
            }
            if (call.Target is IdentifierExpression id)
            {
                var val = EvaluateIdentifier(id, scope);
                if (val.Type == VoxValueType.Function)
                {
                    var func = (VoxFunctionBase?)val.Reference;
                    if (func == null) return VoxValue.Null;

                    List<VoxValue> inputs = [];

                    foreach (var expr in call.Arguments)
                    {
                        inputs.Add(EvaluateValue(expr, scope));
                    }

                    var returned = func.Invoke(inputs);
                    
                    return returned;
                } else throw new InvalidOperationException($"Attempted to call value of type {val.Type} as a function.");
            }
        }
        
        if (expression is FunctionExpression functionExpression)
        {
            return new VoxFunctionExpr(functionExpression, scope);
        }

        return VoxValue.Null;
    }

    public static object? PerformOp(object left, object right, string operation)
    {
        if (left is VoxValue lv)
        {
            if (lv.Type == VoxValueType.Number)
            {
                left = lv.Value.NumberValue;
            }
            else if (lv.Type == VoxValueType.Boolean)
            {
                left = lv.Value.BooleanValue;
            }
            else if (lv.Type == VoxValueType.String)
            {
                left = lv.Value.StringValue;
            }
            else if (lv.Type == VoxValueType.Object)
            {
                var reference = lv.Reference;
                if (reference is VoxExternalObject externalObject)
                {
                    left = externalObject.ConvertBack();
                }
            }
        }
        if (right is VoxValue rv)
        {
            if (rv.Type == VoxValueType.Number)
            {
                right = rv.Value.NumberValue;
            }
            else if (rv.Type == VoxValueType.Boolean)
            {
                right = rv.Value.BooleanValue;
            }
            else if (rv.Type == VoxValueType.String)
            {
                right = rv.Value.StringValue;
            }
            else if (rv.Type == VoxValueType.Object)
            {
                var reference = rv.Reference;
                if (reference is VoxExternalObject externalObject)
                {
                    right = externalObject.ConvertBack();
                }
            }
        }
        
        var leftType = left.GetType();
        var rightType = right.GetType();
        
        var leftMultOps = leftType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);

        foreach (var op in leftMultOps)
        {
            if (op.Name != operation) continue;
            
            var param = op.GetParameters();
        
            if (param[0].ParameterType != leftType) continue;
            if (param[1].ParameterType != rightType) continue;

            return op.Invoke(null, [left, right]);
        }
        
        
        var rightMultOps = rightType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
        
        foreach (var op in rightMultOps)
        {
            if (op.Name != operation) continue;
            
            var param = op.GetParameters();
        
            if (param[0].ParameterType != leftType) continue;
            if (param[1].ParameterType != rightType) continue;

            return op.Invoke(null, [left, right]);
        }

        return null;
    }
    
    public static object? EvaluateExpression(Expression expression, Scope scope)
    {
        if (expression is IdentifierExpression identifier)
        {
            return EvaluateIdentifier(identifier, scope);
        }
        
        if (expression is LiteralExpression literal)
        {
            return literal.Value;
        }

        if (expression is UnaryExpression unary)
        {
            var operation = unary.Op;
            var operand = EvaluateExpression(unary.Operand, scope) ?? throw new InvalidOperationException();
            switch (operation)
            {
                case "-": return (double)operand * -1;
                case "!": return !(bool)operand;
            }
        }

        if (expression is BinaryExpression binary)
        {
            var operation = binary.Operator;
            var left = EvaluateExpression(binary.Left, scope) ?? throw new InvalidOperationException();
            var right = EvaluateExpression(binary.Right, scope) ?? throw new InvalidOperationException();
            
            if (left is VoxValue { Type: VVT.Null } || right is VoxValue { Type: VVT.Null })
            {
                if (left is not VoxValue { Type: VVT.String })
                {
                    throw new ArithmeticException("Attempt to do math operation on null type");
                }
            }

            switch (operation)
            {
                // Math/String operations
                case "+":
                {
                    if (left is VoxValue { Type: VVT.Object, Reference: VoxExternalObject } leftObj)
                    {
                        VoxExternalObject lo = (VoxExternalObject)leftObj.Reference;
                        
                        VoxValue rightVal = VoxValue.FromObject(right);
                        
                        if (rightVal is { Type: VVT.Object, Reference: VoxExternalObject })
                        {
                            VoxExternalObject ro = (VoxExternalObject)rightVal.Reference;
                            var foundOp = lo.RefType.GetMethod("op_Addition",
                                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                                [lo.RefType, ro.RefType]);
                            
                            return foundOp?.Invoke(null, [lo.ConvertBack(), ro.ConvertBack()]) ?? throw new InvalidOperationException("Failed to multiply native types");
                        }
                        else if (rightVal is { Type: VVT.Number })
                        {
                            var foundOp = lo.RefType.GetMethod("op_Addition",
                                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                                [lo.RefType, typeof(double)]);
                            
                            return foundOp?.Invoke(null, [lo.ConvertBack(), rightVal.Value.NumberValue]) ?? throw new InvalidOperationException("Failed to multiply native types");
                        }
                        else if (rightVal is { Type: VVT.String })
                        {
                            var foundOp = lo.RefType.GetMethod("op_Addition",
                                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                                [lo.RefType, typeof(string)]);
                            
                            return foundOp?.Invoke(null, [lo.ConvertBack(), rightVal.Value.StringValue]) ?? throw new InvalidOperationException("Failed to multiply native types");
                        }
                    }
                    if (left is VoxValue {Type: VoxValueType.Number} v1 && right is VoxValue {Type: VoxValueType.Number} v2)
                    {
                        return v1.Value.NumberValue + v2.Value.NumberValue;
                    }
                    if (left is VoxValue val1 && right is VoxValue val2)
                    {
                        return val1 + val2;
                    }
                    if (left is string str)
                    {
                        if (right is string str2)
                        {
                            return str + str2;
                        }

                        return str + (double)right;
                    }
                    if (right is string str3)
                    {
                        return (double)left + str3;
                    }
                    
                    var opResult = PerformOp(left, right, "op_Addition");
                    if (opResult != null) return opResult;
                    
                    return (double)left + (double)right;
                }
                case "-":
                {
                    if (left is VoxValue { Type: VVT.Object, Reference: VoxExternalObject } leftObj)
                    {
                        VoxExternalObject lo = (VoxExternalObject)leftObj.Reference;
                        
                        VoxValue rightVal = (VoxValue) right;
                        
                        if (rightVal is { Type: VVT.Object, Reference: VoxExternalObject })
                        {
                            VoxExternalObject ro = (VoxExternalObject)rightVal.Reference;
                            var foundOp = lo.RefType.GetMethod("op_Subtraction",
                                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                                [lo.RefType, ro.RefType]);
                            
                            return foundOp?.Invoke(null, [lo.ConvertBack(), ro.ConvertBack()]) ?? throw new InvalidOperationException("Failed to multiply native types");
                        }
                        else if (rightVal is { Type: VVT.Number })
                        {
                            var foundOp = lo.RefType.GetMethod("op_Subtraction",
                                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                                [lo.RefType, typeof(double)]);
                            
                            return foundOp?.Invoke(null, [lo.ConvertBack(), rightVal.Value.NumberValue]) ?? throw new InvalidOperationException("Failed to multiply native types");
                        }
                        else if (rightVal is { Type: VVT.String })
                        {
                            var foundOp = lo.RefType.GetMethod("op_Subtraction",
                                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                                [lo.RefType, typeof(string)]);
                            
                            return foundOp?.Invoke(null, [lo.ConvertBack(), rightVal.Value.StringValue]) ?? throw new InvalidOperationException("Failed to multiply native types");
                        }
                    }
                    if (left is VoxValue {Type: VoxValueType.Number} v1 && right is VoxValue {Type: VoxValueType.Number} v2)
                    {
                        return v1.Value.NumberValue - v2.Value.NumberValue;
                    }
                    var opResult = PerformOp(left, right, "op_Subtraction");
                    if (opResult != null) return opResult;
                    
                    return (double)left - (double)right;
                }
                case "*":
                {
                    if (left is VoxValue { Type: VVT.Object, Reference: VoxExternalObject } leftObj)
                    {
                        VoxExternalObject lo = (VoxExternalObject)leftObj.Reference;
                        
                        VoxValue rightVal = (VoxValue) right;
                        
                        if (rightVal is { Type: VVT.Object, Reference: VoxExternalObject })
                        {
                            VoxExternalObject ro = (VoxExternalObject)rightVal.Reference;
                            var foundOp = lo.RefType.GetMethod("op_Multiply",
                                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                                [lo.RefType, ro.RefType]);
                            
                            return foundOp?.Invoke(null, [lo.ConvertBack(), ro.ConvertBack()]) ?? throw new InvalidOperationException("Failed to multiply native types");
                        }
                        else if (rightVal is { Type: VVT.Number })
                        {
                            var foundOp = lo.RefType.GetMethod("op_Multiply",
                                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                                [lo.RefType, typeof(double)]);
                            
                            return foundOp?.Invoke(null, [lo.ConvertBack(), rightVal.Value.NumberValue]) ?? throw new InvalidOperationException("Failed to multiply native types");
                        }
                        else if (rightVal is { Type: VVT.String })
                        {
                            var foundOp = lo.RefType.GetMethod("op_Multiply",
                                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                                [lo.RefType, typeof(string)]);
                            
                            return foundOp?.Invoke(null, [lo.ConvertBack(), rightVal.Value.StringValue]) ?? throw new InvalidOperationException("Failed to multiply native types");
                        }
                    }
                    if (left is string str)
                    {
                        return string.Concat(Enumerable.Repeat(str, (int)Math.Floor((double)right)));
                    }
                    if (left is VoxValue {Type: VoxValueType.Number} v1 && right is VoxValue {Type: VoxValueType.Number} v2)
                    {
                        return v1.Value.NumberValue * v2.Value.NumberValue;
                    }
                    var opResult = PerformOp(left, right, "op_Multiply");
                    if (opResult != null) return opResult;
                    
                    return (double)left * (double)right;
                }
                case "/":
                {
                    if (left is VoxValue { Type: VVT.Object, Reference: VoxExternalObject } leftObj)
                    {
                        VoxExternalObject lo = (VoxExternalObject)leftObj.Reference;
                        
                        VoxValue rightVal = (VoxValue) right;
                        
                        if (rightVal is { Type: VVT.Object, Reference: VoxExternalObject })
                        {
                            VoxExternalObject ro = (VoxExternalObject)rightVal.Reference;
                            var foundOp = lo.RefType.GetMethod("op_Division",
                                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                                [lo.RefType, ro.RefType]);
                            
                            return foundOp?.Invoke(null, [lo.ConvertBack(), ro.ConvertBack()]) ?? throw new InvalidOperationException("Failed to multiply native types");
                        }
                        else if (rightVal is { Type: VVT.Number })
                        {
                            var foundOp = lo.RefType.GetMethod("op_Division",
                                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                                [lo.RefType, typeof(double)]);
                            
                            return foundOp?.Invoke(null, [lo.ConvertBack(), rightVal.Value.NumberValue]) ?? throw new InvalidOperationException("Failed to multiply native types");
                        }
                        else if (rightVal is { Type: VVT.String })
                        {
                            var foundOp = lo.RefType.GetMethod("op_Division",
                                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                                [lo.RefType, typeof(string)]);
                            
                            return foundOp?.Invoke(null, [lo.ConvertBack(), rightVal.Value.StringValue]) ?? throw new InvalidOperationException("Failed to multiply native types");
                        }
                    }
                    if (left is VoxValue {Type: VoxValueType.Number} v1 && right is VoxValue {Type: VoxValueType.Number} v2)
                    {
                        return v1.Value.NumberValue / v2.Value.NumberValue;
                    }
                    var opResult = PerformOp(left, right, "op_Division");
                    if (opResult != null) return opResult;
                    
                    return (double)left / (double)right;
                }
                case "^":
                {if (left is VoxValue {Type: VoxValueType.Number} v1 && right is VoxValue {Type: VoxValueType.Number} v2)
                    {
                        return Math.Pow(v1.Value.NumberValue, v2.Value.NumberValue);
                    }
                    return Math.Pow((double)left, (double)right);
                }
                
                // Bool operations
                case "==": return left.Equals(right);
                case "!=": return !left.Equals(right);
                case ">":
                {
                    if (left is VoxValue {Type: VoxValueType.Number} v1 && right is VoxValue {Type: VoxValueType.Number} v2)
                    {
                        return v1 > v2;
                    }
                    return (double)left > (double)right;
                }
                case "<":
                {
                    if (left is VoxValue {Type: VoxValueType.Number} v1 && right is VoxValue {Type: VoxValueType.Number} v2)
                    {
                        return v1 < v2;
                    }
                    return (double)left < (double)right;
                }
                case ">=": return (double)left >= (double)right;
                case "<=": return (double)left <= (double)right;
                case "&&": return (bool)left && (bool)right;
                case "||": return (bool)left || (bool)right;
            }
        }

        if (expression is CallExpression call)
        {
            if (call.Target is FunctionExpression function)
            {
                return function.Body.Execute(scope);
            }
            else if (call.Target is IdentifierExpression id)
            {
                var val = EvaluateIdentifier(id, scope);
                if (val.Type == VoxValueType.Function)
                {
                    var func = (VoxFunctionBase?)val.Reference;
                    if (func == null) return null;

                    List<VoxValue> inputs = [];

                    foreach (var expr in call.Arguments)
                    {
                        inputs.Add(EvaluateValue(expr, scope));
                    }
                    
                    return func.Invoke(inputs);
                }
                else
                {
                    throw new Exception($"Attempt to call type {val.Type} as function");
                }
            }
        }

        if (expression is ObjectExpression objectExpression)
        {
            VoxObject obj = new();

            for (int index = 0; index < objectExpression.Keys.Count; index++)
            {
                var key = objectExpression.Keys[index];
                var value = objectExpression.Values[index];
                obj[EvaluateValue(key, scope)] = EvaluateValue(value, scope);
            }
            
            return obj;
        }
        if (expression is ArrayExpression arrayExpression)
        {
            VoxObject obj = new();

            for (int index = 1; index <= arrayExpression.Values.Count; index++)
            {
                obj[index] = EvaluateValue(arrayExpression.Values[index-1], scope);
            }
            
            return obj;
        }

        if (expression is ConditionalExpression conditionalExpression)
        {
            if (EvaluateValue(conditionalExpression.Condition, scope))
            {
                return EvaluateValue(conditionalExpression.Primary, scope);
            }
            else
            {
                return EvaluateValue(conditionalExpression.Secondary, scope);
            }
        }

        if (expression is FunctionExpression functionExpression)
        {
            return new VoxFunctionExpr(functionExpression, scope);
        }

        return null;
    }

    public static bool IsTrue(Expression expression, Scope scope)
    {
        var cond = EvaluateExpression(expression, scope);
        return cond != null && (cond is bool b ? b : true);
    }
}