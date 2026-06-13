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
            print(testObj[2])
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
        
        var scriptHandler = new VoxScriptHandler(arithTest);

        scriptHandler.AddContext(new Context());

        var externals = new TestExternals();
        
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

[ExposeToScript(ContextType.Individual)]
public class Context
{
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