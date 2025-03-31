using System.Collections.Generic;
using UnityEngine;
using AlgorithmsHelper;

namespace Generator
{
    public static class White
    {
        public static float[,] Noise(int width, int length, int maxHeight)
        {
            float[,] grid = new float[width, length];
            List<float> list = new();

            int size = width * length;
            int cycles = Mathf.CeilToInt((float)size / maxHeight);
            
            for (int i = 0; i < cycles; i++)
            {
                for (int value = 1; value <= maxHeight; value++)
                {
                    list.Add(value);
                }
            }

            Algorithms.ShuffleList(list);  
            
            // Convert the shuffled list into a 2D array
            for (int x = 0, i = 0; x < width; x++)
            {
                for (int z = 0; z < length; z++)
                {
                    grid[x, z] = list[i++];
                }
            }

            return grid;
        }
    }
    

    public static class Perlin
{
    public static float[,] Noise(int width, int length, float maxHeight, float scale = 0.05f, int resolution = 10)
    {
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

        int finalWidth = width * resolution;
        int finalLength = length * resolution;
        float[,] noise = new float[finalWidth, finalLength];

        for (int x = 0; x < finalWidth; x++)
        {
            for (int y = 0; y < finalLength; y++)
            {
                float fx = (float)x / resolution * scale;
                float fy = (float)y / resolution * scale;

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



    public static class Simplex
    {
        private static readonly Vector2[] gradientTable = GenerateGradientTable(32);

        public struct Point 
        {
            public Vector2 coordinates;
            public Vector2 gradient;

            public Point (float x, float y)
            {
                coordinates = new Vector2(x, y);
                gradient = gradientTable[Algorithms.Hash(coordinates) % gradientTable.Length];
            }
        }

        struct Triangle
        {
            public Point[] points;


            public Triangle(Point a, Point b, Point c)
            {
                points = new Point[3]{ a, b, c };
            }

            public bool ContainsPoint(Vector2 p)
            {
                Vector2 v0 = points[2].coordinates - points[0].coordinates;
                Vector2 v1 = points[1].coordinates - points[0].coordinates;
                Vector2 v2 = p - points[0].coordinates;

                float dot00 = Vector2.Dot(v0, v0);
                float dot01 = Vector2.Dot(v0, v1);
                float dot02 = Vector2.Dot(v0, v2);
                float dot11 = Vector2.Dot(v1, v1);
                float dot12 = Vector2.Dot(v1, v2);

                float denom = dot00 * dot11 - dot01 * dot01;
                float invDenom = 1f / denom;

                float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
                float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

                const float epsilon = 1e-5f;    // Small tolerance to account for floating-point inaccuracies when point is on the edge of the triangle
                return (u >= -epsilon) && (v >= -epsilon) && (u + v <= 1f + epsilon);
            }
        }

        
        public static float[,] Noise(int width, int length, float maxHeight, float scale = 0.05f, float influenceRadius = 1.5f)
        {
            float[,] grid = new float[width, length];
            Triangle[,] simplexGrid = SimplexGridGenerator(width, length);

            for (int x = 0; x < width; x++)
            {   
                for (int y = 0; y < length; y++)
                {
                    Vector2 point = new Vector2(x * scale, y * scale);

                    Triangle[] triangles = new Triangle[3] 
                    { 
                        simplexGrid[Mathf.FloorToInt(x * scale), Mathf.FloorToInt(y * scale) * 2],
                        simplexGrid[Mathf.FloorToInt(x * scale), Mathf.FloorToInt(y * scale) * 2 + 1],
                        simplexGrid[Mathf.FloorToInt(x * scale), Mathf.FloorToInt(y * scale) * 2 + 2],
                    };
                    
                    Triangle triangle = triangles[GetTriangleIndexContainingPoint(point, triangles)];

                    Vector2[] cornerVectors = new Vector2[3]
                    {
                        point - triangle.points[0].coordinates,
                        point - triangle.points[1].coordinates,
                        point - triangle.points[2].coordinates,
                    };
                    
                    float[] falloffs = new float[3]
                    {
                        Mathf.Max(0f, influenceRadius - Vector2.Dot(cornerVectors[0], cornerVectors[0])),
                        Mathf.Max(0f, influenceRadius - Vector2.Dot(cornerVectors[1], cornerVectors[1])),
                        Mathf.Max(0f, influenceRadius - Vector2.Dot(cornerVectors[2], cornerVectors[2])),
                    };

                    // 11
                    float[] ramps = new float[3]
                    {
                        Vector2.Dot(triangle.points[0].gradient, cornerVectors[0]),
                        Vector2.Dot(triangle.points[1].gradient, cornerVectors[1]),
                        Vector2.Dot(triangle.points[2].gradient, cornerVectors[2]),
                    };
                    
                    // 12
                    float[] contributions = new float[3]
                    {
                        ramps[0] * Mathf.Pow(falloffs[0], 4), // using wi⁴ falloff like in the paper
                        ramps[1] * Mathf.Pow(falloffs[1], 4),
                        ramps[2] * Mathf.Pow(falloffs[2], 4),
                    };

                    // 13
                    float noiseValue = contributions[0] + contributions[1] + contributions[2];
                    grid[x, y] = noiseValue;
                }
            }

            Algorithms.NormalizeValues(grid, (float)maxHeight);

            return grid;
        }

        private static Triangle[,] SimplexGridGenerator(int width, int length)
        {
            Triangle[,] grid = new Triangle[width, length * 2 + 1];

            for (int x = 0; x < width; x++)
            {
                if (x % 2 == 0)
                {
                    grid[x, 0] = new Triangle(new Point(x + 1f, -0.5f), new Point(x, 0f), new Point(x + 1f, 0.5f));
                    for (int y = 0; y < length; y++)
                    {
                        grid[x, y * 2 + 1] = new Triangle(new Point(x, y), new Point(x + 1f, y + 0.5f), new Point(x, y + 1f));
                        grid[x, y * 2 + 2] = new Triangle(new Point(x + 1f, y + 0.5f), new Point(x, y + 1f), new Point(x + 1f, y + 1.5f));
                    }
                }
                else
                {
                    grid[x, 0] = new Triangle(new Point(x, -0.5f), new Point(x + 1f, 0f), new Point(x, 0.5f));
                    for (int y = 0; y < length; y++)
                    {
                        grid[x, y * 2 + 1] = new Triangle(new Point(x + 1f, y), new Point(x, y + 0.5f), new Point(x + 1f, y + 1f));
                        grid[x, y * 2 + 2] = new Triangle(new Point(x, y + 0.5f), new Point(x + 1f, y + 1f), new Point(x, y + 1.5f));
                    }
                }
            }
            
            return grid;
        }
        
        private static int GetTriangleIndexContainingPoint(Vector2 point, Triangle[] triangles)
        {
            for (int i = 0; i < triangles.Length; i++)
            {
                if (triangles[i].ContainsPoint(point))
                    return i; 
            }

            throw new System.ArgumentException($"No triangle contains the point {point}");
        }
        
        private static Vector2[] GenerateGradientTable(int directions)
        {
            Vector2[] table = new Vector2[directions];
            float step = 2 * Mathf.PI / directions;

            for (int i = 0; i < directions; i++)
            {
                float angle = i * step;
                table[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }

            return table;
        }
    }

    public static class Noises
    {
        public static void Wavelet(){}
        public static void Worley(){}
        public static void DiamondSquare(){}
        public static void CellularAutomaton(){}
    }
}