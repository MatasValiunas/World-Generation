using UnityEngine;


public static class SimplexV1
{
    private static Vector2[] gradientTable;

    public struct Point 
    {
        public Vector2 coordinates;
        public Vector2 gradient;

        public Point (float x, float y)
        {
            coordinates = new Vector2(x, y);
            gradient = gradientTable[Hash(coordinates) % gradientTable.Length];
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

    
    public static float[,] Noise(int width, int length, float maxHeight, float scale, float influenceRadius, int seed)
    {
        gradientTable = GenerateGradientTable(32, seed);

        float[,] grid = new float[width, length];
        Triangle[,] simplexGrid = SimplexGridGenerator(width, length);

        for (int x = 0; x < width; x++)
        {   
            for (int y = 0; y < length; y++)
            {
                float fx = x / scale;
                float fy = y / scale;

                Vector2 point = new Vector2(fx, fy);

                Triangle[] triangles = new Triangle[3] 
                { 
                    simplexGrid[Mathf.FloorToInt(fx), Mathf.FloorToInt(fy) * 2],
                    simplexGrid[Mathf.FloorToInt(fx), Mathf.FloorToInt(fy) * 2 + 1],
                    simplexGrid[Mathf.FloorToInt(fx), Mathf.FloorToInt(fy) * 2 + 2],
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

        MethodHelper.NormalizeValues(grid, (float)maxHeight);

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
    
    private static Vector2[] GenerateGradientTable(int size, int seed)
    {
        Vector2[] table = new Vector2[size];

        MethodHelper.SetRandomizerSeed(seed);
        
        for (int i = 0; i < size; i++)
        {
            float angle = Random.Range(0f, 2f * Mathf.PI);
            table[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        return table;
    }

    public static int Hash(Vector2 coord)
    {
        unchecked
        {
            int x = Mathf.FloorToInt(coord.x);
            int y = Mathf.FloorToInt(coord.y);

            // FNV-1a like hashing with good bit mixing
            uint hash = 2166136261u;
            hash = (hash ^ (uint)x) * 16777619;
            hash = (hash ^ (uint)y) * 16777619;

            // Final avalanche step (mix bits further)
            hash ^= (hash >> 13);
            hash *= 0x5bd1e995;
            hash ^= (hash >> 15);

            return (int)(hash & 0x7FFFFFFF); // keep it positive
        }
    }
}