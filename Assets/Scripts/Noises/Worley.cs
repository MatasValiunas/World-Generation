using System.Collections.Generic;
using UnityEngine;

public static class Worley
{
    private const int PRIME_X = 501125321;
    private const int PRIME_Y = 1136930381;
    private const int PRIME_SEED = 198491317; // Added a prime for seed mixing

    public static float[,] Noise(int width, int length, float maxHeight, int seed, float scale, float cellDensity, float[] function)
    {
        MethodHelper.RandomizeSeed(ref seed);

        float[,] heightmap = new float[width, length];

        // Generate the raw noise values
        for (int z = 0; z < length; z++)
        {
            for (int x = 0; x < width; x++)
            {
                // Scale the coordinates
                float nx = x / scale;
                float nz = z / scale;

                float[] values = CalculateFunctions(nx, nz, function.Length, cellDensity, seed);

                // Combine the functions with their weights
                float value = 0;
                for (int i = 0; i < function.Length; i++)
                {
                    value += values[i] * function[i];
                }

                heightmap[x, z] = value;
            }
        }

        MethodHelper.NormalizeValues(heightmap, maxHeight);

        return heightmap;
    }

    /// <summary>
    /// Calculates the F1, F2, ..., Fn values for a given point.
    /// </summary>
    /// <returns>Array containing the requested F values</returns>
    private static float[] CalculateFunctions(float x, float z, int count, float cellDensity, int seed)
    {
        // Find the unit cube containing the point
        int xi = Mathf.FloorToInt(x);
        int zi = Mathf.FloorToInt(z);

        // Structure to store distances and compare them
        List<PointDistance> distances = new List<PointDistance>();

        // Check the current cube and neighboring cubes
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int curX = xi + dx;
                int curZ = zi + dy;

                // Generate a deterministic random seed for this cube based on position and global seed
                int cellSeed = Hash(curX, curZ, seed);
                
                // Use a more deterministic way to calculate number of points based on cellDensity and seed
                float randomValue = PseudoRandomFloat(cellSeed);
                int numPoints = Mathf.Max(1, Mathf.Min(9, Mathf.FloorToInt(randomValue * cellDensity + 1)));

                // Generate each feature point and calculate distance
                for (int i = 0; i < numPoints; i++)
                {
                    // Generate consistent point positions based on the cell seed and point index
                    int pointSeed = Hash(cellSeed, i);
                    float px = curX + PseudoRandomFloat(pointSeed);
                    float pz = curZ + PseudoRandomFloat(Hash(pointSeed, 1));

                    // Calculate Euclidean distance squared
                    float dx2 = x - px;
                    float dz2 = z - pz;
                    float distSquared = dx2 * dx2 + dz2 * dz2;

                    // Add to our list of distances
                    distances.Add(new PointDistance { DistanceSquared = distSquared, PointID = pointSeed });
                }
            }
        }

        // Sort by distance
        distances.Sort((a, b) => a.DistanceSquared.CompareTo(b.DistanceSquared));

        // Convert to F values (taking square root for actual distances)
        float[] result = new float[count];
        for (int i = 0; i < count && i < distances.Count; i++)
        {
            result[i] = Mathf.Sqrt(distances[i].DistanceSquared);
        }

        return result;
    }

    /// <summary>
    /// Structure to store distance information for sorting
    /// </summary>
    private struct PointDistance
    {
        public float DistanceSquared;
        public int PointID;
    }

    /// <summary>
    /// Hashes three integers to get a deterministic random seed
    /// </summary>
    private static int Hash(int x, int y, int seed = 0)
    {
        int hash = seed * PRIME_SEED;
        hash ^= PRIME_X * x;
        hash ^= PRIME_Y * y;
        
        // Mix the bits
        hash ^= hash >> 13;
        hash ^= hash << 17;
        hash ^= hash >> 5;
        
        return Mathf.Abs(hash);
    }

    /// <summary>
    /// Generates a pseudo-random float between 0 and 1 from an integer seed
    /// </summary>
    private static float PseudoRandomFloat(int seed)
    {
        // Simple but effective hash function for converting int to float in [0,1] range
        seed = (int)((seed * 747796405) + 2891336453);
        seed = ((seed >> 9) ^ seed) * 733738055;
        seed = ((seed >> 15) ^ seed) * 961748941;
        seed = (seed >> 17) ^ seed;
        
        // Convert to float in range [0, 1)
        float result = (seed & 0x7FFFFFFF) / (float)0x7FFFFFFF;
        return result;
    }


    public static float[] GetPattern(Pattern pattern)
    {
        switch (pattern)
        {
            case Pattern.F1:
                return new float[] { 1.0f };
            case Pattern.F2:
                return new float[] { 0, 1.0f };
            case Pattern.F2MinusF1:
                return new float[] { -1.0f, 1.0f };
            case Pattern.F1PlusF2:
                return new float[] { 1.0f, 1.0f };
            case Pattern.F1TimesF2:
                return new float[] { 1.0f, 0.5f };
            case Pattern.F3MinusF2:
                return new float[] { 0, -1.0f, 1.0f };
            case Pattern.Craters:
                return new float[] { 1.0f, -0.5f };
            case Pattern.Veins:
                return new float[] { -1.0f, 1.0f };
            default:
                return new float[] { 1.0f };
        }
    }

    public enum Pattern
    {
        F1,
        F2,
        F2MinusF1,
        F1PlusF2,
        F1TimesF2,
        F3MinusF2,
        Craters,
        Veins
    }
}