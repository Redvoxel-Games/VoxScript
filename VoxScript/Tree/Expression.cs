using VoxScript.Runtime;

namespace VoxScript.Tree;

public abstract class Expression : AstNode
{
    public override string ToString()
    {
        return GetStringValue();
    }

    protected virtual string GetStringValue()
    {
        return "";
    }
}

public class LiteralExpression(VoxValue value) : Expression
{
    public VoxValue Value { get; } = value;

    public override bool Equals(object? obj)
    {
        if (obj is LiteralExpression literal)
        {
            return Value.Equals(literal.Value);
        }
        if (obj is VoxValue other)
        {
            return Value.Equals(obj);
        }

        return false;
    }

    public string GetStringValue()
    {
        return Value.ToString();
    }
}

public class UnaryExpression(string op, Expression operand) : Expression
{
    public string Op { get; } = op;
    public Expression Operand { get; } = operand;
}

public class BinaryExpression(
    Expression left,
    string op,
    Expression right) : Expression
{
    public Expression Left { get; } = left;
    public string Operator { get; } = op;
    public Expression Right { get; } = right;
}

public class ConditionalExpression(Expression condition, Expression primary, Expression secondary) : Expression
{
    public Expression Condition { get; } = condition;
    public Expression Primary { get; } = primary;
    public Expression Secondary { get; } = secondary;
}

public class FunctionExpression(
    List<IdentifierExpression> parameters,
    StatementSet body) : Expression
{
    public List<IdentifierExpression> Parameters { get; } = parameters;
    public StatementSet Body { get; } = body;
}

public class CallExpression(
    Expression target,
    List<Expression> arguments) : Expression
{
    public Expression Target { get; } = target;
    public List<Expression> Arguments { get; } = arguments;
}

public class IdentifierExpression(List<Expression> path) : Expression
{
    public List<Expression> Path { get; } = path;

    public bool Equals(IdentifierExpression other)
    {
        if (Path.Count != other.Path.Count) return false;
        for (var index = 0; index < Path.Count; index++)
        {
            if (!Path[index].Equals(other.Path[index])) return false;
        }
        return true;
    }

    public override string ToString()
    {
        return string.Join(".", Path);
    }

    public static implicit operator IdentifierExpression(string single)
    {
        return new IdentifierExpression([new LiteralExpression(single)]);
    }
}

public class ObjectExpression(List<Expression> keys, List<Expression> values) : Expression
{
    public List<Expression> Keys { get; } = keys;
    public List<Expression> Values { get; } = values;
}

public class ArrayExpression(List<Expression> values) : Expression
{
    public List<Expression> Values { get; } = values;
}