using VoxScript.Runtime;

namespace VoxScript.Tree;

public record ForCallResult(VoxValue ReturnValue, bool ShouldContinueExecuting);

public abstract class ForModeImpl : AstNode
{
    public abstract void Reset(Scope scope);
    public abstract ForCallResult Execute(StatementSet set, Scope scope);
}

public class ForRepeat(IdentifierExpression identifier, Expression initialValue, Expression repeatCount, Expression valueDelta) : ForModeImpl
{
    // Assume expression results are numbers
    public IdentifierExpression Identifier { get; } = identifier;
    public Expression RawInitialValue { get; } = initialValue;
    public Expression RawValueDelta { get; } = valueDelta;
    public Expression RawRepeatCount { get; } = repeatCount;

    private double _value;
    private double _valueDelta;
    private double _repeatCount;

    public override void Reset(Scope scope)
    {
        _value = ExpressionMath.EvaluateValue(RawInitialValue, scope);
        _valueDelta = ExpressionMath.EvaluateValue(RawValueDelta, scope);
        _repeatCount = ExpressionMath.EvaluateValue(RawRepeatCount, scope);
    }
    
    public override ForCallResult Execute(StatementSet set, Scope scope)
    {
        bool continueExecuting = true;

        _repeatCount--;
        if (_repeatCount <= 0) continueExecuting = false;
        
        scope.SetValue(Identifier, _value);
        _value += _valueDelta;
        
        var result = set.Execute(scope);
        return new ForCallResult(result, continueExecuting);
    }
}

public class ForRange(IdentifierExpression identifier, Expression from, Expression to, Expression increment) : ForModeImpl
{
    public IdentifierExpression Identifier { get; } = identifier;
    public Expression RawFrom { get; } = from;
    public Expression RawTo { get; } = to;
    public Expression RawIncrement { get; } = increment;

    private double _from;
    private double _to;
    private double _increment;
    private double _value;
    private double? _prevDir;

    public override void Reset(Scope scope)
    {
        _from = ExpressionMath.EvaluateValue(RawFrom, scope);
        _to = ExpressionMath.EvaluateValue(RawTo, scope);
        _increment = ExpressionMath.EvaluateValue(RawIncrement, scope);
        _increment = Math.Abs(_increment) * Math.Sign(_to - _from);
        
        _value = _from;
        _prevDir = null;
    }

    public override ForCallResult Execute(StatementSet set, Scope scope)
    {
        scope.SetValue(Identifier, _value);
        _value += _increment;
        
        var dirToEnd = Math.Sign(_to - _value);
        bool continueExecuting = _prevDir == null || dirToEnd == _prevDir;
        _prevDir = dirToEnd;
        
        var result = set.Execute(scope);
        return new ForCallResult(result, continueExecuting);
    }
}

public class ForEach(IdentifierExpression keyIden, IdentifierExpression valIden, Expression obj) : ForModeImpl
{
    public IdentifierExpression KeyIdentifier { get; } = keyIden;
    public IdentifierExpression ValIdentifier { get; } = valIden;
    public Expression Object { get; } = obj;

    private List<VoxValue> _keys = [];
    private List<VoxValue> _values = [];
    private int _index;

    public override void Reset(Scope scope)
    {
        _keys.Clear();
        _values.Clear();
        _index = 0;

        VoxValue val = ExpressionMath.EvaluateValue(Object, scope);
        if (val.Type == VoxValueType.Object)
        {
            VoxObject obj = val.Reference as VoxObject ?? throw new NullReferenceException("Could not find object, make sure you create objects via the Object() or FromObject() methods only");
            _keys.AddRange(obj.Keys);
            _values.AddRange(obj.Values);
        }
        else
        {
            throw new ValueException("Given value is not an object");
        }
    }

    public override ForCallResult Execute(StatementSet set, Scope scope)
    {
        scope.SetValue(KeyIdentifier, _keys[_index]);
        scope.SetValue(ValIdentifier, _values[_index]);
        _index++;
        
        var result = set.Execute(scope);

        return new ForCallResult(result, _index < _keys.Count - 1);
    }
}