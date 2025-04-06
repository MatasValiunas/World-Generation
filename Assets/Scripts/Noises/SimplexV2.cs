using UnityEngine;


public static class SimplexNoise2D
{
    public static float[,] GenerateHeightmap(int width, int height, float scale, float maxHeight, Vector2 period, float alpha = 0f)
    {
        float[,] heightmap = new float[width, height];
        Vector2 gradient; // unused unless you want to collect gradients

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Scale coordinates into noise space
                Vector2 point = new Vector2(x / scale, y / scale);

                // Sample noise
                float noiseValue = Noise(point, period, alpha, out gradient);

                // Optional: rescale to [0, maxHeight] from [-1, 1]
                float mappedValue = (noiseValue + 1f) * 0.5f * maxHeight;
                heightmap[x, y] = mappedValue;
            }
        }

        return heightmap;
    }

    public static float Noise(Vector2 point, Vector2 period, float alpha, out Vector2 gradient)
    {
        // 2. Transform input point to find simplex "base" i0
        Vector2 uv = new Vector2(point.x + point.y * 0.5f, point.y);
        Vector2 i0 = new Vector2(Mathf.Floor(uv.x), Mathf.Floor(uv.y));
        Vector2 f0 = new Vector2(uv.x - i0.x, uv.y - i0.y);

        // 3. Determine simplex corner offsets
        float cmp = f0.y < f0.x ? 1f : 0f;
        Vector2 o1 = new Vector2(cmp, 1f - cmp);
        Vector2 i1 = i0 + o1;
        Vector2 i2 = i0 + Vector2.one;

        // Convert simplex grid coords back to unskewed space
        Vector2 v0 = new Vector2(i0.x - i0.y * 0.5f, i0.y);
        Vector2 v1 = new Vector2(v0.x + o1.x - o1.y * 0.5f, v0.y + o1.y);
        Vector2 v2 = new Vector2(v0.x + 0.5f, v0.y + 1f);

        // 4. Compute distances to corners
        Vector2 x0 = point - v0;
        Vector2 x1 = point - v1;
        Vector2 x2 = point - v2;

        // 5, 6. Handle tiling wrap (optional)
        Vector3 iu, iv;
        if (period.x > 0f || period.y > 0f)
        {
            Vector3 xw = new Vector3(v0.x, v1.x, v2.x);
            Vector3 yw = new Vector3(v0.y, v1.y, v2.y);

            if (period.x > 0f)
            {
                xw.x = Mathf.Repeat(xw.x, period.x);
                xw.y = Mathf.Repeat(xw.y, period.x);
                xw.z = Mathf.Repeat(xw.z, period.x);
            }
            if (period.y > 0f)
            {
                yw.x = Mathf.Repeat(yw.x, period.y);
                yw.y = Mathf.Repeat(yw.y, period.y);
                yw.z = Mathf.Repeat(yw.z, period.y);
            }

            iu = new Vector3(Mathf.Floor(xw.x + 0.5f * yw.x + 0.5f),
                            Mathf.Floor(xw.y + 0.5f * yw.y + 0.5f),
                            Mathf.Floor(xw.z + 0.5f * yw.z + 0.5f));
            iv = new Vector3(Mathf.Floor(yw.x + 0.5f),
                            Mathf.Floor(yw.y + 0.5f),
                            Mathf.Floor(yw.z + 0.5f));
        }
        else
        {
            iu = new Vector3(i0.x, i1.x, i2.x);
            iv = new Vector3(i0.y, i1.y, i2.y);
        }

        // 7. Hash
        Vector3 hash = Mod(iu, 289f);
        hash = Mod(Vector3.Scale(hash * 51f + new Vector3(2f, 2f, 2f), hash) + iv, 289f);
        hash = Mod(Vector3.Scale(hash * 34f + new Vector3(10f, 10f, 10f), hash), 289f);


        // 8, 9a. Generate gradients with rotation
        Vector3 psi = hash * 0.07482f + new Vector3(alpha, alpha, alpha);
        Vector3 gx = new Vector3(Mathf.Cos(psi.x), Mathf.Cos(psi.y), Mathf.Cos(psi.z));
        Vector3 gy = new Vector3(Mathf.Sin(psi.x), Mathf.Sin(psi.y), Mathf.Sin(psi.z));

        Vector2 g0 = new Vector2(gx.x, gy.x);
        Vector2 g1 = new Vector2(gx.y, gy.y);
        Vector2 g2 = new Vector2(gx.z, gy.z);

        // 10. Radial falloff
        Vector3 w = new Vector3(
            0.8f - Vector2.Dot(x0, x0),
            0.8f - Vector2.Dot(x1, x1),
            0.8f - Vector2.Dot(x2, x2)
        );
        w = new Vector3(Mathf.Max(w.x, 0f), Mathf.Max(w.y, 0f), Mathf.Max(w.z, 0f));
        Vector3 w2 = Multiply(w, w);
        Vector3 w4 = Multiply(w2, w2);
        Vector3 w3 = Multiply(w2, w);

        // 11. Linear ramp along gradients
        Vector3 gdotx = new Vector3(
            Vector2.Dot(g0, x0),
            Vector2.Dot(g1, x1),
            Vector2.Dot(g2, x2)
        );

        // 12, 13. Weighted sum
        float n = w4.x * gdotx.x + w4.y * gdotx.y + w4.z * gdotx.z;

        // 14. Analytic gradient
        Vector3 dw = -8f * Multiply(w3, gdotx);
        Vector2 dn0 = w4.x * g0 + dw.x * x0;
        Vector2 dn1 = w4.y * g1 + dw.y * x1;
        Vector2 dn2 = w4.z * g2 + dw.z * x2;

        gradient = 10.9f * (dn0 + dn1 + dn2);

        return 10.9f * n;
    }

    // Helper: modulus for float vectors
    private static Vector3 Mod(Vector3 v, float m)
    {
        return new Vector3(
            v.x - m * Mathf.Floor(v.x / m),
            v.y - m * Mathf.Floor(v.y / m),
            v.z - m * Mathf.Floor(v.z / m)
        );
    }

    // Helper: component-wise multiplication
    private static Vector3 Multiply(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
    }
}