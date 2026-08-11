using System.Collections;
using System.Diagnostics;
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
        get => HasKey(key) ? Values[IndexOfKey(key)] : VoxValue.Null;
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
    public object? Reference;
    public Type RefType;
    public ReflectionResult ReflectionResult;

    private readonly List<string> Keys = [];
    private readonly List<VoxValue> Values = [];

    public object ConvertBack()
    {
        if (Reference == null) throw new NullReferenceException("Attempt to convert object without reference");

        return Reference;
    }

    public static VoxExternalObject ExposeType(Type type, object? instance)
    {
        VoxExternalObject voxObj = new VoxExternalObject();
        voxObj.Reference = instance;
        voxObj.RefType = type;
        voxObj.ReflectionResult = ReflectionCache.Reflect(type, instance == null);
        
        foreach (var pair in voxObj.ReflectionResult.Fields)
        {
            VoxValue value = new VoxValue(VoxValueType.ExternalValue, default, new ExternalField(pair.Value, instance));
                
            voxObj.Keys.Add(pair.Key);
            voxObj.Values.Add(value);
        }
        
        var methods = voxObj.ReflectionResult.Methods
            .GroupBy(p =>
            {
                return p.Key;
            });
        
        foreach (var group in methods)
        {
            var bestMethod = group
                .OrderByDescending(ExposeToScriptAttribute.GetMethodScore)
                .First();

            var func = ExposeToScriptAttribute.ToFunction(bestMethod.Value.Method, instance);

            if (func == null)
                continue;

            voxObj.Keys.Add(group.Key);
            voxObj.Values.Add((VoxValue)func);
        }
        
        foreach (var pair in voxObj.ReflectionResult.Properties)
        {
            VoxValue value = new VoxValue(VoxValueType.ExternalValue, default, new ExternalProperty(pair.Value, instance));
                
            voxObj.Keys.Add(pair.Key);
            voxObj.Values.Add(value);
        }

        return voxObj;
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

        if (Reference is IScriptIndexable indexable)
        {
            return indexable.GetScriptIndexResult(key);
        }

        return VoxValue.Null;
    }

    public override void SetValue(VoxValue key, VoxValue value)
    {
        var objType = Reference.GetType();
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
                             ))
                    {
                        fld.SetValue(Reference, Convert.ChangeType(value.Value.NumberValue, fld.FieldType));
                    }
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
        if (Reference == null)
        {
            return "static:" + RefType.Name;
        }
        if (Reference.ToString() != Reference.GetType().ToString())
        {
            return Reference.ToString() ?? Reference.GetType().Name;
        }
        return Reference.GetType().Name;
    }

    private bool ObjEq(VoxExternalObject externalObject)
    {
        if (Reference is Enum e1 && externalObject.Reference is Enum e2)
        {
            return Equals(e1, e2);
        }
        return externalObject.Reference != null && externalObject.Reference.Equals(Reference);
    }

    public override bool Equals(object? obj)
    {
        if (obj is VoxExternalObject extrObj) return ObjEq(extrObj);
        if (obj is VoxValue { Type: VoxValueType.Object } voxValue)
        {
            if (voxValue.Reference is VoxExternalObject externalObject)
            {
                return ObjEq(externalObject);
            }
        }

        return false;
    }
}

public class VoxTypeInstance : ScriptObject
{
    public readonly VoxType Prototype;
    
    public override VoxValue GetValue(VoxValue key)
    {
        throw new NotImplementedException();
    }

    public override void SetValue(VoxValue key, VoxValue value)
    {
        throw new NotImplementedException();
    }

    public override bool HasKey(VoxValue key)
    {
        throw new NotImplementedException();
    }
}