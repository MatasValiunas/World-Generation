using UnityEngine;


public static class Wavelet
{
    private static float[] noiseTileData;
    private static int noiseTileSize;

    private const int ARAD = 16;

    // Function to generate a heightmap
    public static float[,] GenerateHeightmap(int mapSize = 64, int tileSize = 32)
    {
        // Generate the noise tile
        GenerateNoiseTile(tileSize);

        if (noiseTileData == null)
        {
            Debug.LogError("Error: noiseTileData is NULL");
            return null;
        }

        float[] weights = { 1.0f, 0.5f, 0.25f };
        int nbands = weights.Length;
        float s = -6.0f;
        int firstBand = -3;

        float[,] heightmap = new float[mapSize, mapSize];

        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                float[] p = new float[2]
                {
                    (float)x / (float)mapSize * tileSize,
                    (float)y / (float)mapSize * tileSize
                };

                heightmap[x, y] = WMultibandNoise(p, s, firstBand, nbands, weights);
            }
        }
        Algorithms.NormalizeValues(heightmap, 15);
        return heightmap;
    }

    private static int Mod(int x, int n)
    {
        int m = x % n;
        return (m < 0) ? m + n : m;
    }

    private static float GaussianNoise()
    {
        // Using the Box-Muller transform to generate Gaussian noise
        float u, v, s;
        
        do
        {
            u = UnityEngine.Random.value * 2.0f - 1.0f;
            v = UnityEngine.Random.value * 2.0f - 1.0f;
            s = u * u + v * v;
        } while (s >= 1.0f || s == 0.0f);

        s = Mathf.Sqrt(-2.0f * Mathf.Log(s) / s);
        
        // Return one of the two values
        return u * s;
    }

    private static void Downsample(float[] from, float[] to, int n, int stride)
    {
        float[] aCoeffs = new float[2 * ARAD] {
            0.000334f, -0.001528f, 0.000410f, 0.003545f, -0.000938f, -0.008233f, 0.002172f, 0.019120f,
            -0.005040f, -0.044412f, 0.011655f, 0.103311f, -0.025936f, -0.243780f, 0.033979f, 0.655340f,
            0.655340f, 0.033979f, -0.243780f, -0.025936f, 0.103311f, 0.011655f, -0.044412f, -0.005040f,
            0.019120f, 0.002172f, -0.008233f, -0.000938f, 0.003546f, 0.000410f, -0.001528f, 0.000334f
        };

        for (int i = 0; i < n / 2; i++)
        {
            to[i * stride] = 0;
            for (int k = 2 * i - ARAD; k <= 2 * i + ARAD; k++)
            {
                // Ensure index is in valid range
                int coeffIndex = k - 2 * i + ARAD;
                if (coeffIndex >= 0 && coeffIndex < aCoeffs.Length)
                {
                    to[i * stride] += aCoeffs[coeffIndex] * from[Mod(k, n) * stride];
                }
            }
        }
    }

    private static void Upsample(float[] from, float[] to, int n, int stride)
    {
        float[] pCoeffs = new float[4] { 0.25f, 0.75f, 0.75f, 0.25f };

        for (int i = 0; i < n; i++)
        {
            to[i * stride] = 0;
            for (int k = i / 2; k <= i / 2 + 1; k++)
            {
                // Ensure index is in valid range
                int coeffIndex = i - 2 * k + 2;
                if (coeffIndex >= 0 && coeffIndex < pCoeffs.Length)
                {
                    to[i * stride] += pCoeffs[coeffIndex] * from[Mod(k, n / 2) * stride];
                }
            }
        }
    }

    private static void GenerateNoiseTile(int n)
    {
        if (n % 2 != 0)
            n++; // tile size must be even

        int sz = n * n;
        float[] temp1 = new float[sz];
        float[] temp2 = new float[sz];
        float[] noise = new float[sz];

        // Step 1. Fill the tile with random numbers
        for (int i = 0; i < sz; i++)
            noise[i] = GaussianNoise();

        // Steps 2 and 3. Downsample and upsample the tile
        for (int iy = 0; iy < n; iy++)
        { // each x row
            int i = iy * n;
            Downsample(noise, temp1, n, 1);
            Upsample(temp1, temp2, n, 1);
        }

        for (int ix = 0; ix < n; ix++)
        { // each y row
            int i = ix;
            Downsample(temp2, temp1, n, n);
            Upsample(temp1, temp2, n, n);
        }

        // Step 4. Subtract out the coarse-scale contribution
        for (int i = 0; i < sz; i++)
        {
            noise[i] -= temp2[i];
        }

        // Avoid even/odd variance difference by adding odd-offset version of noise to itself
        int offset = n / 2;
        if (offset % 2 == 0)
            offset++;

        for (int i = 0, ix = 0; ix < n; ix++)
            for (int iy = 0; iy < n; iy++)
                temp1[i++] = noise[Mod(ix + offset, n) + Mod(iy + offset, n) * n];

        for (int i = 0; i < sz; i++)
        {
            noise[i] += temp1[i];
        }

        noiseTileData = noise;
        noiseTileSize = n;
    }

    private static float WNoise(float[] p)
    {
        // 2D noise
        int[] f = new int[2];
        int[] c = new int[2];
        int[] mid = new int[2];
        int n = noiseTileSize;
        float[,] w = new float[2, 3];
        float t, result = 0;

        // Evaluate quadratic B-spline basis functions
        for (int i = 0; i < 2; i++)
        {
            mid[i] = (int)Mathf.Ceil(p[i] - 0.5f);
            t = mid[i] - (p[i] - 0.5f);
            w[i, 0] = t * t / 2.0f;
            w[i, 2] = (1.0f - t) * (1.0f - t) / 2.0f;
            w[i, 1] = 1.0f - w[i, 0] - w[i, 2];
        }

        // Evaluate noise by weighting noise coefficients by basis function values
        for (f[1] = -1; f[1] <= 1; f[1]++)
            for (f[0] = -1; f[0] <= 1; f[0]++)
            {
                float weight = 1.0f;
                for (int i = 0; i < 2; i++)
                {
                    c[i] = Mod(mid[i] + f[i], n);
                    weight *= w[i, f[i] + 1];
                }
                
                // Make sure the indices are in range
                int index = c[1] * n + c[0];
                if (index >= 0 && index < noiseTileData.Length)
                {
                    result += weight * noiseTileData[index];
                }
            }

        return result;
    }

    private static float WMultibandNoise(float[] p, float s, int firstBand, int nbands, float[] w)
    {
        float[] q = new float[2];
        float result = 0, variance = 0;

        for (int b = 0; b < nbands && s + firstBand + b < 0; b++)
        {
            for (int i = 0; i < 2; i++)
            {
                q[i] = 2 * p[i] * Mathf.Pow(2, firstBand + b);
            }
            
            result += w[b] * WNoise(q);
        }

        for (int b = 0; b < nbands; b++)
        {
            variance += w[b] * w[b];
        }

        // Adjust the noise so it has a variance of 1
        if (variance > 0)
            result /= Mathf.Sqrt(variance * 0.210f);

        return result;
    }
}