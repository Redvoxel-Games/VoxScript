using System.Collections.Concurrent;
using System.Reflection;

namespace VoxScript.Integration;

public static class ReflectionCache
{
    private static readonly ConcurrentDictionary<(Type, bool), ReflectionResult> Cache = new();

    public static ReflectionResult Reflect(Type type, bool getStatic = false)
    {
        return Cache.GetOrAdd(
            (type, getStatic),
            key => new ReflectionResult(key.Item1, key.Item2));
    }

    public static MethodInfo[] GetMethodsCached(this Type type, bool getStatic = false)
    {
        var reflectionResult = Reflect(type, getStatic);

        var result = new MethodInfo[reflectionResult.Methods.Count];

        int index = 0;
        foreach (var methodInfo in reflectionResult.Methods)
        {
            result[index++] = methodInfo.Value.Method;
        }
        
        return result;
    }

    public static MethodInfo GetMethodCached(this Type type, string name, bool getStatic = false)
    {
        var reflectionResult = Reflect(type, getStatic);
        
        return reflectionResult.Methods[name].Method;
    }

    public static MethodInfo? GetMethodCached(this Type type, string name, Type[] types, bool getStatic = false)
    {
        var reflectionResult = Reflect(type, getStatic);

        List<MethodInfoCache> named = [];
        
        foreach (var methodInfo in reflectionResult.Methods.Values)
        {
            if (methodInfo.Method.Name == name)
            {
                named.Add(methodInfo);
            }
        }

        foreach (var methodInfo in named)
        {
            if (methodInfo.Parameters.Count != types.Length)
                continue;

            var fits = true;
            for (var i = 0; i < types.Length; i++)
            {
                if (types[i] != methodInfo.Parameters[i].ParameterType)
                {
                    fits = false;
                    break;
                }
            }

            if (fits)
                return methodInfo.Method;
        }

        return null;
    }
}

public class ReflectionResult
{
    public readonly Dictionary<string, MethodInfoCache> Methods = new();
    public readonly Dictionary<string, FieldInfo> Fields = new();
    public readonly Dictionary<string, PropertyInfo> Properties = new();

    public ReflectionResult(Type type, bool getStatic = false)
    {
        var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic;
        
        if (getStatic) bindingFlags |= BindingFlags.Static;
        else bindingFlags |= BindingFlags.Instance;
        
        var methodInfos = type.GetMethods(bindingFlags);
        foreach (var method in methodInfos)
        {
            var expose = method.GetCustomAttribute<ExposeAsAttribute>();
            if (expose == null)
                continue;
            
            var parameters = method.GetParameters();
            var cache = new MethodInfoCache
            {
                Method = method,
            };
            cache.Parameters.AddRange(parameters);
            Methods[expose.Name ?? method.Name] = cache;
        }
        
        var fieldInfos = type.GetFields(bindingFlags);
        CacheMembers(fieldInfos, Fields);
        
        var propertyInfos = type.GetProperties(bindingFlags);
        CacheMembers(propertyInfos, Properties);
    }
    
    private static void CacheMembers<TMember>(
        IEnumerable<TMember> members,
        Dictionary<string, TMember> cache)
        where TMember : MemberInfo
    {
        foreach (var member in members)
        {
            var expose = member.GetCustomAttribute<ExposeAsAttribute>();
            if (expose == null)
                continue;

            cache[expose.Name ?? member.Name] = member;
        }
    }
}

public class MethodInfoCache
{
    public MethodInfo Method;
    public readonly List<ParameterInfo> Parameters = [];
    public Delegate? StaticDelegate;
}