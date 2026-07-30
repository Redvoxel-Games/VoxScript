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
                Method = method
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
}