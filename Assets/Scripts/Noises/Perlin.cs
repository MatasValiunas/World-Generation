using UnityEngine;


public static class Perlin
{
    public static float[,] Noise(int width, int length, float maxHeight, float scale, int seed)
    {
        HelperMethods.SetRandomizerSeed(seed);
        
        // Generate gradient vectors
        Vector2[,] gradients = new Vector2[width + 1, length + 1];
        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y <= length; y++)
            {
                float angle = Random.Range(0f, 2f * Mathf.PI);
                gradients[x, y] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }
        }

        float[,] noise = new float[width, length];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                float fx = x / scale;
                float fy = y / scale;

                int cellX = Mathf.FloorToInt(fx);
                int cellY = Mathf.FloorToInt(fy);

                float localX = fx - cellX;
                float localY = fy - cellY;

                // Dot products from 4 corners
                float d00 = Vector2.Dot(gradients[cellX, cellY], new Vector2(localX, localY));
                float d10 = Vector2.Dot(gradients[cellX + 1, cellY], new Vector2(localX - 1, localY));
                float d01 = Vector2.Dot(gradients[cellX, cellY + 1], new Vector2(localX, localY - 1));
                float d11 = Vector2.Dot(gradients[cellX + 1, cellY + 1], new Vector2(localX - 1, localY - 1));

                // Interpolation weights (smoothstep)
                float u = Smoothstep(localX);
                float v = Smoothstep(localY);

                float interpX1 = Mathf.Lerp(d00, d10, u);
                float interpX2 = Mathf.Lerp(d01, d11, u);
                float interpY = Mathf.Lerp(interpX1, interpX2, v);

                noise[x, y] = interpY;
            }
        }

        Algorithms.NormalizeValues(noise, maxHeight);

        return noise;
    }

    private static float Smoothstep(float t)
    {
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }
}