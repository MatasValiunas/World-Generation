using System.Collections.Generic;
using UnityEngine;

public static class Worley
{
    // Constants for the hash function
    private const int PRIME_X = 501125321;
    private const int PRIME_Y = 1136930381;
    private const int PRIME_SEED = 198491317; // Added a prime for seed mixing


    /// <summary>
    /// Generates a 2D heightmap of Worley noise.
    /// </summary>
    /// <param name="width">Width of the heightmap</param>
    /// <param name="height">Height of the heightmap</param>
    /// <param name="cellDensity">Average number of feature points per cell (recommended 1-4)</param>
    /// <param name="scale">Scale factor for the noise</param>
    /// <param name="functions">Array containing weights for F1, F2, F3, etc. functions</param>
    /// <returns>2D array containing the heightmap values</returns>
    public static float[,] Noise(int width, int height, float maxHeight, int seed, float scale, float cellDensity,  float[] functions = null)
    {
        MethodHelper.SetRandomizerSeed(seed);

        // Default to just F1 if no functions are specified
        if (functions == null)
        {
            functions = new float[] { 1.0f };
        }

        float[,] heightmap = new float[width, height];

        // Generate the raw noise values
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Scale the coordinates
                float nx = x * scale / width;
                float ny = y * scale / height;

                // Get the F1, F2, F3, etc. values
                float[] values = CalculateFunctions(nx, ny, functions.Length, cellDensity, seed);

                // Combine the functions with their weights
                float value = 0;
                for (int i = 0; i < functions.Length; i++)
                {
                    value += values[i] * functions[i];
                }

                heightmap[x, y] = value;
            }
        }

        MethodHelper.NormalizeValues(heightmap, maxHeight);

        return heightmap;
    }

    /// <summary>
    /// Generates a fractal version of the Worley noise.
    /// </summary>
    /// <param name="width">Width of the heightmap</param>
    /// <param name="height">Height of the heightmap</param>
    /// <param name="cellDensity">Average number of feature points per cell</param>
    /// <param name="scale">Base scale factor for the noise</param>
    /// <param name="octaves">Number of octaves to generate</param>
    /// <param name="persistence">How much each octave contributes to the final result</param>
    /// <param name="functions">Array containing weights for F1, F2, F3, etc. functions</param>
    /// <returns>2D array containing the heightmap values</returns>
    public static float[,] GenerateFractalHeightmap(int width, int height, float maxHeight = 10, float cellDensity = 3.0f, float scale = 1.0f, int octaves = 5, float persistence = 0.5f, int seed = 0, float[] functions = null)
    {        
        // Default to just F1 if no functions are specified
        if (functions == null)
        {
            functions = new float[] { 1.0f };
        }

        float[,] heightmap = new float[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float amplitude = 1.0f;
                float frequency = 1.0f;
                float value = 0;
                float totalAmplitude = 0;

                // Add successive octaves
                for (int o = 0; o < octaves; o++)
                {
                    float nx = x * scale * frequency / width;
                    float ny = y * scale * frequency / height;

                    // Generate a different seed for each octave based on the base seed
                    int octaveSeed = Hash(seed, o, 0);
                    
                    // Get the F1, F2, F3, etc. values for this octave
                    float[] values = CalculateFunctions(nx, ny, functions.Length, cellDensity, octaveSeed);

                    // Combine the functions with their weights
                    float octaveValue = 0;
                    for (int i = 0; i < functions.Length; i++)
                    {
                        octaveValue += values[i] * functions[i];
                    }

                    value += octaveValue * amplitude;
                    totalAmplitude += amplitude;
                    amplitude *= persistence;
                    frequency *= 2;
                }

                // Normalize by total amplitude
                value /= totalAmplitude;
                heightmap[x, y] = value;
            }
        }

        MethodHelper.NormalizeValues(heightmap, maxHeight);

        return heightmap;
    }

    /// <summary>
    /// Calculates the F1, F2, ..., Fn values for a given point.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="count">Number of F values to calculate (F1, F2, etc.)</param>
    /// <param name="cellDensity">Average number of feature points per cell</param>
    /// <param name="seed">Seed value for randomization</param>
    /// <returns>Array containing the requested F values</returns>
    private static float[] CalculateFunctions(float x, float y, int count, float cellDensity, int seed)
    {
        // Find the unit cube containing the point
        int xi = Mathf.FloorToInt(x);
        int yi = Mathf.FloorToInt(y);

        // Structure to store distances and compare them
        List<PointDistance> distances = new List<PointDistance>();

        // Check the current cube and neighboring cubes
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int curX = xi + dx;
                int curY = yi + dy;

                // Generate a deterministic random seed for this cube based on position and global seed
                int cellSeed = Hash(curX, curY, seed);
                
                // Initialize random generator with this cell's seed
                MethodHelper.SetRandomizerSeed(cellSeed);
                
                // Use a more deterministic way to calculate number of points based on cellDensity and seed
                float randomValue = PseudoRandomFloat(cellSeed);
                int numPoints = Mathf.Max(1, Mathf.Min(9, Mathf.FloorToInt(randomValue * cellDensity + 1)));

                // Generate each feature point and calculate distance
                for (int i = 0; i < numPoints; i++)
                {
                    // Generate consistent point positions based on the cell seed and point index
                    int pointSeed = Hash(cellSeed, i, 0);
                    float px = curX + PseudoRandomFloat(pointSeed);
                    float py = curY + PseudoRandomFloat(Hash(pointSeed, 1, 0));

                    // Calculate Euclidean distance squared
                    float dx2 = x - px;
                    float dy2 = y - py;
                    float distSquared = dx2 * dx2 + dy2 * dy2;

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
    /// Simple structure to store distance information for sorting
    /// </summary>
    private struct PointDistance
    {
        public float DistanceSquared;
        public int PointID;
    }

    /// <summary>
    /// Hashes three integers to get a deterministic random seed
    /// </summary>
    private static int Hash(int x, int y, int seed)
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

    /// <summary>
    /// Utility method to create linear combinations of F1, F2, etc. for common patterns
    /// </summary>
    /// <param name="pattern">Preset pattern to use</param>
    /// <returns>Array with weights for the F functions</returns>
    public static float[] GetPresetPattern(WorleyPattern pattern)
    {
        switch (pattern)
        {
            case WorleyPattern.F1:
                return new float[] { 1.0f };
            case WorleyPattern.F2:
                return new float[] { 0, 1.0f };
            case WorleyPattern.F2MinusF1:
                return new float[] { -1.0f, 1.0f };
            case WorleyPattern.F1PlusF2:
                return new float[] { 1.0f, 1.0f };
            case WorleyPattern.F1TimesF2:
                // This requires special handling in the generator
                return new float[] { 1.0f, 0.5f };
            case WorleyPattern.F3MinusF2:
                return new float[] { 0, -1.0f, 1.0f };
            case WorleyPattern.Craters:
                return new float[] { 1.0f, -0.5f };
            case WorleyPattern.Veins:
                return new float[] { -1.0f, 1.0f };
            default:
                return new float[] { 1.0f };
        }
    }

    /// <summary>
    /// Common Worley noise patterns
    /// </summary>
    public enum WorleyPattern
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