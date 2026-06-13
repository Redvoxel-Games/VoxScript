using VoxScript.Tree;

namespace VoxScript.Runtime;

public abstract class VoxFunctionBase {
    public abstract VoxValue Invoke(List<VoxValue> args);
}

public class VoxFunction(FunctionDeclaration declaration, Scope parent) : VoxFunctionBase
{
    public FunctionDeclaration Declaration { get; } = declaration;
    public Scope ParentScope { get; } = parent;

    public override VoxValue Invoke(List<VoxValue> args)
    {
        var returned = Declaration.Invoke(ParentScope, args);
        if (returned.Type is VoxValueType.Return)
        {
            var value = returned.Reference;
            if (value is VoxValue v) return v; 
        }

        return VoxValue.Null;
    }
}

public class VoxFunctionExpr(FunctionExpression function, Scope parent) : VoxFunctionBase
{
    public FunctionExpression Function { get; } = function;
    public Scope ParentScope { get; } = parent;

    public override VoxValue Invoke(List<VoxValue> args)
    {
        var bodyScope =  Function.Body.StatementScope;

        int index = 0;
        foreach (var parameter in Function.Parameters)
        {
            bodyScope.SetValue(parameter, args[index]);
            index++;
        }
        
        var returned = Function.Body.Execute(ParentScope);
        if (returned.Type is VoxValueType.Return)
        {
            var value = returned.Reference;
            if (value is VoxValue v) return v; 
        }

        return VoxValue.Null;
    }
}

public class ContextFunction(Func<List<VoxValue>, VoxValue> func) : VoxFunctionBase
{
    public readonly Func<List<VoxValue>, VoxValue> Func = func;

    public override VoxValue Invoke(List<VoxValue> args)
    {
        return Func(args);
    }
}