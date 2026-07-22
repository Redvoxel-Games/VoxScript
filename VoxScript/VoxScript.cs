using System.Reflection;
using Antlr4.Runtime;
using VoxScript.Integration;
using VoxScript.Runtime;
using VoxScript.Test;
using VoxScript.Tree;

namespace VoxScript;

public class VoxScriptHandler
{
    private RootNode ScriptRoot { get; } = null!;
    private ScriptTest? _currentTest = null;

    internal void TestIndex()
    {
        if (_currentTest?.ReceiveEvents ?? false) _currentTest?.TestOnIndex();
    }

    internal void TestScopeGet()
    {
        if (_currentTest?.ReceiveEvents ?? false) _currentTest?.TestScopeGet();
    }

    internal void TestScopeSet()
    {
        if (_currentTest?.ReceiveEvents ?? false) _currentTest?.TestScopeSet();
    }
    internal void TestExprEvalNative() => _currentTest?.TestExprEvalNative();
    internal void TestExprEvalVox() => _currentTest?.TestExprEvalVox();

    public void EnableTestMode(ScriptTest test)
    {
        GlobalScope._testMode = true;
        GlobalScope._handler = this;
        _currentTest = test;
    }
    
    public VoxScriptHandler(string content)
    {
        var inputStream = new AntlrInputStream(content);

        var lexer = new VoxScriptLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);

        var parser = new VoxScriptParser(tokenStream);

        var tree = parser.program();
        
        var builder = new AstBuilder();

        ScriptRoot = builder.Build(tree);
    }
    
    public Scope GlobalScope => ScriptRoot.GlobalScope;

    public void SetGlobal(string name, object value)
    {
        VoxValue pValue = VoxValue.FromObject(value);
        ScriptRoot.GlobalScope.SetValue(name, pValue);
    }

    public VoxValue GetGlobal(string name)
    {
        return ScriptRoot.GlobalScope.GetValue(name);
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
        
        var methodInfos = contextObjectType.GetMethods();

        if (contextType == ContextType.Individual)
        {
            foreach (var methodInfo in methodInfos)
            {
                var exposeAs = methodInfo.GetCustomAttributes(typeof(ExposeAsAttribute), false).FirstOrDefault() as ExposeAsAttribute;

                if (exposeAs != null)
                {
                    string methodName = exposeAs.Name ?? methodInfo.Name;

                    ScriptRoot.GlobalScope.SetValue(methodName, ExposeToScriptAttribute.ToFunction(methodInfo, context));
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