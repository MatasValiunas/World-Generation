using UnityEngine;

public static class Wavelet
{
    const int FILTER_RADIUS = 16;             // The radius of the filter used for downsampling operations
    const float startingFrequency = -6;    // Base frequency scale (negative = very low frequency/large features)
    const int firstBand = -3;                 // Octave to start sampling from
    static readonly float[] pCoeffs = new float[4] { 0.25f, 0.75f, 0.75f, 0.25f };  // Coefficients for the Quadratic B-spline filter used in upsampling
    static readonly float[] bandWeights = { 1.0f, 0.5f, 0.25f };    // Weights for each frequency band (lower frequencies have more influence)
    static readonly float[] filterCoefficients = new float[2 * FILTER_RADIUS] {
        0.000334f, -0.001528f, 0.000410f, 0.003545f, -0.000938f, -0.008233f, 0.002172f, 0.019120f,
        -0.005040f, -0.044412f, 0.011655f, 0.103311f, -0.025936f, -0.243780f, 0.033979f, 0.655340f,  // Coefficients for the low-pass filter used in downsampling
        0.655340f, 0.033979f, -0.243780f, -0.025936f, 0.103311f, 0.011655f, -0.044412f, -0.005040f,  // These form a symmetric filter with good frequency domain characteristics
        0.019120f, 0.002172f, -0.008233f, -0.000938f, 0.003546f, 0.000410f, -0.001528f, 0.000334f
    };

    static float[] noiseValues;    // Cached noise values for the generated noise tile
    static int tileSize;           // Size of the generated noise tile


    /// <param name="tileDimension">Size of the noise tile (higher = more detail but more computation)</param>
    public static float[,] Noise(int mapSize, float maxHeight, int seed, int tileDimension)
    {
        // Generate the noise tile that will be sampled to create the heightmap
        InitializeWaveletTile(tileDimension, seed);

        float[,] heightmap = new float[mapSize, mapSize];

        // Sample the noise at regular intervals to create the heightmap
        for (int z = 0; z < mapSize; z++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                // Convert grid coordinates to noise space coordinates
                float[] samplePoint = new float[2] {
                    (float)x / mapSize * tileDimension,
                    (float)z / mapSize * tileDimension
                };

                // Sample the noise by combining multiple frequency bands
                heightmap[x, z] = CombineFrequencyBands(samplePoint);
            }
        }

        MethodHelper.NormalizeValues(heightmap, maxHeight);

        return heightmap;
    }

    /// <summary>
    /// Initializes the wavelet noise tile used for sampling.
    /// Creates a tileable, band-limited noise pattern using wavelet decomposition.
    /// </summary>
    static void InitializeWaveletTile(int size, int seed)
    {
        // Ensure tile size is even (required for wavelet operations)
        size += size % 2;

        int dataSize = size * size;
        float[] noise = new float[dataSize]; 

        // Step 1: Start with white noise as the base
        float[,] whiteNoise = White.Noise(size, size, 100, seed);
        MethodHelper.NormalizeValues(whiteNoise, 1, -1);

        // Convert 2D array to 1D array for faster processing
        float[] downsampled = new float[dataSize]; // Temporary buffer for downsampling
        float[] upsampled = new float[dataSize];   // Temporary buffer for upsampling

        for (int i = 0, ix = 0; ix < size; ix++)
            for (int iz = 0; iz < size; iz++)
                noise[i++] = whiteNoise[ix, iz];

        // Steps 2 and 3: Apply wavelet transform to extract band-limited detail
        // Process rows first (x-direction)
        for (int iz = 0; iz < size; iz++)
        {
            Downsample(noise, downsampled, size, 1);  // Low-pass filter
            Upsample(downsampled, upsampled, size, 1); // Interpolate back to original size
        }

        // Then process columns (z-direction)
        for (int ix = 0; ix < size; ix++)
        {
            Downsample(upsampled, downsampled, size, size);
            Upsample(downsampled, upsampled, size, size);
        }

        // Step 4: Extract detail by subtracting the low-frequency component
        // This isolates just the band-limited detail we want
        for (int i = 0; i < dataSize; i++)
        {
            noise[i] -= upsampled[i];
        }

        // Add an offset version of the noise to itself to avoid even/odd variance differences
        // This creates a more uniform noise pattern without directional artifacts
        int offset = (size / 2) | 1; // Ensures the offset is odd

        for (int i = 0, ix = 0; ix < size; ix++)
        {
            for (int iz = 0; iz < size; iz++)
            {
                downsampled[i++] = noise[WrapCoordinate(ix + offset, size) + WrapCoordinate(iz + offset, size) * size];
            }
        }

        for (int i = 0; i < dataSize; i++)
        {
            noise[i] += downsampled[i];
        }

        // Store the final noise pattern for later sampling
        noiseValues = noise;
        tileSize = size;
    }

    /// <summary>
    /// Reduces the resolution of the data by applying a low-pass filter.
    /// This is the analysis step of the wavelet transform.
    /// </summary>
    /// <param name="sourceData">Source data array</param>
    /// <param name="targetData">Target (downsampled) data array</param>
    /// <param name="length">Length of the data</param>
    /// <param name="stride">Stride for accessing data (1 for rows, length for columns)</param>
    private static void Downsample(float[] sourceData, float[] targetData, int length, int stride)
    {
        for (int i = 0; i < length / 2; i++)
        {
            targetData[i * stride] = 0;

            // Apply the filter by convolving with source data
            for (int k = 2 * i - FILTER_RADIUS; k <= 2 * i + FILTER_RADIUS; k++)
            {
                // Calculate the coefficient index
                int coeffIndex = k - 2 * i + FILTER_RADIUS;
                if (coeffIndex >= 0 && coeffIndex < filterCoefficients.Length)
                {
                    // Apply filter coefficient to the corresponding sample (with wrapping)
                    targetData[i * stride] += filterCoefficients[coeffIndex] * sourceData[WrapCoordinate(k, length) * stride];
                }
            }
        }
    }

    /// <summary>
    /// Increases the resolution of the data using a quadratic B-spline interpolation.
    /// This is the synthesis step of the wavelet transform.
    /// </summary>
    /// <param name="sourceData">Source (low-resolution) data array</param>
    /// <param name="targetData">Target (upsampled) data array</param>
    /// <param name="length">Length of the target data</param>
    /// <param name="stride">Stride for accessing data (1 for rows, length for columns)</param>
    static void Upsample(float[] sourceData, float[] targetData, int length, int stride)
    {
        for (int i = 0; i < length; i++)
        {
            targetData[i * stride] = 0;

            // Interpolate values using the quadratic B-spline coefficients
            for (int k = i / 2; k <= i / 2 + 1; k++)
            {
                // Calculate the coefficient index
                int coeffIndex = i - 2 * k + 2;
                if (coeffIndex >= 0 && coeffIndex < pCoeffs.Length)
                {
                    // Apply the coefficient to the source value (with wrapping)
                    targetData[i * stride] += pCoeffs[coeffIndex] * sourceData[WrapCoordinate(k, length / 2) * stride];
                }
            }
        }
    }

    /// <summary>
    /// Combines multiple frequency bands of noise with different weights.
    /// This creates noise with characteristics of natural phenomena like terrain.
    /// </summary>
    /// <param name="samplePoint">The 2D point to sample</param>
    /// <param name="bandWeights">Weights for each frequency band</param>
    static float CombineFrequencyBands(float[] samplePoint)
    {
        float[] scaledPoint = new float[2];
        float result = 0, variance = 0;

        // Combine each frequency band with appropriate scaling and weighting
        for (int bandIndex = 0; bandIndex < bandWeights.Length && startingFrequency + firstBand + bandIndex < 0; bandIndex++)
        {
            // Scale the sample point based on the current frequency band
            for (int i = 0; i < 2; i++)
            {
                scaledPoint[i] = 2 * samplePoint[i] * Mathf.Pow(2, firstBand + bandIndex);
            }

            // Add this band's contribution to the result
            result += bandWeights[bandIndex] * WaveletNoise(scaledPoint);
        }

        // Calculate the total variance to normalize the result
        for (int bandIndex = 0; bandIndex < bandWeights.Length; bandIndex++)
        {
            variance += bandWeights[bandIndex] * bandWeights[bandIndex];
        }

        // Normalize the result to ensure consistent amplitude regardless of the number of bands
        if (variance > 0)
            result /= Mathf.Sqrt(variance * 0.210f); // 0.210 is an empirical correction factor

        return result;
    }

    /// <summary>
    /// Samples the wavelet noise at a specific point using quadratic B-spline interpolation.
    /// </summary>
    /// <param name="point">The 2D point to sample</param>
    /// <returns>The interpolated noise value</returns>
    static float WaveletNoise(float[] point)
    {
        // Variables for 2D noise interpolation
        int[] filterIndex = new int[2];
        int[] noiseCoordinate = new int[2];
        int[] centerCoordinate = new int[2];
        int n = tileSize;
        float[,] weights = new float[2, 3]; // B-spline weights for each dimension
        float parameterValue, result = 0;

        // Calculate quadratic B-spline basis function weights
        // This ensures smooth interpolation between noise values
        for (int i = 0; i < 2; i++)
        {
            centerCoordinate[i] = (int)Mathf.Ceil(point[i] - 0.5f);
            parameterValue = centerCoordinate[i] - (point[i] - 0.5f);

            // Quadratic B-spline weights for -1, 0, and +1 positions
            weights[i, 0] = parameterValue * parameterValue / 2.0f;                   // Weight for -1 position
            weights[i, 2] = (1.0f - parameterValue) * (1.0f - parameterValue) / 2.0f; // Weight for +1 position
            weights[i, 1] = 1.0f - weights[i, 0] - weights[i, 2];                     // Weight for 0 position
        }

        // Evaluate the noise by sampling a 3x3 neighborhood around the center point
        // and weighting each sample by the B-spline basis function values
        for (filterIndex[1] = -1; filterIndex[1] <= 1; filterIndex[1]++)
            for (filterIndex[0] = -1; filterIndex[0] <= 1; filterIndex[0]++)
            {
                float weight = 1.0f;
                for (int i = 0; i < 2; i++)
                {
                    // Calculate the noise coordinate with wrapping
                    noiseCoordinate[i] = WrapCoordinate(centerCoordinate[i] + filterIndex[i], n);
                    // Multiply weights from both dimensions
                    weight *= weights[i, filterIndex[i] + 1];
                }

                // Calculate the index into the 1D noise array
                int index = noiseCoordinate[1] * n + noiseCoordinate[0];
                if (index >= 0 && index < noiseValues.Length)
                {
                    // Add the weighted noise value to the result
                    result += weight * noiseValues[index];
                }
            }

        return result;
    }

    /// <summary>
    /// Wraps a coordinate to ensure it's within the range [0, n-1].
    /// This creates seamless tiling of the noise pattern.
    /// </summary>
    /// <param name="x">The coordinate to wrap</param>
    /// <param name="n">The size of the dimension</param>
    /// <returns>The wrapped coordinate</returns>
    static int WrapCoordinate(int x, int n)
    {
        int remainder = x % n;
        return (remainder < 0) ? remainder + n : remainder;
    }
}