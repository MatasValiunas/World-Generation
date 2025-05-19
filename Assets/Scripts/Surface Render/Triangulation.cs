using UnityEngine;

public static class Triangulation
{
    public static GameObject GenerateTerrain(float[,] heightmap, float scale = 1f, Material material = null, Color[] Colors = null)
    {
        GameObject terrain = new GameObject("Terrain");
        terrain.transform.localScale = new Vector3(scale, scale, scale);

        // Create mesh with vertex colors if material is null
        Mesh mesh = CreateMeshFromHeightmap(heightmap, Colors);

        // Apply appropriate material
        MeshRenderer meshRenderer = terrain.AddComponent<MeshRenderer>();
        if (material == null)
        {
            // Use UnlitVertexColor shader when no material is provided
            material = new Material(Shader.Find("Custom/LitVertexColorURP"));
        }
        else
        {
            material = new Material(material);
        }

        meshRenderer.material = material;

        // Apply the mesh
        MeshFilter meshFilter = terrain.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        return terrain;
    }

    private static Mesh CreateMeshFromHeightmap(float[,] heightmap, Color[] colors = null)
    {
        int width = heightmap.GetLength(0), length = heightmap.GetLength(1);

        // Check if we need to use 32-bit indices (for large meshes)
        bool use32BitIndices = width * length > 65000; // Unity's safe limit is around 65k vertices

        // Find min and max heights for normalization
        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;
        for (int z = 0; z < length; z++)
        {
            for (int x = 0; x < width; x++)
            {
                if (heightmap[x, z] < minHeight) minHeight = heightmap[x, z];
                if (heightmap[x, z] > maxHeight) maxHeight = heightmap[x, z];
            }
        }
        float heightRange = maxHeight - minHeight;

        // Create vertices
        Vector3[] vertices = new Vector3[width * length];
        for (int z = 0; z < length; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = z * width + x;
                if (index < vertices.Length) // Added safety check
                {
                    vertices[index] = new Vector3(x, heightmap[x, z], z);
                }
            }
        }

        // Create triangles with error checking
        int triangleCount = (width - 1) * (length - 1) * 6;
        int[] triangles = new int[triangleCount];

        for (int z = 0, index = 0; z < length - 1 && index < triangles.Length - 5; z++)
        {
            for (int x = 0; x < width - 1 && index < triangles.Length - 5; x++)
            {
                int topLeft = z * width + x;
                int topRight = topLeft + 1;
                int bottomLeft = (z + 1) * width + x;
                int bottomRight = bottomLeft + 1;

                // Validate indices are within bounds
                if (topLeft >= vertices.Length || topRight >= vertices.Length ||
                    bottomLeft >= vertices.Length || bottomRight >= vertices.Length)
                {
                    continue; // Skip this quad if any vertex is out of bounds
                }

                // First triangle
                triangles[index++] = topLeft;
                triangles[index++] = bottomLeft;
                triangles[index++] = topRight;

                // Second triangle
                triangles[index++] = topRight;
                triangles[index++] = bottomLeft;
                triangles[index++] = bottomRight;
            }
        }

        // If we didn't fill the entire triangles array due to bounds checking
        if (triangles.Length != triangleCount)
        {
            System.Array.Resize(ref triangles, triangleCount);
        }

        // Create UVs
        Vector2[] uvs = new Vector2[vertices.Length];
        for (int z = 0; z < length; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = z * width + x;
                if (index < uvs.Length) // Added safety check
                {
                    uvs[index] = new Vector2((float)x / width, (float)z / length);
                }
            }
        }

        // Create vertex colors based on heightmap
        Color[] vertexColors = new Color[vertices.Length];

        // Set up color gradient
        Gradient gradient;
        if (colors != null && colors.Length > 0)
        {
            gradient = CreateGradient(colors);
        }
        else
        {
            // Default to black-white gradient to match Heightmap.cs default
            Color[] defaultColors = new Color[] { Color.black, Color.white };
            gradient = CreateGradient(defaultColors);
        }

        // Apply colors based on normalized height values
        for (int z = 0; z < length; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = z * width + x;
                if (index < vertexColors.Length) // Added safety check
                {
                    // Normalize height value between 0 and 1
                    float normalizedHeight = heightRange > 0
                        ? (heightmap[x, z] - minHeight) / heightRange
                        : 0.5f;

                    vertexColors[index] = gradient.Evaluate(normalizedHeight);
                }
            }
        }

        // Assign data to mesh
        Mesh mesh = new Mesh();

        // Enable 32-bit indices if needed for large meshes
        if (use32BitIndices)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.colors = vertexColors;

        // Calculate normals for proper lighting
        mesh.RecalculateNormals();

        // Add bounds optimization
        mesh.RecalculateBounds();

        return mesh;
    }

    private static Gradient CreateGradient(Color[] colors)
    {
        Gradient gradient = new Gradient();
        int numColors = colors.Length;

        GradientColorKey[] colorKeys = new GradientColorKey[numColors];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[numColors];

        for (int i = 0; i < numColors; i++)
        {
            float time = (float)i / (numColors - 1);
            colorKeys[i] = new GradientColorKey(colors[i], time);
            alphaKeys[i] = new GradientAlphaKey(1.0f, time); // Always opaque
        }

        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }
}