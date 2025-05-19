using System.Collections.Generic;
using UnityEngine;

public static class MethodHelper
{
    public static void SetRandomizerSeed(int seed)
    {
        if (seed == 0)
        {
            seed = System.Environment.TickCount % 100;     // random seed
            Debug.Log($"Seed: {seed}");
        }

        Random.InitState(seed);
    }

    public static void RandomizeSeed(ref int seed)
    {
        if (seed == 0)
        {
            seed = System.Environment.TickCount % 100;     // random seed
            Debug.Log($"Seed: {seed}");
        }
    }

    public static void NormalizeValues(float[,] array, float max = 1, float min = 0)
    {
        int width = array.GetLength(0), length = array.GetLength(1);
        float minVal = float.MaxValue, maxVal = float.MinValue;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < length; j++)
            {
                if (array[i, j] < minVal) minVal = array[i, j];
                if (array[i, j] > maxVal) maxVal = array[i, j];
            }
        }

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < length; j++)
            {
                array[i, j] = min + (array[i, j] - minVal) / (maxVal - minVal) * (max - min);
            }
        }
    }

    public static float[,] ResizeArray(float[,] array, int size)
    {
        float[,] newArray = new float[size, size];

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                newArray[i, j] = array[i, j];
            }
        }

        return newArray;
    }

    public static float[,] MultiplyArrays(List<float[,]> arrays)
    {
        int x = arrays[0].GetLength(0), y = arrays[0].GetLength(1);
        float[,] result = arrays[0];

        for (int a = 1; a < arrays.Count; a++)
        {
            for (int i = 0; i < x; i++)
            {
                for (int j = 0; j < y; j++)
                {
                    result[i, j] *= arrays[a][i, j];
                }
            }
        }

        return result;
    }
}