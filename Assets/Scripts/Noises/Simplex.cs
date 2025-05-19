using UnityEngine;
public static class Simplex
{
    const float SKEW_FACTOR = 0.5f;
    const int HASH_PRIME = 289;
    const int HASH_MULT_1 = 51;
    const int HASH_MULT_2 = 34;
    const int HASH_OFFSET_1 = 2;
    const int HASH_OFFSET_2 = 10;
    const float DEFAULT_FALLOFF_POWER = 4;

    public static float[,] Noise(int width, int length, float maxHeight, int seed, float scale, float falloffRadius, int gradientCount, float variation)
    {
        Vector2[] gradients = GenerateGradients(gradientCount, seed, variation);

        float[,] heightmap = new float[width, length];
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                Vector2 point = new Vector2(x / scale, z / scale);
                heightmap[x, z] = PointNoise(point, gradients, falloffRadius);
            }
        }

        MethodHelper.NormalizeValues(heightmap, maxHeight);

        return heightmap;
    }

    public static float PointNoise(Vector2 point, Vector2[] gradients, float falloffRadius)
    {
        // 2. Transform input point to find simplex "base" i0
        Vector2 uv = new Vector2(point.x + point.y * SKEW_FACTOR, point.y);
        Vector2 gridCoord1 = new Vector2(Mathf.Floor(uv.x), Mathf.Floor(uv.y));
        Vector2 gridRelativePos = uv - gridCoord1;

        // Determine simplex corner offsets
        bool isFirstTriangle = gridRelativePos.y < gridRelativePos.x;
        Vector2 simplexOffset = isFirstTriangle ? new Vector2(1, 0) : new Vector2(0, 1);

        Vector2 gridCoord2 = gridCoord1 + simplexOffset;
        Vector2 gridCoord3 = gridCoord1 + Vector2.one;

        // 3. Convert simplex grid coords back to unskewed space
        Vector2 worldPos1 = new Vector2(gridCoord1.x - gridCoord1.y * SKEW_FACTOR, gridCoord1.y);
        Vector2 worldPos2 = new Vector2(worldPos1.x + simplexOffset.x - simplexOffset.y * SKEW_FACTOR, worldPos1.y + simplexOffset.y);
        Vector2 worldPos3 = new Vector2(worldPos1.x + SKEW_FACTOR, worldPos1.y + 1);

        // 4. Compute distances to corners
        Vector2 cornerDist1 = point - worldPos1;
        Vector2 cornerDist2 = point - worldPos2;
        Vector2 cornerDist3 = point - worldPos3;

        // 5. Hash
        Vector3 gridX = new Vector3(gridCoord1.x, gridCoord2.x, gridCoord3.x);
        Vector3 gridZ = new Vector3(gridCoord1.y, gridCoord2.y, gridCoord3.y);

        Vector3 hash = Mod(gridX, HASH_PRIME);
        hash = Mod(Vector3.Scale(hash * HASH_MULT_1 + new Vector3(HASH_OFFSET_1, HASH_OFFSET_1, HASH_OFFSET_1), hash) + gridZ, HASH_PRIME);
        hash = Mod(Vector3.Scale(hash * HASH_MULT_2 + new Vector3(HASH_OFFSET_2, HASH_OFFSET_2, HASH_OFFSET_2), hash), HASH_PRIME);

        // 6. Map hash values to gradients
        Vector3 h = hash / HASH_PRIME;  // Normalize hash to [0,1]

        // Select gradients based on hash
        int idx1 = Mathf.FloorToInt(h.x * gradients.Length) % gradients.Length;
        int idx2 = Mathf.FloorToInt(h.y * gradients.Length) % gradients.Length;
        int idx3 = Mathf.FloorToInt(h.z * gradients.Length) % gradients.Length;

        Vector2 gradient1 = gradients[idx1];
        Vector2 gradient2 = gradients[idx2];
        Vector2 gradient3 = gradients[idx3];

        // 7. Radial falloff
        Vector3 radialFalloff = new Vector3(
            Mathf.Max(falloffRadius - Vector2.Dot(cornerDist1, cornerDist1), 0),
            Mathf.Max(falloffRadius - Vector2.Dot(cornerDist2, cornerDist2), 0),
            Mathf.Max(falloffRadius - Vector2.Dot(cornerDist3, cornerDist3), 0)
        );

        radialFalloff.x = Mathf.Pow(radialFalloff.x, DEFAULT_FALLOFF_POWER);
        radialFalloff.y = Mathf.Pow(radialFalloff.y, DEFAULT_FALLOFF_POWER);
        radialFalloff.z = Mathf.Pow(radialFalloff.z, DEFAULT_FALLOFF_POWER);

        // 8. Linear ramp along gradients
        Vector3 LinearRamps = new Vector3(
            Vector2.Dot(gradient1, cornerDist1),
            Vector2.Dot(gradient2, cornerDist2),
            Vector2.Dot(gradient3, cornerDist3)
        );

        // 9. Multiply the ramp values at x with their corresponding falloff
        float contribution1 = radialFalloff.x * LinearRamps.x;
        float contribution2 = radialFalloff.y * LinearRamps.y;
        float contribution3 = radialFalloff.z * LinearRamps.z;

        // 10. Weighted sum
        float n = contribution1 + contribution2 + contribution3;

        return n;
    }

    static Vector2[] GenerateGradients(int count, int seed, float variation)
    {
        MethodHelper.SetRandomizerSeed(seed);

        Vector2[] gradients = new Vector2[count];

        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, 2f * Mathf.PI);
            float magnitude = Random.Range(1 - variation, 1);//(hashVal % 100) / 25f + 0.5f; // Generate a random magnitude between 0.5 and 4.5
            gradients[i] = new Vector2(Mathf.Cos(angle) * magnitude, Mathf.Sin(angle) * magnitude);
        }

        return gradients;
    }

    static Vector3 Mod(Vector3 v, float m)
    {
        return new Vector3(
            v.x - m * Mathf.Floor(v.x / m),
            v.y - m * Mathf.Floor(v.y / m),
            v.z - m * Mathf.Floor(v.z / m)
        );
    }
}