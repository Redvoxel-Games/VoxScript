using System.Collections;
using VoxScript.Integration;

namespace VoxScript.Runtime;

[ExposeToScript(ContextType.Joined)]
public class VoxObject : IEnumerable<KeyValuePair<VoxValue, VoxValue>>
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
}