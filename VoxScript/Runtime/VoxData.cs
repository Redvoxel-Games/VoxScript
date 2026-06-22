using System.Collections;
using System.Reflection;
using VoxScript.Integration;

namespace VoxScript.Runtime;

public abstract class ScriptObject
{
    public abstract VoxValue GetValue(VoxValue key);
    public abstract void SetValue(VoxValue key, VoxValue value);
    public abstract bool HasKey(VoxValue key);
}

[ExposeToScript(ContextType.Joined)]
public class VoxObject : ScriptObject, IEnumerable<KeyValuePair<VoxValue, VoxValue>>
{
    [ExposeAs]
    public readonly List<VoxValue> Keys = [];
    
    [ExposeAs]
    public readonly List<VoxValue> Values = [];

    private int IndexOfKey(VoxValue key)
    {
        int i = 0;
        foreach (VoxValue v in Keys)
        {
            if (v.Equals(key)) return i;
            i++;
        }
        return -1;
    }

    public VoxValue this[VoxValue key]
    {
        get => Values[IndexOfKey(key)];
        set
        {
            if (!Keys.Contains(key))
            {
                Keys.Add(key);
                Values.Add(value);
            }
            else
            {
                Values[Keys.IndexOf(key)] = value;
            }
        }
    }

    public override bool HasKey(VoxValue key)
    {
        return Keys.Contains(key);
    }


    public IEnumerator<KeyValuePair<VoxValue, VoxValue>> GetEnumerator()
    {
        for (int i = 0; i < Keys.Count; i++)
        {
            yield return new KeyValuePair<VoxValue, VoxValue>(Keys[i], Values[i]);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public override VoxValue GetValue(VoxValue key)
    {
        return this[key];
    }

    public override void SetValue(VoxValue key, VoxValue value)
    {
        this[key] = value;
    }

    public override string ToString()
    {
        var result = "{";
        for (var i = 0; i < Keys.Count; i++)
        {
            result += (i>0 ? ", " : "") + $"{Keys[i]}={Values[i]}";
        }
        return result + "}";
    }
}

public class VoxExternalObject : ScriptObject
{
    public readonly object Reference;

    private readonly List<string> Keys = [];
    private readonly List<VoxValue> Values = [];

    public VoxExternalObject(object obj)
    {
        Reference = obj;
        
        var objType = obj.GetType();
        
        var fieldInfos = objType.GetFields();
        foreach (var fieldInfo in fieldInfos)
        {
            if (fieldInfo.GetCustomAttributes(typeof(ExposeAsAttribute), false).FirstOrDefault() is ExposeAsAttribute exposeAs)
            {
                string name = exposeAs.Name ?? fieldInfo.Name;
                    
                VoxValue value = new VoxValue(VoxValueType.ExternalValue, default, new ExternalField(fieldInfo, obj));
                
                Keys.Add(name);
                Values.Add(value);
            }
        }
        
        var methods = objType.GetMethods()
            .Where(m => m.GetCustomAttribute<ExposeAsAttribute>() != null)
            .GroupBy(m =>
            {
                var attr = m.GetCustomAttribute<ExposeAsAttribute>();
                return attr?.Name ?? m.Name;
            });
        
        foreach (var group in methods)
        {
            var bestMethod = group
                .OrderByDescending(ExposeToScriptAttribute.GetMethodScore)
                .First();

            var func = ExposeToScriptAttribute.ToFunction(bestMethod, obj);

            if (func == null)
                continue;

            Keys.Add(group.Key);
            Values.Add((VoxValue)func);
        }
        
        var propertyInfos = objType.GetProperties();
        foreach (var propertyInfo in propertyInfos)
        {
            if (propertyInfo.GetCustomAttributes(typeof(ExposeAsAttribute), false).FirstOrDefault() is ExposeAsAttribute exposeAs)
            {
                string name = exposeAs.Name ?? propertyInfo.Name;
                
                VoxValue value = new VoxValue(VoxValueType.ExternalValue, default, new ExternalProperty(propertyInfo, obj));
                
                Keys.Add(name);
                Values.Add(value);
            }
        }
    }
    
    public override VoxValue GetValue(VoxValue key)
    {
        for (int i = 0; i < Keys.Count; i++)
        {
            if (Keys[i].Equals(key))
            {
                var vRef = Values[i].Reference;
                if (vRef is ExternalField externalField) return VoxValue.FromObject(externalField.fieldInfo.GetValue(Reference));
                if (vRef is ExternalProperty externalProperty) return VoxValue.FromObject(externalProperty.propertyInfo.GetValue(Reference));
                if (vRef is VoxFunctionBase func) return func;
            }
        }

        return VoxValue.Null;
    }

    public override void SetValue(VoxValue key, VoxValue value)
    {
        if (Keys.Contains(key))
        {
            var existing = Values[Keys.IndexOf(key)];
            if (existing.Reference is ExternalField field)
            {
                if (!field.readOnly)
                {
                    var fld = field.fieldInfo;
                    if (value.Type == VVT.String && fld.FieldType == typeof(string))
                        fld.SetValue(Reference, value.ToString());
                    else if (value.Type == VVT.Number && (
                                 fld.FieldType == typeof(double)
                                 || fld.FieldType == typeof(float)
                                 || fld.FieldType == typeof(int)
                             )) fld.SetValue(Reference, value.Value.NumberValue);
                    else if (value.Type == VVT.Boolean && fld.FieldType == typeof(bool))
                        fld.SetValue(Reference, value.Value.BooleanValue);
                    else if (value.Type == VoxValueType.Object && value.Reference is VoxExternalObject externalObject)
                    {
                        fld.SetValue(Reference, externalObject.Reference);
                    }

                } else throw new AccessViolationException("Attempted to set readonly key.");
            }
            else if (existing.Reference is ExternalProperty property)
            {
                if (property.propertyInfo.CanWrite)
                {
                    var prop = property.propertyInfo;
                    if (value.Type == VVT.String && prop.PropertyType == typeof(string))
                        prop.SetValue(Reference, value.ToString());
                    else if (value.Type == VVT.Number && (
                                 prop.PropertyType == typeof(double)
                                 || prop.PropertyType == typeof(float)
                                 || prop.PropertyType == typeof(int)
                             )) prop.SetValue(Reference, value.Value.NumberValue);
                    else if (value.Type == VVT.Boolean && prop.PropertyType == typeof(bool))
                        prop.SetValue(Reference, value.Value.BooleanValue);
                    else if (value.Type == VoxValueType.Object && value.Reference is VoxExternalObject externalObject)
                    {
                        prop.SetValue(Reference, externalObject.Reference);
                    }
                } else throw new AccessViolationException("Attempted to set readonly key.");
            }
        }
        else throw new AccessViolationException("Attempted to add key to external object.");
    }

    public override bool HasKey(VoxValue key)
    {
        return Keys.Contains(key);
    }

    public override string ToString()
    {
        if (Reference.ToString() != Reference.GetType().ToString())
        {
            return Reference.ToString() ?? Reference.GetType().Name;
        }
        return Reference.GetType().Name;
    }
}