using System.Reflection;
using System.Runtime.InteropServices.JavaScript;
using VoxScript.Runtime;

namespace VoxScript.Tree;

public abstract class Statement : AstNode
{
    public abstract VoxValue Execute(Scope scope);
}

public class VariableDeclaration(
    string name,
    Expression? type,
    bool isConst,
    Expression initializer) : Statement
{
    public string Name { get; } = name;
    public Expression? Type { get; } = type;
    public bool IsConstant { get; } = isConst;
    public Expression Initializer { get; } = initializer;
    
    public override VoxValue Execute(Scope scope)
    {
        var value = ExpressionMath.EvaluateValue(Initializer, scope);
        scope.SetValue(new IdentifierExpression([new LiteralExpression(Name)]), value, true);
        return VoxValue.Null;
    }
}

public class VariableRedefinition(
    IdentifierExpression identifier,
    Expression value) : Statement
{
    public IdentifierExpression Identifier { get; } = identifier;
    public Expression Value { get; } = value;
    
    public override VoxValue Execute(Scope scope)
    {
        scope.SetValue(Identifier, ExpressionMath.EvaluateValue(Value, scope));
        return VoxValue.Null;
    }
}

public class ArithmeticAssignment(
    IdentifierExpression identifier,
    string operation,
    Expression value) : Statement
{
    public IdentifierExpression Identifier { get; } = identifier;
    public string Operation { get; } = operation;
    public Expression Value { get; } = value;

    public override VoxValue Execute(Scope scope)
    {
        var existing = scope.GetValue(Identifier);
        VoxValue newVal = existing;
        if (existing is { Type: VoxValueType.Number })
        {
            var pVal = ExpressionMath.EvaluateValue(Value, scope);
            if (pVal is not {Type: VoxValueType.Number}) throw new ArithmeticException($"Attempted to add value of type {pVal.Type} to {existing.Type}");
            
            newVal = Operation switch
            {
                "+=" => existing + pVal,
                "-=" => existing - pVal,
                "*=" => existing * pVal,
                "/=" => existing / pVal,
                "%=" => existing % pVal,
                "^=" => Math.Pow(existing, pVal),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        if (existing is { Type: VoxValueType.String } && Operation is "+=" or "*=")
        {
            var pVal = ExpressionMath.EvaluateValue(Value, scope);
            
            if (Operation == "+=") newVal = existing.ToString() + pVal.ToString();
            if (Operation == "*=" && pVal is { Type: VoxValueType.Number })
            {
                newVal = string.Concat(Enumerable.Repeat(existing.ToString(), (int)pVal));
            }
        }
        scope.SetValue(Identifier, newVal);
        return VoxValue.Null;
    }
}

public class IncrementAssignment(IdentifierExpression identifier, bool negative) : Statement
{
    public IdentifierExpression Identifier { get; } = identifier;
    public double Sign { get; } = negative ? -1 : 1;

    public override VoxValue Execute(Scope scope)
    {
        var existing = scope.GetValue(Identifier);
        VoxValue newVal = existing;
        if (existing is { Type: VoxValueType.Number })
        {
            newVal = existing + Sign;
        }
        scope.SetValue(Identifier, newVal);
        
        return VoxValue.Null;
    }
}

public class StatementSet(List<Statement> statements) : Statement
{
    public List<Statement> Statements { get; } = statements;
    public Scope StatementScope { get; } = new();

    public void ResetScope(Scope parent)
    {
        StatementScope.Clear();
        StatementScope.ParentScope = parent;
    }
    
    public override VoxValue Execute(Scope scope)
    {
        ResetScope(scope);
        foreach (var statement in Statements)
        {
            var returned = statement.Execute(StatementScope);
            if (returned.Type is VoxValueType.Return or VoxValueType.Break or VoxValueType.Continue)
            {
                return returned;
            }
        }
        return VoxValue.Null;
    }
}

public class IfStatement(
    Expression condition,
    StatementSet thenBranch,
    StatementSet? elseBranch)
    : Statement
{
    public Expression Condition { get; } = condition;
    public StatementSet ThenBranch { get; } = thenBranch;
    public StatementSet? ElseBranch { get; } = elseBranch;
    
    public override VoxValue Execute(Scope scope)
    {
        if (ExpressionMath.EvaluateValue(Condition, scope))
        {
            return ThenBranch.Execute(scope);
        }

        return ElseBranch?.Execute(scope) ?? VoxValue.Null;
    }
}

public class WhileStatement(
    Expression condition,
    StatementSet body) : Statement
{
    public Expression Condition { get; } = condition;
    public StatementSet Body { get; } = body;
    
    public override VoxValue Execute(Scope scope)
    {
        while (ExpressionMath.EvaluateValue(Condition, scope))
        {
            var returned = Body.Execute(scope);
            if (returned.Type is VoxValueType.Return)
            {
                return returned;
            }
            if (returned.Type is VoxValueType.Break)
            {
                break;
            }
            // Do nothing on continue because StatementSet has already handled it
        }
        return VoxValue.Null;
    }
}

public class ForStatement(ForModeImpl mode, StatementSet body) : Statement
{
    public StatementSet Body { get; } = body;
    public ForModeImpl Mode { get; } = mode;

    public override VoxValue Execute(Scope scope)
    {
        Mode.Reset(scope);

        while (true)
        {
            var result = Mode.Execute(Body, scope);
            if (result.ReturnValue.Type == VoxValueType.Break) break;
            if (result.ReturnValue.Type == VoxValueType.Return) return result.ReturnValue;
            if (!result.ShouldContinueExecuting) break;
        }
        
        return VoxValue.Null;
    }
}

public class FunctionDeclaration(
    IdentifierExpression identifier,
    List<IdentifierExpression> parameters,
    StatementSet body)
    : Statement
{
    public IdentifierExpression Identifier { get; } = identifier;
    public List<IdentifierExpression> Parameters { get; } = parameters;
    public StatementSet Body { get; } = body;

    public VoxValue Invoke(Scope scope, List<VoxValue> args)
    {
        int index = 0;
        foreach (var parameter in Parameters)
        {
            scope.SetValue(parameter, args[index]);
            index++;
        }
        
        return Body.Execute(scope);
    }
    
    public override VoxValue Execute(Scope scope)
    {
        scope.SetValue(Identifier, new VoxFunction(this, scope), true);
        
        return VoxValue.Null;
    }
}

public class FunctionCall(
    IdentifierExpression identifier,
    List<Expression> parameters) : Statement
{
    public IdentifierExpression Identifier { get; } = identifier;
    public List<Expression> Parameters { get; } = parameters;
    
    public override VoxValue Execute(Scope scope)
    {
        VoxValue potentialFunc = scope.GetValue(Identifier);

        if (potentialFunc.Type == VoxValueType.Function)
        {
            var func = (VoxFunctionBase?)potentialFunc.Reference;
            if (func != null)
            {
                List<VoxValue> inputs = [];
                foreach (var param in Parameters)
                {
                    inputs.Add(ExpressionMath.EvaluateValue(param, scope));
                }
                
                return func.Invoke(inputs);
            }
        }
        
        return VoxValue.Null;
    }
}

public class ReturnStatement(Expression? value) : Statement
{
    public Expression? Value { get; } = value;
    
    public override VoxValue Execute(Scope scope)
    {
        return VoxValue.Return(Value != null ? ExpressionMath.EvaluateValue(Value, scope) : VoxValue.Null);
    }
}

public class BreakStatement : Statement
{
    public override VoxValue Execute(Scope scope)
    {
        return VoxValue.Break();
    }
}

public class ContinueStatement : Statement
{
    public override VoxValue Execute(Scope scope)
    {
        return VoxValue.Continue();
    }
}