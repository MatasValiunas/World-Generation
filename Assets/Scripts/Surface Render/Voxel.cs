using UnityEngine;

public static class Voxel
{
    static GameObject cube = Resources.Load<GameObject>("Cube");

    public static void GenerateTerrain(float[,] heights, Material material = null, bool fixedToGrid = false)
    {
        GameObject terrainParent = new GameObject("Terrain");

        if (material != null)
        {
            MeshRenderer meshRenderer = terrainParent.AddComponent<MeshRenderer>();
            meshRenderer.material = material;
            cube.GetComponent<MeshRenderer>().material = material;
        }

        int width = heights.GetLength(0);
        int length = heights.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                float height;
                if (fixedToGrid) { height = Mathf.Floor(heights[x, z]); }
                else { height = heights[x, z]; }

                for (float y = height; y > 0; y--)
                    Object.Instantiate(cube, new Vector3(x, y, z), Quaternion.identity, terrainParent.transform);
            }
        }
    }
}