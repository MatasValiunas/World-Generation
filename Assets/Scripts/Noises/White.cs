using System.Collections.Generic;
using UnityEngine;


public static class White
{
    public static float[,] Noise(int width, int length, int maxHeight, int seed, int minHeight = 1)
    {
        float[,] grid = new float[width, length];
        List<float> list = new();

        int size = width * length;
        int cycles = Mathf.CeilToInt((float)size / maxHeight);
        
        for (int i = 0; i < cycles; i++)
        {
            for (int value = minHeight; value <= maxHeight; value++)
            {
                list.Add(value);
            }
        }

        Algorithms.ShuffleList(list, seed);  
        
        // Convert the shuffled list into a 2D array
        for (int x = 0, i = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                grid[x, z] = list[i++];
            }
        }

        return grid;
    }
}