using System.Reflection;
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

    public static ContextFunction? ToFunction(MethodInfo methodInfo, object? context)
    {
        // Detect if method uses any ref structs and return null if so
        foreach (ParameterInfo parameter in methodInfo.GetParameters())
        {
            if (parameter.ParameterType.IsByRefLike) return null;
        }
        
        // Return
        return new ContextFunction(args =>
                    {
                        var parameters = methodInfo.GetParameters();

                        object? result;

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
                        else
                        {
                            object?[] invokeArgs = new object[parameters.Length];

                            for (int i = 0; i < parameters.Length; i++)
                            {
                                var parameter = parameters[i];
                                // Convert arg to parameter type if possible
                                var type = parameter.ParameterType;
                                if (TryConvert(args[i], type, out var value))
                                {
                                    invokeArgs[i] = value;
                                } else throw new InvalidCastException("Cannot convert value to parameter type of " + type.Name);
                            } 
                                
                            result = methodInfo.Invoke(context, invokeArgs);
                        }
                        
                        return VoxValue.FromObject(result);
                    });
    }

    public static bool TryConvert(VoxValue value, Type type, out object? result)
    {
        result = null;
        
        if (type == typeof(double)) result = (double)value;
        else if (type == typeof(float)) result = (float)(double)value;
        else if (type == typeof(int)) result = (int)(double)value;
        else if (type == typeof(long)) result = (long)(double)value;
        else if (type == typeof(string)) result = value.ToString();
        else if (type == typeof(VoxValue)) result = value;
        
        return result != null;
    }

    public static ScriptObject ToVoxObject(object obj)
    {
        return new VoxExternalObject(obj);
    }

    public static ScriptObject ExposeStaticMethods(Type type)
    {
        VoxObject obj = new();
        
        var methods = type.GetMethods()
            .Where(m => m.GetCustomAttribute<ExposeAsAttribute>() != null)
            .GroupBy(m =>
            {
                var attr = m.GetCustomAttribute<ExposeAsAttribute>();
                return attr?.Name ?? m.Name;
            });
        
        foreach (var group in methods)
        {
            var bestMethod = group
                .OrderByDescending(GetMethodScore)
                .First();

            var func = ToFunction(bestMethod, obj);

            if (func == null)
                continue;

            obj.SetValue(group.Key, func);
        }

        return obj;
    }
    
    internal static int GetMethodScore(MethodInfo method)
    {
        int score = 0;

        foreach (var p in method.GetParameters())
        {
            if (p.ParameterType == typeof(string))
                score += 100;

            if (p.ParameterType.IsByRefLike)
                score -= 1000;

            if (p.ParameterType.IsPointer)
                score -= 1000;
        }

        return score;
    }
}

/// <summary>
/// Exposes a field or method to VoxScript.
/// </summary>
/// <param name="name">Optional name parameter, defaults to existing name.</param>
/// <remarks>Fields only expose if the parent class <see cref="ContextType"/> is set to "Joined"</remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property)]
public class ExposeAsAttribute(string? name = null, bool readOnly = false) : Attribute
{
    public readonly string? Name = name;
    public readonly bool? ReadOnly = readOnly;
}