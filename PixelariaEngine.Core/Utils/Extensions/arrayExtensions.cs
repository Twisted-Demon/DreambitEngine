namespace PixelariaEngine;

public static class arrayExtensions
{
    public static T[,] To2DArray<T>(this T[] array, int width, int height)
    {
        var result = new T[width, height];
        
        for (var i = 0; i < array.Length; i++)
        {
            var x = i % width;
            var y = i / width;
            result[x, y] = array[i];
        }

        return result;
    }
}