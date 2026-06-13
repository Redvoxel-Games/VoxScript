namespace VoxScript;

public class Helper
{
    public static List<TArray> ArrayToList<TArray>(TArray[] array)
    {
        List<TArray> list = [];
        list.AddRange(array);
        return list;
    }
}