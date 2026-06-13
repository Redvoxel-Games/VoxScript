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
        
        var first = path.FirstOrDefault().ToString();
        
        var scopeToSetIn = forceThisScope ? this : BackPropagate(first) ?? this;

        if (path.Count == 1)
        {
            scopeToSetIn._values[path[0].ToString()] = value;
            return;
        }
        
        if (scopeToSetIn._values.TryGetValue(first, out var existingValue))
        {
            var objectToSetIn = ExploreTree(existingValue, path);

            if (objectToSetIn.Type == VoxValueType.Object)
            {
                var obj = (VoxObject?)objectToSetIn.Reference;
                if (obj != null)
                {
                    if (obj[path.Last()].Type == VoxValueType.ExternalValue)
                    {
                        var fieldObj = obj[path.Last()].Reference;
                        if (fieldObj is ExternalField { readOnly: false } field)
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
                    }
                    else
                    {
                        obj[path.Last()] = value;
                    }
                }
            }
        }
    }

    private VoxValue ExploreTree(VoxValue root, List<VoxValue> accessor)
    {
        while (true)
        {
            // Explore object/array tree to get the last possible data holder
            var withoutFirst = accessor[1..];

            if (withoutFirst.Count <= 1)
            {
                return root;
            }

            var newFirst = withoutFirst[0];

            if (root.Type != VoxValueType.Object) return VoxValue.Null;
            var obj = (VoxObject?)root.Reference;
            if (obj == null) return VoxValue.Null;

            VoxValue newValue = obj[newFirst];
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
            path.Add(ExpressionMath.EvaluateValue(expr, this));
        }
        
        var first = path.FirstOrDefault();

        if (_values.TryGetValue(first, out VoxValue value))
        {
            if (value.Type == VoxValueType.ExternalValue)
            {
                var fieldObj = value.Reference;
                if (fieldObj is ExternalField field)
                {
                    return VoxValue.FromObject(field.fieldInfo.GetValue(field.reference));
                }
            }
            
            var objectToGetIn = ExploreTree(value, path);

            if (objectToGetIn.Type == VoxValueType.Object)
            {
                var obj = (VoxObject?)objectToGetIn.Reference;
                if (obj != null)
                {
                    if (obj[path.Last()].Type == VoxValueType.ExternalValue)
                    {
                        var fieldObj = obj[path.Last()].Reference;
                        if (fieldObj is ExternalField field)
                        {
                            return VoxValue.FromObject(field.fieldInfo.GetValue(field.reference));
                        }
                    }
                    else
                    {
                        return obj[path.Last()];
                    }
                }
            }
            return value;
        }

        return ParentScope?.GetValue(accessor) ?? VoxValue.Null;
    }
}