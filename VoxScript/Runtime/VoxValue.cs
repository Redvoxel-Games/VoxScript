using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using VoxScript.Integration;

namespace VoxScript.Runtime;

public enum VoxValueType
{
    Null,
    Number, 
    String,
    Boolean,
    Object,
    Function,
    ExternalValue,
    Return, // Cancels current function (or script if it doesn't path to a function) execution and carries a single value
    Break, // Breaks highest loop, Carries no value
    Continue, // Skips highest loop, Carries no value
    End, // Cancels current scope execution, Carries no value.
}

[StructLayout(LayoutKind.Auto)]
public readonly struct VoxValue : IEquatable<VoxValue>
{
    public struct ValueUnion
    {
        public double NumberValue;
        public bool BooleanValue;
        public string StringValue;
    }
    
    public static VoxValue Null => new(VoxValueType.Null, default);

    public readonly VoxValueType Type;
    public readonly ValueUnion Value;
    public readonly object? Reference;

    public VoxValue(VoxValueType type, ValueUnion value, object? reference=null)
    {
        Type = type;
        Value = value;
        Reference = reference;
    }

    public static VoxValue Number(double number)
    {
        return new VoxValue(VoxValueType.Number, new ValueUnion { NumberValue = number });
    }
    public static VoxValue String(string stringValue)
    {
        return new VoxValue(VoxValueType.String, new ValueUnion { StringValue = stringValue });
    }
    public static VoxValue Boolean(bool boolean)
    {
        return new VoxValue(VoxValueType.Boolean, new ValueUnion { BooleanValue = boolean });
    }
    public static VoxValue Object(VoxObject obj)
    {
        return new VoxValue(VoxValueType.Object, default, obj);
    }
    public static VoxValue Function(VoxFunctionBase func)
    {
        return new VoxValue(VoxValueType.Function, default, func);
    }

    public static VoxValue Return(VoxValue value)
    {
        return new VoxValue(VoxValueType.Return, default, value);
    }

    public static VoxValue Break()
    {
        return new VoxValue(VoxValueType.Break, default);
    }

    public static VoxValue Continue()
    {
        return new VoxValue(VoxValueType.Continue, default);
    }

    public override string ToString()
    {
        return Type switch
        {
            VoxValueType.Null => "null",
            VoxValueType.Number => Value.NumberValue.ToString(),
            VoxValueType.String => Value.StringValue,
            VoxValueType.Boolean => Value.BooleanValue ? "true" : "false",
            VoxValueType.Object => Reference is null ? "null" : Reference.ToString(),
            VoxValueType.Function => Reference is null ? "null" : "function",
            VoxValueType.ExternalValue => Reference is null ? "null" : Reference.ToString(),
            _ => "",
        };
    }

    public bool Equals(VoxValue other)
    {
        return Type == other.Type && Value.Equals(other.Value) && Equals(Reference, other.Reference);
    }

    public override bool Equals(object? obj)
    {
        return obj is VoxValue other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)Type, Value, Reference);
    }
    
    public static implicit operator VoxValue(double number) => Number(number);
    public static implicit operator VoxValue(string str) => String(str);
    public static implicit operator VoxValue(bool boolean) => Boolean(boolean);
    public static implicit operator VoxValue(VoxFunctionBase func) => Function(func);
    public static implicit operator VoxValue(VoxObject obj) => Object(obj);

    public static implicit operator double(VoxValue value) => value.Value.NumberValue;
    public static implicit operator string(VoxValue value) => value.ToString();

    public static implicit operator bool(VoxValue value)
    {
        if (value.Type == VoxValueType.Boolean)
        {
            return value.Value.BooleanValue;
        }
        return value.Type != VoxValueType.Null;
    }

    public static VoxValue FromObject(object? obj)
    {
        if (obj == null) return Null;
        if (obj is double number) return Number(number);
        if (obj is string str) return String(str);
        if (obj is bool boolean) return Boolean(boolean);
        if (obj is VoxFunctionBase func) return Function(func);
        if (obj is VoxObject obj2) return Object(obj2);
        if (obj is VoxValue value)
        {
            if (value.Type == VoxValueType.Number) return Number(value.Value.NumberValue);
            if (value.Type == VoxValueType.String) return String(value.Value.StringValue);
            if (value.Type == VoxValueType.Boolean) return Boolean(value.Value.BooleanValue);
            return value;
        }
        return ExposeToScriptAttribute.ToVoxObject(obj);
    }
    
    public static VoxValue operator +(VoxValue left, VoxValue right)
    {
        if (left.Type == VoxValueType.Number && right.Type == VoxValueType.Number)
        {
            return left.Value.NumberValue + right.Value.NumberValue;
        }
        if (left.Type == VoxValueType.String || right.Type == VoxValueType.String)
        {
            return left.ToString() + right.ToString();
        }
        return Null;
    }

    public object? GetPrimitive()
    {
        if (Type == VoxValueType.Number) return Value.NumberValue;
        if (Type == VoxValueType.String) return Value.StringValue;
        if (Type == VoxValueType.Boolean) return Value.BooleanValue;
        return null;
    }
}

public record ExternalField(FieldInfo fieldInfo, object reference, bool readOnly = false)
{
    public override string ToString()
    {
        return fieldInfo.Name;
    }
}