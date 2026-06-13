namespace VoxScript.Tree;

public abstract class CallResult(string message)
{
    public readonly string Message = message;

    public class Success() : CallResult("");
    public class Warning(string message) : CallResult(message);
    public class Error(string message) : CallResult(message);
}