using System.Diagnostics;
using VoxScript.Integration;
using VoxScript.Runtime;

namespace VoxScript.Tree;

public class Scope(Scope? parent=null)
{
    private readonly Dictionary<string, VoxValue> _values = new();
    public Scope? ParentScope { get; internal set; } = parent;

    internal void Clear()
    {
        _values.Clear();
    }

    public void PrintValues()
    {
        foreach (var pair in _values)
        {
            Console.WriteLine($"{pair.Key} = {pair.Value}");
        }
    }

    /// <summary>
    /// Propagate request backwards to get the lowest scope that contains <paramref name="accessor" />.
    /// </summary>
    /// <param name="accessor">Accessor to check for.</param>
    /// <returns>Target scope, null if none is found.</returns>
    private Scope? BackPropagate(string accessor)
    {
        if (_values.TryGetValue(accessor, out var value))
        {
            return this;
        }
        return ParentScope?.BackPropagate(accessor);
    }

    /// <summary>
    /// Set a value, if a higher scope has the same accessor it is set there instead.
    /// </summary>
    /// <param name="accessor">Accessor to set to.</param>
    /// <param name="value">Value to set to <paramref name="accessor"/></param>
    /// <param name="forceThisScope">Whether to force the value to be set in this scope.</param>
    public void SetValue(IdentifierExpression accessor, VoxValue value)
    {
        if (accessor.ToString() == "_") return;
        
        var valueToSet = ExpressionMath.EvaluateIdentifier(accessor, this);
        
        var extrObj = valueToSet.Reference;
        if (extrObj is ExternalField { readOnly: false } field)
        {
            object nativeValue = null!;
            if (field.fieldInfo.FieldType == typeof(string)) nativeValue = value.ToString();
            if (field.fieldInfo.FieldType == typeof(double)) nativeValue = value.Value.NumberValue;
            if (field.fieldInfo.FieldType == typeof(float)) nativeValue = (float)value.Value.NumberValue;
            if (field.fieldInfo.FieldType == typeof(int)) nativeValue = (int)value.Value.NumberValue;
            if (field.fieldInfo.FieldType == typeof(long)) nativeValue = (long)value.Value.NumberValue;
            if (field.fieldInfo.FieldType == typeof(bool)) nativeValue = value;
            field.fieldInfo.SetValue(field.reference, nativeValue);
        }
        else if (extrObj is ExternalProperty prop)
        {
            if (!prop.propertyInfo.CanWrite) throw new AccessViolationException("Attempt to set readonly key.");
            object nativeValue = null!;
            if (prop.propertyInfo.PropertyType == typeof(string)) nativeValue = value.ToString();
            if (prop.propertyInfo.PropertyType == typeof(double)) nativeValue = value.Value.NumberValue;
            if (prop.propertyInfo.PropertyType == typeof(float)) nativeValue = (float)value.Value.NumberValue;
            if (prop.propertyInfo.PropertyType == typeof(int)) nativeValue = (int)value.Value.NumberValue;
            if (prop.propertyInfo.PropertyType == typeof(long)) nativeValue = (long)value.Value.NumberValue;
            if (prop.propertyInfo.PropertyType == typeof(bool)) nativeValue = value;

            prop.propertyInfo.SetValue(prop.reference, nativeValue);
        }
        else
        {
            var objToCreateIn = ExpressionMath.EvaluateIdentifier(accessor, this, true);
            if (objToCreateIn.Type == VoxValueType.Null && accessor.Path.Count == 1)
            {
                _values[ExpressionMath.EvaluateValue(accessor.Path.Last(), this)] = value;
            }
            else if (accessor.Path.Count == 1)
            {
                var key = ExpressionMath.EvaluateValue(accessor.Path.Last(), this);
                var scopeToSetIn = BackPropagate(key);
                if (scopeToSetIn != null)
                {
                    scopeToSetIn._values[key] = value;
                }
            }
            else if (objToCreateIn.Type == VoxValueType.Object)
            {
                var obj = objToCreateIn.Reference as ScriptObject;
                if (obj != null)
                {
                    obj.SetValue(ExpressionMath.EvaluateValue(accessor.Path.Last(), this), value);
                }
            }
            else throw new Exception("Attempt to set key in type " + objToCreateIn.Type);
        }
    }

    /// <summary>
    /// Gets a value, finds the lowest possible scope to get from.
    /// </summary>
    /// <param name="accessor">Accessor to get from.</param>
    /// <returns>The value retrieved, null if none is found.</returns>
    public VoxValue GetValue(string name)
    {
        if (name == "_") return VoxValue.Null;
        
        if (_values.TryGetValue(name, out VoxValue value))
            return value;

        return ParentScope?.GetValue(name) ?? VoxValue.Null;
    }

    public bool IsGlobal => ParentScope == null;
}