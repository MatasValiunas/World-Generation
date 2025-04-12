using UnityEngine;

public static class Triangulation
{
    public static GameObject GenerateTerrain(float[,] heightmap, float scale = 1f, Material material = null)
    {
        GameObject terrain = new GameObject("Terrain");
        terrain.transform.localScale = new Vector3(scale, scale, scale);

        if (material == null)
        {
            material = new Material(Shader.Find("Default"));
        }
        else
        {
            material = new Material(material);
        }

        MeshRenderer meshRenderer = terrain.AddComponent<MeshRenderer>();
        meshRenderer.material = material;

        MeshFilter meshFilter = terrain.AddComponent<MeshFilter>();
        meshFilter.mesh = CreateMeshFromHeightmap(heightmap);
        
        return terrain;
    }

    private static Mesh CreateMeshFromHeightmap(float[,] heightmap)
    {
        int width = heightmap.GetLength(0), length = heightmap.GetLength(1);
        
        Vector3[] vertices = new Vector3[width * length];
        for (int z = 0; z < length; z++)
        {
            for (int x = 0; x < width; x++)
            {
                vertices[z * width + x] = new Vector3(x, heightmap[x, z], z);
            }
        }
        
        // Create triangles (2 triangles per grid cell)
        int[] triangles = new int[(width - 1) * (length - 1) * 6];
        
        for (int z = 0, i = 0; z < length - 1; z++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                int bottomLeft = z * width + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = (z + 1) * width + x;
                int topRight = topLeft + 1;
                
                // First triangle
                triangles[i++] = bottomLeft;
                triangles[i++] = topLeft;
                triangles[i++] = bottomRight;
                
                // Second triangle
                triangles[i++] = bottomRight;
                triangles[i++] = topLeft;
                triangles[i++] = topRight;
            }
        }
        
        // Create UVs
        Vector2[] uvs = new Vector2[vertices.Length];
        for (int z = 0; z < length; z++)
        {
            for (int x = 0; x < width; x++)
            {
                uvs[z * width + x] = new Vector2((float)x / width, (float)z / length);
            }
        }

        // Assign data to mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        
        // Calculate normals for proper lighting
        mesh.RecalculateNormals();
        
        return mesh;
    }
}