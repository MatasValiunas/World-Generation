using UnityEngine;

public static class Voxel
{
    static GameObject cube = Resources.Load<GameObject>("Cube");

    public static void GenerateTerrain(float[,] heightmap, float scale = 1, Material material = null, bool fixedToGrid = false)
    {
        GameObject terrain = new GameObject("Terrain");
        terrain.transform.localScale = new Vector3(scale, scale, scale);

        if (material != null)
        {
            MeshRenderer meshRenderer = terrain.AddComponent<MeshRenderer>();
            meshRenderer.material = material;
            cube.GetComponent<MeshRenderer>().material = material;
        }

        int width = heightmap.GetLength(0);
        int length = heightmap.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                float height;
                if (fixedToGrid) { height = Mathf.Floor(heightmap[x, z]); }
                else { height = heightmap[x, z]; }

                for (float y = height; y > 0; y--)
                    Object.Instantiate(cube, new Vector3(x * scale, y * scale, z * scale), Quaternion.identity, terrain.transform);
            }
        }
    }
}