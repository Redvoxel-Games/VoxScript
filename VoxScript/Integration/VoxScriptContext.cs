using System.Runtime.CompilerServices;
using VoxScript.Runtime;

namespace VoxScript.Integration;

/// <summary>
/// How a class is exposed to a script instance.
/// </summary>
/// 
public enum ContextType { Joined, Individual }

/// <summary>
/// Allows exposing any object of a class to VoxScript.
/// </summary>
/// <param name="contextType">The way it should be exposed</param>
/// <param name="name">Optional name parameter, defaults to existing name.</param>
[AttributeUsage(AttributeTargets.Class)]
public class ExposeToScriptAttribute(ContextType contextType, string? name=null) : Attribute
{
    public readonly ContextType ContextType = contextType;
    public readonly string? Name = name;

    public static VoxObject ToVoxObject(object obj)
    {
        var objType = obj.GetType();
        
        var fieldInfos = objType.GetFields();
        var methodInfos = objType.GetMethods();
        
        var voxObject = new VoxObject();
            
        foreach (var methodInfo in methodInfos)
        {
            var exposeAs = methodInfo.GetCustomAttributes(typeof(ExposeAsAttribute), false).FirstOrDefault() as ExposeAsAttribute;

            if (exposeAs != null)
            {
                string name = exposeAs.Name ?? methodInfo.Name;
                    
                ContextFunction func = new ContextFunction(args =>
                {
                    object? result = methodInfo.Invoke(obj, new object[] { args });
                    if (result != null) return (VoxValue)result;
                    return VoxValue.Null;
                });

                voxObject[name] = func;
            }
        }

        foreach (var fieldInfo in fieldInfos)
        {
            var exposeAs = fieldInfo.GetCustomAttributes(typeof(ExposeAsAttribute), false).FirstOrDefault() as ExposeAsAttribute;

            if (exposeAs != null)
            {
                string name = exposeAs.Name ?? fieldInfo.Name;
                    
                VoxValue value = new VoxValue(VoxValueType.ExternalValue, default, new ExternalField(fieldInfo, obj));

                voxObject[name] = value;
            }
        }
        
        return voxObject;
    }
}

/// <summary>
/// Exposes a field or method to VoxScript.
/// </summary>
/// <param name="name">Optional name parameter, defaults to existing name.</param>
/// <remarks>Fields only expose if the parent class <see cref="ContextType"/> is set to "Joined"</remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Field)]
public class ExposeAsAttribute(string? name = null, bool readOnly = false) : Attribute
{
    public readonly string? Name = name;
    public readonly bool? ReadOnly = readOnly;
}