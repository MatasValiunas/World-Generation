using UnityEngine;
public static class DiamondSquare
{
    public static float[,] Noise(int size, float maxHeight, int seed, float roughness, bool resize, float scale = 1.0f)
    {
        // Validate and adjust size to be (2^n) + 1
        int originalSize = size;
        for (int n = 0; (1 << n) < originalSize - 1; n++)
        {
            size = (1 << n + 1) + 1;
        }

        if (size != originalSize)
        {
            Debug.Log($"Size adjusted from {originalSize} to {size} to satisfy 2^n + 1 requirement");
        }
        MethodHelper.SetRandomizerSeed(seed);
        // Create heightmap
        float[,] heightmap = new float[size, size];
        // Initialize corners with random values
        heightmap[0, 0] = Random.Range(-1f, 1f);
        heightmap[0, size - 1] = Random.Range(-1f, 1f);
        heightmap[size - 1, 0] = Random.Range(-1f, 1f);
        heightmap[size - 1, size - 1] = Random.Range(-1f, 1f);
        // Run diamond-square algorithm
        for (int step = size - 1, i = 1; step > 1; step /= 2, i++)
        {
            // Apply scale to the noise calculation
            float scaledNoise = Mathf.Pow(roughness, i) * (1.0f / scale);

            DiamondStep(heightmap, step, scaledNoise, size);
            SquareStep(heightmap, step, scaledNoise, size);
        }
        if (resize) { heightmap = MethodHelper.ResizeArray(heightmap, originalSize); }

        MethodHelper.NormalizeValues(heightmap, maxHeight);
        return heightmap;
    }
    static void DiamondStep(float[,] heightmap, int step, float noise, int size)
    {
        for (int x = 0; x < size - 1; x += step)
        {
            for (int z = 0; z < size - 1; z += step)
            {
                // Calculate midpoint of the square
                int midX = x + step / 2;
                int midZ = z + step / 2;
                // Average the four corners and add random displacement
                float avg = (
                    heightmap[x, z] +               // Top-left
                    heightmap[x + step, z] +        // Top-right
                    heightmap[x, z + step] +        // Bottom-left
                    heightmap[x + step, z + step]   // Bottom-right
                ) / 4.0f;
                heightmap[midX, midZ] = avg + (Random.Range(-1f, 1f) * noise);
            }
        }
    }
    static void SquareStep(float[,] heightmap, int step, float noise, int size)
    {
        int halfStep = step / 2;
        // Process all square step points by generating them from the grid intersections
        for (int x = 0; x < size; x += step)
        {
            for (int z = 0; z < size; z += step)
            {
                // Calculate horizontal midpoint
                if (x + halfStep < size) { CalculateSquarePoint(heightmap, x + halfStep, z, halfStep, noise, size); }
                // Calculate vertical midpoint
                if (z + halfStep < size) { CalculateSquarePoint(heightmap, x, z + halfStep, halfStep, noise, size); }
            }
        }
    }
    static void CalculateSquarePoint(float[,] heightmap, int x, int z, int halfStep, float noise, int size)
    {
        int count = 0;
        float sum = 0;
        if (x - halfStep >= 0 && x - halfStep < size) { sum += heightmap[x - halfStep, z]; count++; }
        if (x + halfStep >= 0 && x + halfStep < size) { sum += heightmap[x + halfStep, z]; count++; }
        if (z - halfStep >= 0 && z - halfStep < size) { sum += heightmap[x, z - halfStep]; count++; }
        if (z + halfStep >= 0 && z + halfStep < size) { sum += heightmap[x, z + halfStep]; count++; }
        heightmap[x, z] = (sum / count) + (Random.Range(-1f, 1f) * noise);
    }
}