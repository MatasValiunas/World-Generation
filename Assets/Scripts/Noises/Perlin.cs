using UnityEngine;

public static class Perlin
{
    public static float[,] Noise(int width, int length, float maxHeight, int seed, float scale)
    {
        MethodHelper.SetRandomizerSeed(seed);
        
        // Generate gradient vectors
        int gradientWidth = Mathf.CeilToInt(width / scale) + 1;
        int gradientLength = Mathf.CeilToInt(length / scale) + 1;
        Vector2[,] gradients = new Vector2[gradientWidth, gradientLength];

        for (int x = 0; x < gradientWidth; x++)
        {
            for (int z = 0; z < gradientLength; z++)
            {
                float angle = Random.Range(0f, 2f * Mathf.PI);
                gradients[x, z] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }
        }

        float[,] heightmap = new float[width, length];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                float fx = x / scale;
                float fz = z / scale;

                int cellX = Mathf.FloorToInt(fx);
                int cellZ = Mathf.FloorToInt(fz);

                float localX = fx - cellX;
                float localZ = fz - cellZ;

                // Dot products from 4 corners
                float d00 = Vector2.Dot(gradients[cellX, cellZ], new Vector2(localX, localZ));
                float d10 = Vector2.Dot(gradients[cellX + 1, cellZ], new Vector2(localX - 1, localZ));
                float d01 = Vector2.Dot(gradients[cellX, cellZ + 1], new Vector2(localX, localZ - 1));
                float d11 = Vector2.Dot(gradients[cellX + 1, cellZ + 1], new Vector2(localX - 1, localZ - 1));

                // Interpolation
                float u = Smoothstep(localX);
                float v = Smoothstep(localZ);

                float interpX1 = Mathf.Lerp(d00, d10, u);
                float interpX2 = Mathf.Lerp(d01, d11, u);
                float interpY = Mathf.Lerp(interpX1, interpX2, v);

                heightmap[x, z] = interpY;
            }
        }

        MethodHelper.NormalizeValues(heightmap, maxHeight);

        return heightmap;
    }

    static float Smoothstep(float t)
    {
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }
}