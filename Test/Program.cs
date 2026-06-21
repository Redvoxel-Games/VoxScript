using VoxScript;
using VoxScript.Integration;
using VoxScript.Runtime;

namespace Test;

public class Program
{
    public static void Main(string[] args)
    {
        const string fibTest =
            """
            print("Fibonacci sequence:")
            var a = 1
            var b = 2
            while (a < 10000) {
                var c = a + b
                a = b
                b = c
                print(c)
            }
            print("Done!")
            """;
        const string varToVarTest =
            """
            var a = 10
            var b = 20
            b = a + b
            print(b)
            """;
        const string externalTest =
            """
            print(obj.num)
            obj.num = 1
            print(obj.num)
            """;
        const string forTest =
            """
            print("Repeat:")
            for (num at 0 do 10 add 1) {
                print(num)
            }
            
            print("Range:")
            for (num from 0 to 10 per 2) {
                print(num)
            }
            """;
        const string objectTest =
            """
            var testObj = [
                "Hello, world!",
                "Second item",
                obj.num
            ]
            print(obj)
            obj.num = 10
            print(testObj[3])
            print(obj.num)
            """;
        const string functionTest =
            """
            function testFunc() {
                print("Hello, world!")
                return 3.14159
            }
            print(testFunc())
            """;
        const string scriptTest =
            """
            var result = addNum(1, 2)
            print(result)
            """;
        const string abstractTest =
            """
            print(abst.a + abst.b)
            """;
        const string arithTest =
            """
            var a = 11
            print(a)
            a%=10
            print(a)
            """;
        const string externalObjectTest =
            """
            var vec = newVec(3.14, 159)
            
            vecTest.Vector = vec;
            print(vecTest.Vector)
            """;
        
        var scriptHandler = new VoxScriptHandler(externalObjectTest);

        scriptHandler.AddContext(new Context());

        var externals = new TestExternals();

        var vecTest = new VectorContainer();
        
        scriptHandler.SetGlobal("vecTest", vecTest);
        scriptHandler.SetGlobal("obj", externals);
        scriptHandler.SetGlobal("abst", new AbstractTest2());
        
        scriptHandler.Run();
    }
}

public class TestExternals
{
    [ExposeAs("num")]
    public double number = 3.14159;
}

public abstract class AbstractTest1
{
    [ExposeAs]
    public string a = "Hello,";
}

public class AbstractTest2 : AbstractTest1
{
    [ExposeAs]
    public string b = " World!";
}

public class VectorContainer
{
    [ExposeAs] public Vector? Vector = null;
}

public class Vector(double x, double y)
{
    [ExposeAs] public double X = x;
    [ExposeAs] public double Y = y;

    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}

[ExposeToScript(ContextType.Individual)]
public class Context
{
    [ExposeAs]
    public Vector newVec(double x, double y)
    {
        return new Vector(x, y);
    }
    
    [ExposeAs("print")]
    public VoxValue Print(params VoxValue[] input)
    {
        string str = input[0].ToString();
        foreach (var inp in input[1..])
        {
            str += " " + inp.ToString();
        }
        Console.WriteLine(": "+str);
        return VoxValue.Null;
    }

    [ExposeAs("addNum")]
    public double Add(double a, double b)
    {
        return a + b;
    }
}