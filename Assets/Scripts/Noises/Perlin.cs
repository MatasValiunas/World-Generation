using UnityEngine;

public static class Perlin
{
    public static float[,] Noise(int width, int length, float maxHeight, int seed, float scale, float variation)
    {
        // Set seed once at the beginning
        MethodHelper.SetRandomizerSeed(seed);

        float[,] heightmap = new float[width, length];

        // Calculate the scaled dimensions for consistency
        float scaledWidth = width / scale;
        float scaledLength = length / scale;

        // Use a fixed number of grid cells regardless of scale
        // This makes the noise pattern more consistent when changing scale
        int gridSize = 256;
        Vector2[,] gradients = GenerateGradientGrid(gridSize, gridSize, seed, variation);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                // Map coordinates to noise space (0 to gridSize)
                float nx = (x / (float)width) * gridSize;
                float nz = (z / (float)length) * gridSize;

                // Apply scale to frequency, not to grid positioning
                nx /= scale;
                nz /= scale;

                // Calculate the noise value
                heightmap[x, z] = PerlinNoise2D(nx, nz, gradients, gridSize);
            }
        }

        // Normalize the values to the desired height range
        MethodHelper.NormalizeValues(heightmap, maxHeight);

        return heightmap;
    }

    private static Vector2[,] GenerateGradientGrid(int width, int height, int seed, float variation)
    {
        Vector2[,] gradients = new Vector2[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Use hash function of coordinates with seed to ensure
                // same positions always get same gradients
                int hashVal = HashCoordinates(x, y, seed);
                float angle = (hashVal % 360) * Mathf.Deg2Rad;
                //gradients[x, y] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                float magnitude = Random.Range(1 - variation, 1);//(hashVal % 100) / 25f + 0.5f; // Generate a random magnitude between 0.5 and 4.5
                gradients[x, y] = new Vector2(Mathf.Cos(angle) * magnitude, Mathf.Sin(angle) * magnitude);
            }
        }

        return gradients;
    }

    private static int HashCoordinates(int x, int y, int seed)
    {
        // Basic hash function that combines coordinates with seed
        // This ensures the same position always gets the same gradient
        int hash = seed;
        hash = hash * 31 + x;
        hash = hash * 31 + y;
        hash = hash ^ (hash >> 13);
        hash = hash * (hash * hash * 15731 + 789221) + 1376312589;
        return hash & 0x7fffffff; // Make positive
    }

    private static float PerlinNoise2D(float x, float y, Vector2[,] gradients, int gridSize)
    {
        // Get grid cell coordinates
        int x0 = Mathf.FloorToInt(x) % gridSize;
        int y0 = Mathf.FloorToInt(y) % gridSize;
        int x1 = (x0 + 1) % gridSize;
        int y1 = (y0 + 1) % gridSize;

        // Get local coordinates within cell (0-1)
        float sx = x - Mathf.Floor(x);
        float sy = y - Mathf.Floor(y);

        // Dot products with corner gradients
        float n00 = Vector2.Dot(gradients[x0, y0], new Vector2(sx, sy));
        float n10 = Vector2.Dot(gradients[x1, y0], new Vector2(sx - 1, sy));
        float n01 = Vector2.Dot(gradients[x0, y1], new Vector2(sx, sy - 1));
        float n11 = Vector2.Dot(gradients[x1, y1], new Vector2(sx - 1, sy - 1));

        // Smoothing
        float u = Smoothstep(sx);
        float v = Smoothstep(sy);

        // Bilinear interpolation
        float nx0 = Mathf.Lerp(n00, n10, u);
        float nx1 = Mathf.Lerp(n01, n11, u);
        float nxy = Mathf.Lerp(nx0, nx1, v);

        // Normalize result to approximate range [-1, 1]
        return nxy * 0.5f + 0.5f;
    }

    private static float Smoothstep(float t)
    {
        // Improved smoothstep function (Perlin's improved version)
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }
}