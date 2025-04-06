using System;
using System.Collections.Generic;
using UnityEngine;

public static class MarchingCubes
{
    // Triangulation table based on the 15 cube configurations (0-14)
    private static readonly int[][] triangulationTable = new int[][]
    {
        new int[] { }, // Case 0: No vertices
        new int[] { 0, 3, 8 }, // Case 1: 1 vertex
        new int[] { 0, 1, 9 }, // Case 2: 1 vertex
        new int[] { 1, 3, 8, 1, 8, 9 }, // Case 3: 2 vertices
        new int[] { 1, 2, 10 }, // Case 4: 1 vertex
        new int[] { 0, 3, 8, 1, 2, 10 }, // Case 5: 2 vertices
        new int[] { 0, 2, 10, 0, 10, 9 }, // Case 6: 2 vertices
        new int[] { 2, 3, 8, 2, 8, 10, 10, 8, 9 }, // Case 7: 3 vertices
        new int[] { 3, 2, 11 }, // Case 8: 1 vertex
        new int[] { 0, 2, 11, 0, 11, 8 }, // Case 9: 2 vertices
        new int[] { 0, 1, 9, 3, 2, 11 }, // Case 10: 2 vertices
        new int[] { 1, 2, 11, 1, 11, 9, 9, 11, 8 }, // Case 11: 3 vertices
        new int[] { 3, 1, 10, 3, 10, 11 }, // Case 12: 2 vertices
        new int[] { 0, 1, 10, 0, 10, 8, 8, 10, 11 }, // Case 13: 3 vertices
        new int[] { 0, 3, 11, 0, 11, 9, 9, 11, 10 }, // Case 14: 3 vertices
    };

    // Vertex positions for a cube (local coordinates)
    private static readonly Vector3[] cubeVertices = new Vector3[]
    {
        new Vector3(0, 0, 0), // 0: bottom-left-back
        new Vector3(1, 0, 0), // 1: bottom-right-back
        new Vector3(1, 0, 1), // 2: bottom-right-front
        new Vector3(0, 0, 1), // 3: bottom-left-front
        new Vector3(0, 1, 0), // 4: top-left-back
        new Vector3(1, 1, 0), // 5: top-right-back
        new Vector3(1, 1, 1), // 6: top-right-front
        new Vector3(0, 1, 1)  // 7: top-left-front
    };

    // Edge vertices for interpolation (pairs of cube vertices forming an edge)
    private static readonly int[,] edgeVertices = new int[,]
    {
        {0, 1}, // Edge 0: bottom-back
        {1, 2}, // Edge 1: bottom-right
        {2, 3}, // Edge 2: bottom-front
        {3, 0}, // Edge 3: bottom-left
        {4, 5}, // Edge 4: top-back
        {5, 6}, // Edge 5: top-right
        {6, 7}, // Edge 6: top-front
        {7, 4}, // Edge 7: top-left
        {0, 4}, // Edge 8: back-left
        {1, 5}, // Edge 9: back-right
        {2, 6}, // Edge 10: front-right
        {3, 7}  // Edge 11: front-left
    };

    // Surface level threshold
    private const float surfaceLevel = 0.5f;

    /// <summary>
    /// Generate a mesh from a heightmap using the Marching Cubes algorithm
    /// </summary>
    /// <param name="heightmap">2D array of height values (0-1)</param>
    /// <param name="scale">Scale of the generated mesh</param>
    /// <returns>Generated mesh representing the terrain</returns>
    public static Mesh GenerateMesh(float[,] heightmap, Vector3 scale)
    {
        int width = heightmap.GetLength(0);
        int depth = heightmap.GetLength(1);
        int height = Mathf.Max(width, depth); // Use the larger dimension for height

        // Convert 2D heightmap to 3D density field
        float[,,] densityField = GenerateDensityField(heightmap, height);

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Process each cube in the density field
        for (int x = 0; x < width - 1; x++)
        {
            for (int y = 0; y < height - 1; y++)
            {
                for (int z = 0; z < depth - 1; z++)
                {
                    ProcessCube(densityField, x, y, z, vertices, triangles, scale);
                }
            }
        }

        // Create and return the mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }

    /// <summary>
    /// Generate a 3D density field from a 2D heightmap
    /// </summary>
    private static float[,,] GenerateDensityField(float[,] heightmap, int height)
    {
        int width = heightmap.GetLength(0);
        int depth = heightmap.GetLength(1);
        float[,,] densityField = new float[width, height, depth];

        // Initialize the density field based on the heightmap
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                float surfaceHeight = heightmap[x, z] * height;

                for (int y = 0; y < height; y++)
                {
                    // Density is 1 below the surface, 0 above
                    densityField[x, y, z] = y < surfaceHeight ? 1.0f : 0.0f;
                }
            }
        }

        return densityField;
    }

    /// <summary>
    /// Process a single cube in the density field and generate triangles
    /// </summary>
    private static void ProcessCube(float[,,] densityField, int x, int y, int z, List<Vector3> vertices, List<int> triangles, Vector3 scale)
    {
        // Get the density values at each corner of the cube
        float[] cornerDensities = new float[8];
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = cubeVertices[i];
            cornerDensities[i] = densityField[x + (int)corner.x, y + (int)corner.y, z + (int)corner.z];
        }

        // Determine which case we're in (0-14)
        int caseIndex = GetCaseIndex(cornerDensities);

        // If the cube is completely inside or outside the surface, skip it
        if (caseIndex == 0 || caseIndex == 15)
            return;

        // Ensure we have a valid case index (we only have cases 0-14)
        caseIndex = Mathf.Min(caseIndex, 14);

        // Get the edges that are intersected by the surface
        int[] edges = triangulationTable[caseIndex];

        // Generate triangles
        for (int i = 0; i < edges.Length; i += 3)
        {
            // For each triangle
            for (int j = 0; j < 3; j++)
            {
                int edgeIndex = edges[i + j];
                
                // Get the two vertices that form the edge
                int v1 = edgeVertices[edgeIndex, 0];
                int v2 = edgeVertices[edgeIndex, 1];
                
                // Get the positions of the two vertices
                Vector3 p1 = cubeVertices[v1] + new Vector3(x, y, z);
                Vector3 p2 = cubeVertices[v2] + new Vector3(x, y, z);
                
                // Get the density values at the two vertices
                float d1 = cornerDensities[v1];
                float d2 = cornerDensities[v2];
                
                // Interpolate to find where the surface intersects the edge
                Vector3 vertexPosition = InterpolateVertex(p1, p2, d1, d2);
                
                // Apply scale
                vertexPosition.x *= scale.x;
                vertexPosition.y *= scale.y;
                vertexPosition.z *= scale.z;
                
                // Add the vertex and its index to the triangle
                triangles.Add(vertices.Count);
                vertices.Add(vertexPosition);
            }
        }
    }

    /// <summary>
    /// Determine the case index based on which corners are inside the surface
    /// </summary>
    private static int GetCaseIndex(float[] cornerDensities)
    {
        int caseIndex = 0;
        
        for (int i = 0; i < 8; i++)
        {
            if (cornerDensities[i] >= surfaceLevel)
            {
                caseIndex |= 1 << i;
            }
        }
        
        return caseIndex;
    }

    /// <summary>
    /// Interpolate between two vertices to find where the surface intersects the edge
    /// </summary>
    private static Vector3 InterpolateVertex(Vector3 p1, Vector3 p2, float d1, float d2)
    {
        if (Mathf.Abs(surfaceLevel - d1) < 0.00001f)
            return p1;
            
        if (Mathf.Abs(surfaceLevel - d2) < 0.00001f)
            return p2;
            
        if (Mathf.Abs(d1 - d2) < 0.00001f)
            return p1;
            
        float t = (surfaceLevel - d1) / (d2 - d1);
        return p1 + t * (p2 - p1);
    }

    /// <summary>
    /// Usage example
    /// </summary>
    public static void Example()
    {
        // Create a sample heightmap (100x100)
        int size = 100;
        float[,] heightmap = new float[size, size];
        
        // Fill with sample height data (e.g., a simple hill)
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                float distX = x - size / 2;
                float distZ = z - size / 2;
                float distance = Mathf.Sqrt(distX * distX + distZ * distZ);
                heightmap[x, z] = Mathf.Max(0, 1 - distance / (size / 2));
            }
        }
        
        // Generate the mesh
        Vector3 scale = new Vector3(1, 1, 1);
        Mesh terrainMesh = GenerateMesh(heightmap, scale);
        
        // Use the mesh (e.g., attach to a GameObject)
        GameObject terrainObject = new GameObject("Terrain");
        MeshFilter meshFilter = terrainObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = terrainObject.AddComponent<MeshRenderer>();
        meshFilter.mesh = terrainMesh;
        
        // Assign a material
        Material terrainMaterial = new Material(Shader.Find("Standard"));
        if (terrainMaterial != null)
        {
            meshRenderer.material = terrainMaterial;
        }
        else
        {
            Debug.LogError("Could not find the Standard shader. Using default material instead.");
            // Unity will automatically assign a default material in this case
        }
    }
}