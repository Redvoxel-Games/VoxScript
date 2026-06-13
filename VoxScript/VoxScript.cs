using System.Reflection;
using Antlr4.Runtime;
using VoxScript.Integration;
using VoxScript.Runtime;
using VoxScript.Tree;

namespace VoxScript;

public class VoxScriptHandler
{
    private RootNode ScriptRoot { get; } = null!;
    private Scope _currentScope = null!;
    
    public VoxScriptHandler(string content)
    {
        var inputStream = new AntlrInputStream(content);

        var lexer = new VoxScriptLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);

        var parser = new VoxScriptParser(tokenStream);

        var tree = parser.program();
        
        var builder = new AstBuilder();

        ScriptRoot = builder.Build(tree);
        _currentScope = ScriptRoot.GlobalScope;
    }

    public void SetGlobal(string name, object value)
    {
        VoxValue pValue = VoxValue.FromObject(value);
        ScriptRoot.GlobalScope.SetValue(name, pValue);
    }

    public void AddContext(object context)
    {
        var contextObjectType = context.GetType();
        var exposeType = contextObjectType.GetCustomAttributes(typeof(ExposeToScriptAttribute), false);
        var contextType = ContextType.Individual;
        var name = context.GetType().Name;
        if (exposeType.Length > 0)
        {
            ExposeToScriptAttribute expose = (ExposeToScriptAttribute)exposeType[0];
            contextType = expose.ContextType;
            name = expose.Name ?? name;
        }
        
        var fieldInfos = contextObjectType.GetFields();
        var methodInfos = contextObjectType.GetMethods();

        if (contextType == ContextType.Individual)
        {
            foreach (var methodInfo in methodInfos)
            {
                var exposeAs = methodInfo.GetCustomAttributes(typeof(ExposeAsAttribute), false).FirstOrDefault() as ExposeAsAttribute;

                if (exposeAs != null)
                {
                    string methodName = exposeAs.Name ?? methodInfo.Name;
                    
                    ContextFunction func = new ContextFunction(args =>
                    {
                        var parameters = methodInfo.GetParameters();

                        object? result = null;

                        if (parameters.LastOrDefault()?.GetCustomAttribute<ParamArrayAttribute>() != null)
                        {
                            int fixedCount = parameters.Length - 1;

                            object[] invokeArgs = new object[parameters.Length];

                            // Fixed parameters
                            for (int i = 0; i < fixedCount; i++)
                            {
                                invokeArgs[i] = args[i];
                            }

                            // Params parameter
                            Type elementType = parameters[^1].ParameterType.GetElementType()!;
                            Array paramArray = Array.CreateInstance(
                                elementType,
                                args.Count - fixedCount);

                            for (int i = fixedCount; i < args.Count; i++)
                            {
                                paramArray.SetValue(args[i], i - fixedCount);
                            }

                            invokeArgs[^1] = paramArray;

                            result = methodInfo.Invoke(context, invokeArgs);
                        }

                        return result is VoxValue value
                            ? value
                            : VoxValue.Null;
                    });

                    ScriptRoot.GlobalScope.SetValue(methodName, func);
                }
            }
        }

        if (contextType == ContextType.Joined)
        {
            ScriptRoot.GlobalScope.SetValue(name, ExposeToScriptAttribute.ToVoxObject(context));
        }
    }

    public void Run()
    {
        ScriptRoot.Run();
    }
}