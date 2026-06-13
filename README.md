A basic scripting language made in C#.
<br />Created because there's no good C# binding for [Luau](https://luau.org/)

Setup example:
```csharp
const string content =
"""
print("Hello, World!")
""";

var scriptHandler = new VoxScriptHandler(content);

scriptHandler.AddContext(new Context()); // A context object

scriptHandler.Run();
```

Context example:
```csharp
[ExposeToScript(ContextType.Individual)]
public class Context
{
    [ExposeAs("print")] public VoxValue Print(params VoxValue[] input)
    {
        string str = input[0].ToString();
        foreach (var inp in input[1..])
        {
            str += " " + inp.ToString();
        }
        Console.WriteLine(str);

        return VoxValue.Null;
    }

    [ExposeAs("add")] public VoxValue Add(VoxValue a, VoxValue b)
    {
        return a + b;
    }
}
```