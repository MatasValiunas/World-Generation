using System.Collections.Generic;
using UnityEngine;

public static class White
{
    public static float[,] Noise(int width, int length, float maxHeight, int seed)
    {
        int size = width * length;
        int cycles = Mathf.CeilToInt((float)size / maxHeight);

        List<float> heightList = new();
        for (int i = 0; i < cycles; i++)
        {
            for (int height = 1; height <= maxHeight; height++)
            {
                heightList.Add(height);
            }
        }

        ShuffleList(heightList, seed);  
        
        float[,] heightmap = new float[width, length];
        for (int x = 0, i = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                heightmap[x, z] = heightList[i++];
            }
        }

        return heightmap;
    }

    // Fisher-Yates shuffle algorithm
    static void ShuffleList(List<float> list, int seed)
    {
        MethodHelper.SetRandomizerSeed(seed);

        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}