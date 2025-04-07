using UnityEngine;

public static class VoxelV2
{
    private static Color topColor = Color.green;
    private static Color bottomColor = new Color(0.545f, 0.271f, 0.075f); // Brown

    public static void GenerateTerrainMesh(float[,] heights, float cubeSize = 1f)
    {
        // Create terrain object with required components
        GameObject terrain = new GameObject("Terrain");
        MeshFilter meshFilter = terrain.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = terrain.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = terrain.AddComponent<MeshCollider>();
        
        // Setup material
        meshRenderer.material = CreateTerrainMaterial();
        
        // Get terrain dimensions
        int width = heights.GetLength(0);
        int depth = heights.GetLength(1);
        
        // Create mesh data containers
        Mesh mesh = new Mesh();
        MeshData meshData = new MeshData();
        
        // Generate mesh data
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                int cubeHeight = Mathf.FloorToInt(heights[x, z]);
                for (int y = 0; y < cubeHeight; y++)
                {
                    // Determine cube visibility
                    bool[] visibleFaces = GetVisibleFaces(heights, width, depth, x, y, z, cubeHeight);
                    
                    // Calculate cube position
                    Vector3 position = new Vector3(x * cubeSize, y * cubeSize, z * cubeSize);
                    
                    // Calculate color based on height
                    Color cubeColor = CalculateCubeColor(y, cubeHeight);
                    
                    // Add cube to mesh
                    AddCube(meshData, position, cubeSize, cubeColor, visibleFaces);
                }
            }
        }
        
        // Apply mesh data
        ApplyMeshData(mesh, meshData);
        
        // Assign mesh
        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;
    }
    
    private static Material CreateTerrainMaterial()
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.EnableKeyword("_EMISSION");
        material.SetFloat("_Smoothness", 0);
        material.SetColor("_BaseColor", Color.white);
        material.SetInt("_Surface", 0); // Opaque
        return material;
    }
    
    private static bool[] GetVisibleFaces(float[,] heights, int width, int depth, int x, int y, int z, int cubeHeight)
    {
        bool[] visibleFaces = new bool[6]; // Bottom, Top, Front, Back, Left, Right
        
        visibleFaces[0] = y == 0; // Bottom
        visibleFaces[1] = y == cubeHeight - 1; // Top
        visibleFaces[2] = z == 0 || (z > 0 && Mathf.FloorToInt(heights[x, z-1]) < y + 1); // Front
        visibleFaces[3] = z == depth - 1 || (z < depth - 1 && Mathf.FloorToInt(heights[x, z+1]) < y + 1); // Back
        visibleFaces[4] = x == 0 || (x > 0 && Mathf.FloorToInt(heights[x-1, z]) < y + 1); // Left
        visibleFaces[5] = x == width - 1 || (x < width - 1 && Mathf.FloorToInt(heights[x+1, z]) < y + 1); // Right
        
        return visibleFaces;
    }
    
    private static Color CalculateCubeColor(int y, int cubeHeight)
    {
        if (cubeHeight == 1) return topColor;
        
        float depthFromTop = (float)(cubeHeight - 1 - y) / (cubeHeight - 1);
        return Color.Lerp(topColor, bottomColor, depthFromTop);
    }
    
    private static void AddCube(MeshData meshData, Vector3 position, float size, Color color, bool[] visibleFaces)
    {
        float halfSize = size * 0.5f;
        Vector3[] cubeVertices = new Vector3[8];
        
        // Define the 8 corners of the cube
        cubeVertices[0] = position + new Vector3(-halfSize, -halfSize, -halfSize); // Bottom Front Left
        cubeVertices[1] = position + new Vector3(halfSize, -halfSize, -halfSize);  // Bottom Front Right
        cubeVertices[2] = position + new Vector3(halfSize, -halfSize, halfSize);   // Bottom Back Right
        cubeVertices[3] = position + new Vector3(-halfSize, -halfSize, halfSize);  // Bottom Back Left
        cubeVertices[4] = position + new Vector3(-halfSize, halfSize, -halfSize);  // Top Front Left
        cubeVertices[5] = position + new Vector3(halfSize, halfSize, -halfSize);   // Top Front Right
        cubeVertices[6] = position + new Vector3(halfSize, halfSize, halfSize);    // Top Back Right
        cubeVertices[7] = position + new Vector3(-halfSize, halfSize, halfSize);   // Top Back Left
        
        // Add visible faces
        if (visibleFaces[0]) AddFace(meshData, new int[] { 0, 1, 2, 3 }, Vector3.down, color); // Bottom
        if (visibleFaces[1]) AddFace(meshData, new int[] { 7, 6, 5, 4 }, Vector3.up, color); // Top
        if (visibleFaces[2]) AddFace(meshData, new int[] { 0, 4, 5, 1 }, Vector3.back, color); // Front
        if (visibleFaces[3]) AddFace(meshData, new int[] { 2, 6, 7, 3 }, Vector3.forward, color); // Back
        if (visibleFaces[4]) AddFace(meshData, new int[] { 3, 7, 4, 0 }, Vector3.left, color); // Left
        if (visibleFaces[5]) AddFace(meshData, new int[] { 1, 5, 6, 2 }, Vector3.right, color); // Right
        
        // Helper method to add a face using corner indices
        void AddFace(MeshData data, int[] cornerIndices, Vector3 normal, Color faceColor)
        {
            int vIndex = data.vertices.Count;
            
            // Add vertices
            for (int i = 0; i < 4; i++)
            {
                data.vertices.Add(cubeVertices[cornerIndices[i]]);
                data.normals.Add(normal);
                data.colors.Add(faceColor);
            }
            
            // Add triangles
            data.triangles.Add(vIndex);
            data.triangles.Add(vIndex + 1);
            data.triangles.Add(vIndex + 2);
            data.triangles.Add(vIndex);
            data.triangles.Add(vIndex + 2);
            data.triangles.Add(vIndex + 3);
        }
    }
    
    private static void ApplyMeshData(Mesh mesh, MeshData data)
    {
        // Check if we need 32-bit index format
        if (data.vertices.Count > 65535)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        
        // Apply mesh data
        mesh.SetVertices(data.vertices);
        mesh.SetTriangles(data.triangles.ToArray(), 0);
        mesh.SetNormals(data.normals);
        mesh.SetColors(data.colors);
        
        // Finalize mesh
        mesh.RecalculateBounds();
        mesh.Optimize();
    }
    
    // Helper class to store mesh data during generation
    private class MeshData
    {
        public System.Collections.Generic.List<Vector3> vertices = new System.Collections.Generic.List<Vector3>();
        public System.Collections.Generic.List<int> triangles = new System.Collections.Generic.List<int>();
        public System.Collections.Generic.List<Color> colors = new System.Collections.Generic.List<Color>();
        public System.Collections.Generic.List<Vector3> normals = new System.Collections.Generic.List<Vector3>();
    }
}