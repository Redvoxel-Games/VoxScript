using VoxScript.Runtime;

namespace VoxScript.Integration;

public interface IScriptIndexable
{
    public VoxValue GetScriptIndexResult(VoxValue indexer) { return VoxValue.Null; }
}