using UnityEngine;
using Generator;

public enum Noise { WhiteNoise, PerlinNoise, SimplexNoise }

public class TerrainGeneration : MonoBehaviour
{
    [Header("Noise Algorithm")]
    [SerializeField] private Noise selectedNoise;

    [ShowIfEnum("selectedNoise", Noise. PerlinNoise, Noise.SimplexNoise), Min(0.0001f)]
    public float scale = 0.05f;

    [ShowIfEnum("selectedNoise", Noise.SimplexNoise), Min(1)]
    public float influenceRadius = 1.2f;

    [ShowIfEnum("selectedNoise", Noise.PerlinNoise), Min(1)]
    public int resolution = 10;     //  Determines how many noise values are sampled per unit of grid
    

    [Header("Terrain Settings")]
    [Min(1)]
    public int terrainDimensions = 15;
    [Min(1)]
    public int maxTerrainHeight = 8;

    
    [Header("References")]
    public GameObject cubePrefab; // Keep for reference dimensions
    public Heighmap heightmapScript;
    
    // Colors for the terrain gradient
    public Color bottomColor = new Color(0.545f, 0.271f, 0.075f); // Brown
    public Color topColor = Color.green;
    
    void Start()
    {
        float[,] heights = GenerateHeightMap();
        GenerateTerrainMesh(heights);
        heightmapScript.SetHeightmap(heights);
    }
    
    float[,] GenerateHeightMap()
    {
        return selectedNoise switch
        {
            Noise.WhiteNoise => White.Noise(terrainDimensions, terrainDimensions, maxTerrainHeight),
            Noise.PerlinNoise => Perlin.Noise(terrainDimensions, terrainDimensions, maxTerrainHeight, scale, resolution),
            Noise.SimplexNoise => Simplex.Noise(terrainDimensions, terrainDimensions, maxTerrainHeight, scale, influenceRadius),
            _ => throw new System.ArgumentException($"Unsupported noise type: {selectedNoise}")
        };
    }
    
    void GenerateTerrainMesh(float[,] heights)
    {
        // Create a parent GameObject for our terrain
        GameObject terrainParent = new GameObject("Terrain");
        MeshFilter meshFilter = terrainParent.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = terrainParent.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = terrainParent.AddComponent<MeshCollider>();
        
        // Get cube dimensions from the prefab
        Vector3 cubeDimensions = cubePrefab.transform.localScale;
        
        // Create a material
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.EnableKeyword("_EMISSION");
        material.SetFloat("_Smoothness", 0);
        material.SetColor("_BaseColor", Color.white);
        material.SetInt("_Surface", 0); // 0 = Opaque
        
        meshRenderer.material = material;
        
        // Get terrain dimensions
        int width = heights.GetLength(0);
        int depth = heights.GetLength(1);
        
        // Calculate total number of visible cubes
        int totalCubes = 0;
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                totalCubes += Mathf.FloorToInt(heights[x, z]);
            }
        }
        
        // Pre-calculate array sizes
        int vertexCount = totalCubes * 24; // 4 vertices per face, 6 faces
        int triangleCount = totalCubes * 36; // 6 triangles per face, 6 faces
        
        // Initialize arrays with exact sizes
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[triangleCount];
        Color[] colors = new Color[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        
        int vertexIndex = 0;
        int triangleIndex = 0;
        
        // Generate mesh data
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                int cubeHeight = Mathf.FloorToInt(heights[x, z]);
                for (int y = 0; y < cubeHeight; y++)
                {
                    // Determine which faces are visible
                    bool isTopVisible = y == cubeHeight - 1;
                    bool isBottomVisible = y == 0;
                    bool isFrontVisible = z == 0 || (z > 0 && Mathf.FloorToInt(heights[x, z-1]) < y + 1);
                    bool isBackVisible = z == depth - 1 || (z < depth - 1 && Mathf.FloorToInt(heights[x, z+1]) < y + 1);
                    bool isLeftVisible = x == 0 || (x > 0 && Mathf.FloorToInt(heights[x-1, z]) < y + 1);
                    bool isRightVisible = x == width - 1 || (x < width - 1 && Mathf.FloorToInt(heights[x+1, z]) < y + 1);
                    
                    // Calculate position for this cube
                    Vector3 position = new Vector3(
                        x * cubeDimensions.x, 
                        y * cubeDimensions.y, 
                        z * cubeDimensions.z
                    );
                    
                    // Calculate color based on cube's depth from the top
                    float depthFromTop = (float)(cubeHeight - 1 - y) / (cubeHeight - 1);
                    // If there's only one cube (cubeHeight==1), use top color
                    Color cubeColor = cubeHeight == 1 ? 
                        topColor : 
                        Color.Lerp(topColor, bottomColor, depthFromTop);
                    
                    // Add only visible faces to the mesh
                    AddCubeToMesh(
                        position, 
                        cubeDimensions, 
                        vertices, 
                        triangles, 
                        colors,
                        normals,
                        ref vertexIndex, 
                        ref triangleIndex, 
                        cubeColor,
                        isTopVisible,
                        isBottomVisible,
                        isFrontVisible,
                        isBackVisible,
                        isLeftVisible,
                        isRightVisible
                    );
                }
            }
        }
        
        // Resize arrays if needed (in case we didn't use all vertices due to hidden faces)
        if (vertexIndex < vertices.Length)
        {
            System.Array.Resize(ref vertices, vertexIndex);
            System.Array.Resize(ref colors, vertexIndex);
            System.Array.Resize(ref normals, vertexIndex);
            System.Array.Resize(ref triangles, triangleIndex);
        }
        
        // Create the mesh
        Mesh mesh = new Mesh();
        
        // Check if we need 32-bit index buffer
        if (vertexIndex > 65535)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        
        // Set mesh data
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.normals = normals;
        
        // Finalize the mesh
        mesh.RecalculateBounds();
        mesh.Optimize();
        
        // Assign mesh to components
        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;
    }
    
    void AddCubeToMesh(
        Vector3 position, 
        Vector3 size, 
        Vector3[] vertices, 
        int[] triangles, 
        Color[] colors,
        Vector3[] normals,
        ref int vertexIndex, 
        ref int triangleIndex, 
        Color cubeColor,
        bool isTopVisible,
        bool isBottomVisible,
        bool isFrontVisible,
        bool isBackVisible,
        bool isLeftVisible,
        bool isRightVisible
    )
    {
        // Calculate half size for vertex positioning
        Vector3 halfSize = size * 0.5f;
        
        // Add only visible faces
        if (isBottomVisible)
        {
            // Bottom face (using 4 vertices per face instead of sharing)
            vertices[vertexIndex] = position + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
            vertices[vertexIndex + 1] = position + new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
            vertices[vertexIndex + 2] = position + new Vector3(halfSize.x, -halfSize.y, halfSize.z);
            vertices[vertexIndex + 3] = position + new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
            
            // Set normal for all vertices in this face
            normals[vertexIndex] = Vector3.down;
            normals[vertexIndex + 1] = Vector3.down;
            normals[vertexIndex + 2] = Vector3.down;
            normals[vertexIndex + 3] = Vector3.down;
            
            // Set color for all vertices
            colors[vertexIndex] = cubeColor;
            colors[vertexIndex + 1] = cubeColor;
            colors[vertexIndex + 2] = cubeColor;
            colors[vertexIndex + 3] = cubeColor;
            
            // Set triangles
            triangles[triangleIndex] = vertexIndex;
            triangles[triangleIndex + 1] = vertexIndex + 1;
            triangles[triangleIndex + 2] = vertexIndex + 2;
            triangles[triangleIndex + 3] = vertexIndex;
            triangles[triangleIndex + 4] = vertexIndex + 2;
            triangles[triangleIndex + 5] = vertexIndex + 3;
            
            vertexIndex += 4;
            triangleIndex += 6;
        }
        
        if (isTopVisible)
        {
            // Top face
            vertices[vertexIndex] = position + new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
            vertices[vertexIndex + 1] = position + new Vector3(-halfSize.x, halfSize.y, halfSize.z);
            vertices[vertexIndex + 2] = position + new Vector3(halfSize.x, halfSize.y, halfSize.z);
            vertices[vertexIndex + 3] = position + new Vector3(halfSize.x, halfSize.y, -halfSize.z);
            
            normals[vertexIndex] = Vector3.up;
            normals[vertexIndex + 1] = Vector3.up;
            normals[vertexIndex + 2] = Vector3.up;
            normals[vertexIndex + 3] = Vector3.up;
            
            colors[vertexIndex] = cubeColor;
            colors[vertexIndex + 1] = cubeColor;
            colors[vertexIndex + 2] = cubeColor;
            colors[vertexIndex + 3] = cubeColor;
            
            triangles[triangleIndex] = vertexIndex;
            triangles[triangleIndex + 1] = vertexIndex + 1;
            triangles[triangleIndex + 2] = vertexIndex + 2;
            triangles[triangleIndex + 3] = vertexIndex;
            triangles[triangleIndex + 4] = vertexIndex + 2;
            triangles[triangleIndex + 5] = vertexIndex + 3;
            
            vertexIndex += 4;
            triangleIndex += 6;
        }
        
        if (isFrontVisible)
        {
            // Front face
            vertices[vertexIndex] = position + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
            vertices[vertexIndex + 1] = position + new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
            vertices[vertexIndex + 2] = position + new Vector3(halfSize.x, halfSize.y, -halfSize.z);
            vertices[vertexIndex + 3] = position + new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
            
            normals[vertexIndex] = Vector3.back;
            normals[vertexIndex + 1] = Vector3.back;
            normals[vertexIndex + 2] = Vector3.back;
            normals[vertexIndex + 3] = Vector3.back;
            
            colors[vertexIndex] = cubeColor;
            colors[vertexIndex + 1] = cubeColor;
            colors[vertexIndex + 2] = cubeColor;
            colors[vertexIndex + 3] = cubeColor;
            
            triangles[triangleIndex] = vertexIndex;
            triangles[triangleIndex + 1] = vertexIndex + 1;
            triangles[triangleIndex + 2] = vertexIndex + 2;
            triangles[triangleIndex + 3] = vertexIndex;
            triangles[triangleIndex + 4] = vertexIndex + 2;
            triangles[triangleIndex + 5] = vertexIndex + 3;
            
            vertexIndex += 4;
            triangleIndex += 6;
        }
        
        if (isBackVisible)
        {
            // Back face
            vertices[vertexIndex] = position + new Vector3(halfSize.x, -halfSize.y, halfSize.z);
            vertices[vertexIndex + 1] = position + new Vector3(halfSize.x, halfSize.y, halfSize.z);
            vertices[vertexIndex + 2] = position + new Vector3(-halfSize.x, halfSize.y, halfSize.z);
            vertices[vertexIndex + 3] = position + new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
            
            normals[vertexIndex] = Vector3.forward;
            normals[vertexIndex + 1] = Vector3.forward;
            normals[vertexIndex + 2] = Vector3.forward;
            normals[vertexIndex + 3] = Vector3.forward;
            
            colors[vertexIndex] = cubeColor;
            colors[vertexIndex + 1] = cubeColor;
            colors[vertexIndex + 2] = cubeColor;
            colors[vertexIndex + 3] = cubeColor;
            
            triangles[triangleIndex] = vertexIndex;
            triangles[triangleIndex + 1] = vertexIndex + 1;
            triangles[triangleIndex + 2] = vertexIndex + 2;
            triangles[triangleIndex + 3] = vertexIndex;
            triangles[triangleIndex + 4] = vertexIndex + 2;
            triangles[triangleIndex + 5] = vertexIndex + 3;
            
            vertexIndex += 4;
            triangleIndex += 6;
        }
        
        if (isLeftVisible)
        {
            // Left face
            vertices[vertexIndex] = position + new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
            vertices[vertexIndex + 1] = position + new Vector3(-halfSize.x, halfSize.y, halfSize.z);
            vertices[vertexIndex + 2] = position + new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
            vertices[vertexIndex + 3] = position + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
            
            normals[vertexIndex] = Vector3.left;
            normals[vertexIndex + 1] = Vector3.left;
            normals[vertexIndex + 2] = Vector3.left;
            normals[vertexIndex + 3] = Vector3.left;
            
            colors[vertexIndex] = cubeColor;
            colors[vertexIndex + 1] = cubeColor;
            colors[vertexIndex + 2] = cubeColor;
            colors[vertexIndex + 3] = cubeColor;
            
            triangles[triangleIndex] = vertexIndex;
            triangles[triangleIndex + 1] = vertexIndex + 1;
            triangles[triangleIndex + 2] = vertexIndex + 2;
            triangles[triangleIndex + 3] = vertexIndex;
            triangles[triangleIndex + 4] = vertexIndex + 2;
            triangles[triangleIndex + 5] = vertexIndex + 3;
            
            vertexIndex += 4;
            triangleIndex += 6;
        }
        
        if (isRightVisible)
        {
            // Right face
            vertices[vertexIndex] = position + new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
            vertices[vertexIndex + 1] = position + new Vector3(halfSize.x, halfSize.y, -halfSize.z);
            vertices[vertexIndex + 2] = position + new Vector3(halfSize.x, halfSize.y, halfSize.z);
            vertices[vertexIndex + 3] = position + new Vector3(halfSize.x, -halfSize.y, halfSize.z);
            
            normals[vertexIndex] = Vector3.right;
            normals[vertexIndex + 1] = Vector3.right;
            normals[vertexIndex + 2] = Vector3.right;
            normals[vertexIndex + 3] = Vector3.right;
            
            colors[vertexIndex] = cubeColor;
            colors[vertexIndex + 1] = cubeColor;
            colors[vertexIndex + 2] = cubeColor;
            colors[vertexIndex + 3] = cubeColor;
            
            triangles[triangleIndex] = vertexIndex;
            triangles[triangleIndex + 1] = vertexIndex + 1;
            triangles[triangleIndex + 2] = vertexIndex + 2;
            triangles[triangleIndex + 3] = vertexIndex;
            triangles[triangleIndex + 4] = vertexIndex + 2;
            triangles[triangleIndex + 5] = vertexIndex + 3;
            
            vertexIndex += 4;
            triangleIndex += 6;
        }
    }
}