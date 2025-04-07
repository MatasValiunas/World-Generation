using UnityEngine;

public static class DiamondSquare
{
    /// <summary>
    /// Generates a heightmap using the Diamond-Square algorithm
    /// </summary>
    /// <param name="size">Size of the heightmap (must be 2^n + 1)</param>
    /// <param name="roughness">Controls terrain roughness (0.1-1.0)</param>
    /// <param name="maxHeight">Overall height multiplier</param>
    /// <param name="seed">Random seed for reproducible results (0 uses a random seed)</param>
    /// <returns>2D float array containing the generated heightmap</returns>
    public static float[,] GenerateHeightmap(int size, float maxHeight, float roughness, int seed)
    {
        // Validate and adjust size to be (2^n) + 1
        int n = 0;
        int originalSize = size;
        while ((1 << n) < size - 1)
        {
            n++;
        }
        size = (1 << n) + 1;
        
        // Log a warning if the size was adjusted
        if (size != originalSize)
        {
            Debug.LogWarning($"DiamondSquare: Size adjusted from {originalSize} to {size} to satisfy 2^n + 1 requirement");
        }

        HelperMethods.SetRandomizerSeed(seed);

        // Create heightmap
        float[,] heightmap = new float[size, size];
        
        // Initialize corners with random values
        heightmap[0, 0] = Random.Range(-1f, 1f);
        heightmap[0, size - 1] = Random.Range(-1f, 1f);
        heightmap[size - 1, 0] = Random.Range(-1f, 1f);
        heightmap[size - 1, size - 1] = Random.Range(-1f, 1f);

        float scale = 1;
        // Run diamond-square algorithm
        for (int step = size - 1; step > 1; step /= 2)
        {
            DiamondStep(heightmap, step, scale, size);

            SquareStep(heightmap, step, scale, size);

            scale *= roughness;
        }

        // Restore original size heightmap
        float[,] originalSizeHeightmap = new float[originalSize, originalSize];
        for (int x = 0; x < originalSize; x++)
        {
            for (int y = 0; y < originalSize; y++)
            {
                originalSizeHeightmap[x, y] = heightmap[x, y];
            }
        }

        Algorithms.NormalizeValues(originalSizeHeightmap, maxHeight);

        return originalSizeHeightmap;
    }

    private static void DiamondStep(float[,] heightMap, int step, float scale, int size)
    {
        for (int x = 0; x < size - 1; x += step)
        {
            for (int y = 0; y < size - 1; y += step)
            {
                // Calculate midpoint of the square
                int midX = x + step / 2;
                int midY = y + step / 2;

                // Average the four corners and add random displacement
                float avg = (
                    heightMap[x, y] +               // Top-left
                    heightMap[x + step, y] +        // Top-right
                    heightMap[x, y + step] +        // Bottom-left
                    heightMap[x + step, y + step]   // Bottom-right
                ) / 4.0f;

                // Add random displacement scaled by current roughness
                heightMap[midX, midY] = avg + (Random.Range(-1f, 1f) * scale);
            }
        }
    }

    private static void SquareStep(float[,] heightMap, int step, float scale, int size)
    {
        int halfStep = step / 2;
        
        for (int x = 0; x < size; x += halfStep)
        {
            for (int y = 0; y < size; y += step / 2)
            {
                // Only process points that haven't been calculated yet
                if ((x % step != 0 || y % step != 0) && (x % step == 0 || y % step == 0))
                {
                    // Count valid points and sum their values
                    int count = 0;
                    float sum = 0;

                    // Check all four adjacent diamond midpoints (if they exist)
                    if (x - halfStep >= 0 && x - halfStep < size) { sum += heightMap[x - halfStep, y]; count++; }
                    if (x + halfStep >= 0 && x + halfStep < size) { sum += heightMap[x + halfStep, y]; count++; }
                    if (y - halfStep >= 0 && y - halfStep < size) { sum += heightMap[x, y - halfStep]; count++; }
                    if (y + halfStep >= 0 && y + halfStep < size) { sum += heightMap[x, y + halfStep]; count++; }

                    // Set point to average plus random displacement
                    heightMap[x, y] = (sum / count) + (Random.Range(-1f, 1f) * scale);
                }
            }
        }
    }
}