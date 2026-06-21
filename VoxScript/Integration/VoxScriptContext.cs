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

    public static ContextFunction ToFunction(MethodInfo methodInfo, object context)
    {
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
                                // Convert arg to parameter type if possible
                                var type = parameters[i].ParameterType;
                                if (type == typeof(double)) invokeArgs[i] = (double)args[i];
                                else if (type == typeof(float)) invokeArgs[i] = (float)(double)args[i];
                                else if (type == typeof(int)) invokeArgs[i] = (int)(double)args[i];
                                else if (type == typeof(long)) invokeArgs[i] = (long)(double)args[i];
                                else if (type == typeof(string)) invokeArgs[i] = args[i].ToString();
                            } 
                                
                            result = methodInfo.Invoke(context, invokeArgs);
                        }
                        
                        return VoxValue.FromObject(result);
                    });
    }

    public static ScriptObject ToVoxObject(object obj)
    {
        return new VoxExternalObject(obj);
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