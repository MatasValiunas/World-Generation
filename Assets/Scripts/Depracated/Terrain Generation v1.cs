/*using UnityEngine;
using Generator;

public class TerrainGeneration : MonoBehaviour
{
    [SerializeField] private Noise selectedNoise;
    public enum Noise { WhiteNoise, PerlinNoise }

    [Header("Terrain Settings")]
    public int terrainDimensions = 15;
    public int maxTerrainHeight = 8;

    [Header("etc.")]
    public GameObject cubePrefab;
    public Heighmap heightmapScript;
    

    void Start()
    {
        float[,] heights = GenerateHeightMap();

        GenerateTerrain(heights);

        heightmapScript.SetHeightmap(heights);
    }

    float[,] GenerateHeightMap()
    {
        return selectedNoise switch
        {
            Noise.WhiteNoise => Noises.White(terrainDimensions, terrainDimensions, maxTerrainHeight),
            Noise.PerlinNoise => Noises.Perlin(terrainDimensions, terrainDimensions, maxTerrainHeight),
            _ => throw new System.ArgumentException($"Unsupported noise type: {selectedNoise}")
        };
    }

    void GenerateTerrain(float[,] heights)
    {
        GameObject terrainParent = new("Terrain");
        Vector3 cubeDimensions = cubePrefab.transform.localScale;
        int width = heights.GetLength(0), height = heights.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                for (int y = 0; y < heights[x, z]; y++)
                {
                    Vector3 position = new(x * cubeDimensions.x, y * cubeDimensions.y, z * cubeDimensions.z);
                    GameObject cube = Instantiate(cubePrefab, position, Quaternion.identity, terrainParent.transform);

                    // Color the cubes based on their height (brown to green)
                    if (cube.TryGetComponent<Renderer>(out Renderer cubeRenderer))
                    {
                        float normalizedHeight = (float)y / maxTerrainHeight;
                        cubeRenderer.material.color = Color.Lerp(new Color(0.545f, 0.271f, 0.075f), Color.green, normalizedHeight);
                    }
                }
            }
        }
    }
}*/