namespace VoxScript.Runtime;

// For both script and context defined types
public abstract class VoxType : ScriptObject
{
    public readonly VoxType? Inherits = null;

    public override VoxValue GetValue(VoxValue key)
    {
        var result = Get(key);
        if (result.Equals(VoxValue.Null))
        {
            if (Inherits != null) return Inherits.GetValue(key);
        }
        return result;
    }

    public override void SetValue(VoxValue key, VoxValue value)
    {
        if (Has(key))
        {
            Set(key, value);
        }
        else
        {
            Inherits?.SetValue(key, value);
        }
    }

    public override bool HasKey(VoxValue key)
    {
        return Has(key) || (Inherits?.HasKey(key) ?? false);
    }

    protected abstract VoxValue Get(string key);
    protected abstract void Set(string key, VoxValue value);
    protected abstract bool Has(string key);
}

public class PrototypeMemberInfo
{
    public readonly bool IsStatic;
    public readonly string Name;
    public readonly bool ReadOnly;
    public readonly VoxType BelongsTo;

    public readonly VoxType? VoxType = null;
    public readonly Type? NativeType = null;
}

public class Prototype : VoxType
{
    private readonly Dictionary<string, VoxValue> _staticValues = new();

    private readonly List<PrototypeMemberInfo> _memberInfos = new();
    
    protected override VoxValue Get(string key)
    {
        if (Has(key))
        {
            return _staticValues[key];
        }
        return VoxValue.Null;
    }

    protected override void Set(string key, VoxValue value)
    {
        if (Has(key))
        {
            _staticValues[key] = value;
        }
        else
        {
            _staticValues.Add(key, value);
        }
    }

    protected override bool Has(string key)
    {
        foreach (var info in _memberInfos)
        {
            if (info.Name == key) return true;
        }

        return false;
    }
}