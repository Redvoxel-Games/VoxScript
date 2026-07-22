using VoxScript;
using VoxScript.Integration;
using VoxScript.Runtime;
using VoxScript.Test;

namespace Test;

public class Program
{
    public static void Main(string[] args)
    {
        List<ScriptTest> tests = [];
        tests.Add(new IndexPerformanceTest("", 0, "Zero index test"));
        tests.Add(new IndexPerformanceTest("pr(\"t\")", 1, "Single index test"));
        tests.Add(new IndexPerformanceTest("pr(\"1\"); pr(\"2\")", 2, "Double index test"));
        
        tests.Add(new VarPerformanceTest("", 0, 0, "Zero var test"));
        tests.Add(new VarPerformanceTest("var a = 10", 1, 1, "Single var test"));

        string forPerfTest =
            """
            var obj = [1,2,3,4,5,6,7,8,9,10]
            
            for (_, _ in obj) {
                var a = 10
            }
            """;
        
        tests.Add(new VarPerformanceTest(forPerfTest, 41, 31, "For var index test"));
        
        string forPerfTest2 =
            """
            var obj = [1,2,3,4,5,6,7,8,9,10]

            var a
            for (_, _ in obj) {
                a = 10
            }
            """;
        
        tests.Add(new VarPerformanceTest(forPerfTest2, 42, 32, "For var index test 2"));
        
        
        var totalTests = tests.Count;
        var successes = 0;
        var failures = 0;
        var overshoots = 0;

        foreach (ScriptTest test in tests)
        {
            Console.WriteLine("");
            Console.WriteLine(string.Concat(Enumerable.Repeat("-", 25)));
            Console.WriteLine("PERFORMING TEST: '" + test.Name + "'\n");
            
            test.RunTest();
            TestResult result = test.EndTest();
            
            Console.WriteLine("\nRESULT: " + result.Result + ", " + result.Message);
            
            if (result.Result == TestResultType.Success) successes++;
            if (result.Result == TestResultType.Failure) failures++;
            if (result.Result == TestResultType.Overshoot) overshoots++;
        }
        
        Console.WriteLine(string.Concat(Enumerable.Repeat("-", 25)));
        Console.WriteLine("RESULTS:");
        Console.WriteLine("S: " + successes);
        Console.WriteLine("F: " + failures);
        Console.WriteLine("O: " + overshoots);
    }
}