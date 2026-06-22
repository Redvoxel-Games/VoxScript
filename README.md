A basic scripting language made in C#.

Created for an in-development roblox-like game engine called Voxgen.

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

<p>Note that VoxScript does not come with any built-in functions for anything (Such as `print()`).</p>
<p>You are expected to supply such functions yourself, an example can be found below.</p>

Context example:
```csharp
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
}
```
Methods that you expose to a script automatically convert to and from VoxValues. For example, if an exposed method takes in a double, VoxScript will convert given VoxValues to a double.

Things that are not currently implemented because I did this in my free time and can't be bothered to try:
<ul>
<li>Threads/Coroutines
<li>Types
<li>Error detection/handling
</ul>