using VoxScript;
using VoxScript.Integration;
using VoxScript.Runtime;

namespace VoxScript.Test;

public abstract class ScriptTest(string name)
{
    public readonly string Name = name;
    public bool ReceiveEvents = false;

    public abstract void RunTest();

    public virtual TestResult EndTest()
    {
        return new(TestResultType.Success, "Default");
    }

    public virtual void TestOnIndex() { }
    public virtual void TestScopeSet() { }
    public virtual void TestScopeGet() { }
    public virtual void TestExprEvalNative() { }
    public virtual void TestExprEvalVox() { }
}

public enum TestResultType { Success, Failure, Overshoot }
public record TestResult(TestResultType Result, string Message);

[ExposeToScript(ContextType.Individual)]
public class BaseEnv
{
    [ExposeAs("pr")]
    public void print(VoxValue msg)
    {
        Console.WriteLine(msg.ToString());
    }
}

public class IndexPerformanceTest(string source, int expectedIndexCount, string name) : ScriptTest(name)
{
    private string _src = source;
    private int _expectedIndexCount = expectedIndexCount;
    private int _actualIndexCount = 0;
    
    private VoxScriptHandler _handler;
    
    public override void RunTest()
    {
        _handler = new VoxScriptHandler(_src);
        _handler.EnableTestMode(this);
        
        _handler.AddContext(new BaseEnv());
        
        ReceiveEvents = true;
        Console.WriteLine("Enabled events");
        
        _handler.Run();
    }

    public override void TestOnIndex()
    {
        _actualIndexCount++;
    }

    public override TestResult EndTest()
    {
        ReceiveEvents = false;
        
        TestResultType result = _actualIndexCount == _expectedIndexCount ? TestResultType.Success : (_actualIndexCount < _expectedIndexCount ? TestResultType.Overshoot : TestResultType.Failure);
        return new(result, "Expected " + _expectedIndexCount + " and got " + _actualIndexCount);
    }
}

public class VarPerformanceTest(string source, int expectedIndexCount, int expectedSetCount, string name)
    : ScriptTest(name)
{
    private string _src = source;
    private int _expectedIndexCount = expectedIndexCount;
    private int _expectedSetCount = expectedSetCount;
    
    private int _actualIndexCount = 0;
    private int _actualSetCount = 0;
    
    private VoxScriptHandler _handler;
    public override void RunTest()
    {
        _handler = new VoxScriptHandler(_src);
        _handler.EnableTestMode(this);
        
        _handler.AddContext(new BaseEnv());
        
        ReceiveEvents = true;
        Console.WriteLine("Enabled events");
        
        _handler.Run();
    }

    public override void TestOnIndex()
    {
        _actualIndexCount++;
    }

    public override void TestScopeSet()
    {
        _actualSetCount++;
    }

    public override TestResult EndTest()
    {
        ReceiveEvents = false;

        var res = TestResultType.Success;

        if (_actualIndexCount > _expectedIndexCount)
        {
            res = TestResultType.Failure;
        }

        if (_actualSetCount > _expectedSetCount)
        {
            res = TestResultType.Failure;
        }
        
        return new TestResult(res, _expectedSetCount + "|" + _actualSetCount + ":" + _expectedIndexCount + "|" + _actualIndexCount);
    }
}