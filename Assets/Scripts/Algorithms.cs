using System.Collections.Generic;
using UnityEngine;


public static class Algorithms  
{
    // Fisher-Yates shuffle algorithm
    public static void ShuffleList(List<float> list, int seed = 0)
    {
        HelperMethods.SetRandomizerSeed(seed);

        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    public static void NormalizeValues(float[,] heightmap, float max = 1)
    {
        int width = heightmap.GetLength(0), height = heightmap.GetLength(1);
        float minVal = float.MaxValue, maxVal = float.MinValue;

        // Step 1: Find Min & Max Values
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (heightmap[x, y] < minVal) minVal = heightmap[x, y];
                if (heightmap[x, y] > maxVal) maxVal = heightmap[x, y];
            }
        }

        // Step 2: Normalize Values to 0-1 Range
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                heightmap[x, y] = (heightmap[x, y] - minVal) / (maxVal - minVal) * max;
            }
        }
    }

    public static int Hash(Vector2 coord)
    {
        unchecked
        {
            int x = Mathf.FloorToInt(coord.x);
            int y = Mathf.FloorToInt(coord.y);

            // FNV-1a like hashing with good bit mixing
            uint hash = 2166136261u;
            hash = (hash ^ (uint)x) * 16777619;
            hash = (hash ^ (uint)y) * 16777619;

            // Final avalanche step (mix bits further)
            hash ^= (hash >> 13);
            hash *= 0x5bd1e995;
            hash ^= (hash >> 15);

            return (int)(hash & 0x7FFFFFFF); // keep it positive
        }
    }
}
