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
    public void SetValue(IdentifierExpression accessor, VoxValue value, bool forceThisScope=false)
    {
        var rawPath = accessor.Path;
        List<VoxValue> path = [];
        foreach (var expr in rawPath)
        {
            path.Add(ExpressionMath.EvaluateValue(expr, this));
        }

        if (path[0].ToString() == "_") return;
        
        var first = path.FirstOrDefault().ToString();
        
        var scopeToSetIn = forceThisScope ? this : BackPropagate(first) ?? this;

        if (path.Count == 1)
        {
            scopeToSetIn._values[path[0].ToString()] = value;
            return;
        }
        
        if (scopeToSetIn._values.TryGetValue(first, out var existingValue))
        {
            var result = ExploreTree(existingValue, path);
            var objectToSetIn = result.returned;

            if (objectToSetIn.Type == VoxValueType.Object)
            {
                var obj = (ScriptObject?)objectToSetIn.Reference;
                if (obj != null)
                {
                    if (obj.GetValue(path.Last()).Type == VoxValueType.ExternalValue)
                    {
                        var extrObj = obj.GetValue(path.Last()).Reference;
                        if (extrObj is ExternalField { readOnly: false } field)
                        {
                            object nativeValue = null!;
                            if (field.fieldInfo.FieldType == typeof(string)) nativeValue = value.ToString();
                            if (field.fieldInfo.FieldType == typeof(double)) nativeValue = value.Value.NumberValue;
                            if (field.fieldInfo.FieldType == typeof(float)) nativeValue = value.Value.NumberValue;
                            if (field.fieldInfo.FieldType == typeof(int)) nativeValue = value.Value.NumberValue;
                            if (field.fieldInfo.FieldType == typeof(long)) nativeValue = value.Value.NumberValue;
                            if (field.fieldInfo.FieldType == typeof(bool)) nativeValue = value;
                            field.fieldInfo.SetValue(field.reference, nativeValue);
                        }
                        else if (extrObj is ExternalProperty prop)
                        {
                            if (!prop.propertyInfo.CanWrite) throw new AccessViolationException("Attempt to set readonly key.");
                            object nativeValue = null!;
                            if (prop.propertyInfo.PropertyType == typeof(string)) nativeValue = value.ToString();
                            if (prop.propertyInfo.PropertyType == typeof(double)) nativeValue = value.Value.NumberValue;
                            if (prop.propertyInfo.PropertyType == typeof(float)) nativeValue = value.Value.NumberValue;
                            if (prop.propertyInfo.PropertyType == typeof(int)) nativeValue = value.Value.NumberValue;
                            if (prop.propertyInfo.PropertyType == typeof(long)) nativeValue = value.Value.NumberValue;
                            if (prop.propertyInfo.PropertyType == typeof(bool)) nativeValue = value;
                            prop.propertyInfo.SetValue(prop.reference, nativeValue);
                        }
                    }
                    else
                    {
                        obj.SetValue(path.Last(), value);
                    }
                }
            }
        }
    }

    private record TreeResult(VoxValue returned, bool exitedAt0);

    private TreeResult ExploreTree(VoxValue root, List<VoxValue> accessor)
    {
        while (true)
        {
            // Explore object/array tree to get the last possible data holder
            var withoutFirst = accessor[1..];

            if (withoutFirst.Count <= 1)
            {
                return new TreeResult(root, withoutFirst.Count == 0);
            }

            var newFirst = withoutFirst[0];

            if (root.Type != VoxValueType.Object) return new TreeResult(VoxValue.Null, false);
            var obj = (ScriptObject?)root.Reference;
            if (obj == null) return new TreeResult(VoxValue.Null, false);

            VoxValue newValue = obj.GetValue(newFirst);
            root = newValue;
            accessor = withoutFirst;
        }
    }

    /// <summary>
    /// Gets a value, finds the lowest possible scope to get from.
    /// </summary>
    /// <param name="accessor">Accessor to get from.</param>
    /// <returns>The value retrieved, null if none is found.</returns>
    public VoxValue GetValue(IdentifierExpression accessor)
    {
        var rawPath = accessor.Path;
        List<VoxValue> path = [];
        foreach (var expr in rawPath)
        {
            var val = ExpressionMath.EvaluateValue(expr, this);
            path.Add(val);
        }
        
        if (path[0].ToString() == "_") return VoxValue.Null;
        
        var first = path.FirstOrDefault();

        if (_values.TryGetValue(first, out VoxValue value))
        {
            if (value.Type == VoxValueType.ExternalValue)
            {
                var extrObj = value.Reference;
                if (extrObj is ExternalField field)
                {
                    return VoxValue.FromObject(field.fieldInfo.GetValue(field.reference));
                }
                if (extrObj is ExternalProperty prop)
                {
                    if (!prop.propertyInfo.CanRead) throw new AccessViolationException("Attempt to read non-readable value.");
                    return VoxValue.FromObject(prop.propertyInfo.GetValue(prop.reference));
                }
            }

            var result = ExploreTree(value, path);
            var objectToGetIn = result.returned;

            if (objectToGetIn.Type == VoxValueType.Object)
            {
                var obj = (ScriptObject?)objectToGetIn.Reference;
                if (obj != null)
                {
                    if (obj.GetValue(path.Last()).Type == VoxValueType.ExternalValue)
                    {
                        var extrObj = obj.GetValue(path.Last()).Reference;
                        if (extrObj is ExternalField field)
                        {
                            var val = field.fieldInfo.GetValue(field.reference);
                            return VoxValue.FromObject(val);
                        }
                        if (extrObj is ExternalProperty prop)
                        {
                            var val = prop.propertyInfo.GetValue(prop.reference);
                            return VoxValue.FromObject(val);
                        }
                    }
                    else if (result.exitedAt0)
                    {
                        return obj;
                    }
                    else
                    {
                        var voxValue = obj.GetValue(path.Last());
                        return voxValue;
                    }
                }
            }
            return value;
        }

        return ParentScope?.GetValue(accessor) ?? VoxValue.Null;
    }
}