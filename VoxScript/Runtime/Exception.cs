namespace VoxScript.Runtime;

public class ValueException : Exception
{
    public ValueException(string message) : base(message) { }
    public ValueException(string message, Exception inner) : base(message, inner) { }
}