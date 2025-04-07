using UnityEngine;

public static class Triangulation
{
    /// <summary>
    /// Creates a GameObject with the generated mesh
    /// </summary>
    /// <param name="heightmap">2D array of float values representing heights</param>
    /// <param name="material">Material to apply to the mesh</param>
    /// <returns>GameObject with the mesh renderer</returns>
    public static GameObject CreateVoxelObject(float[,] heightmap, Material material = null)
    {
        // Create game object
        GameObject voxelObject = new GameObject("Terrain");
        
        // Add mesh filter and renderer components
        MeshFilter meshFilter = voxelObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = voxelObject.AddComponent<MeshRenderer>();
        
        // Generate and assign mesh
        meshFilter.mesh = CreateMeshFromHeightmap(heightmap);
        
        // Assign material
        if (material == null)
        {
            material = new Material(Shader.Find("Default"));
        }
        meshRenderer.material = material;
        
        // Add mesh collider for physics interactions
        MeshCollider meshCollider = voxelObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = meshFilter.mesh;
        
        return voxelObject;
    }


    /// <summary>
    /// Creates a mesh based on a heightmap
    /// </summary>
    /// <param name="heightmap">2D array of float values representing heights</param>
    /// <returns>Generated mesh</returns>
    private static Mesh CreateMeshFromHeightmap(float[,] heightmap)
    {
        int width = heightmap.GetLength(0);
        int height = heightmap.GetLength(1);
        
        // Create mesh object
        Mesh mesh = new Mesh();
        
        // Create vertices based on heightmap
        Vector3[] vertices = new Vector3[width * height];
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = z * width + x;
                float y = heightmap[x, z];
                vertices[index] = new Vector3(x, y, z);
            }
        }
        
        // Create triangles (2 triangles per grid cell)
        int[] triangles = new int[(width - 1) * (height - 1) * 6];
        int triangleIndex = 0;
        
        for (int z = 0; z < height - 1; z++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                int bottomLeft = z * width + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = (z + 1) * width + x;
                int topRight = topLeft + 1;
                
                // First triangle
                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = bottomRight;
                
                // Second triangle
                triangles[triangleIndex++] = bottomRight;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = topRight;
            }
        }
        
        // Create UVs
        Vector2[] uvs = new Vector2[vertices.Length];
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = z * width + x;
                uvs[index] = new Vector2((float)x / width, (float)z / height);
            }
        }
        
        // Assign data to mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        
        // Calculate normals for proper lighting
        mesh.RecalculateNormals();
        
        return mesh;
    }
}