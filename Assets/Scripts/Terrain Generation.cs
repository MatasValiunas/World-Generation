using UnityEngine;

public enum Noise { WhiteNoise, PerlinNoise, SimplexNoise, SimplexNoiseV2, WaveletNoise, WorleyNoise, DiamondSquareNoise }
public enum Render { Voxel, Triangulation, MarchingCubes, DelaunayTriangulation }

public class TerrainGeneration : MonoBehaviour
{
    [Header("Noise Algorithm")]
    [SerializeField] private Noise noiseType;

    [ShowIfEnum("noiseType", Noise.PerlinNoise /*>3*/, Noise.SimplexNoise /*>3*/, Noise.WorleyNoise), Min(1)]
    public float scale = 3;

    [ShowIfEnum("noiseType", Noise.SimplexNoise), Min(1)]
    public float influenceRadius = 1.2f;

    [ShowIfEnum("noiseType", Noise.WaveletNoise), Min(1)]
    public int tileSize = 32;

    [ShowIfEnum("noiseType", Noise.WorleyNoise), Min(0.1f)]
    public float cellDensity = 3;

    [ShowIfEnum("noiseType", Noise.DiamondSquareNoise), Range(0.1f, 1)]
    public float roughness = 0.5f;

    [ShowIfEnum("noiseType", Noise.WhiteNoise, Noise.SimplexNoise, Noise.WorleyNoise, Noise.DiamondSquareNoise), Min(0)]
    public int seed = 0;
    
    [Header("Terrain Rendering")]
    [SerializeField] private Render renderType;

    [ShowIfEnum("renderType", Render.Voxel), Min(0.1f)]
    public float cubeSize = 1f;

    [ShowIfEnum("renderType", Render.Voxel)]
    public bool fixedToGrid = true;
    
    [Header("Terrain Settings")]
    [Min(1)]
    public int terrainDimensions = 15;

    [Min(1)]
    public int maxTerrainHeight = 8;
    
    [Space(15)]
    [Header("References")]
    public Heighmap heightmapScript;
    public Material texture;
   

    void Start()
    {
        RegenerateTerrain();
    }
    
    void Update()
    {
        // Check for R key press to regenerate terrain
        if (Input.GetKeyDown(KeyCode.R))
        {
            RegenerateTerrain();
        }
    }
    
    [ContextMenu("Regenerate Terrain")]
    void RegenerateTerrain()
    {
        // Remove any previously generated terrain
        RemoveExistingTerrain();
        
        // Generate new heightmap
        float[,] heightmap = GenerateHeightmap();
        
        // Render terrain with the new heightmap
        RenderTerrain(heightmap);
        
        // Update the heightmap visualization
        heightmapScript.SetHeightmapImage(heightmap);
    }
    
    void RemoveExistingTerrain()
    {
        DestroyImmediate(GameObject.Find("Terrain"));
    }
   
    float[,] GenerateHeightmap()
    {
        return noiseType switch
        {
            Noise.WhiteNoise => White.Noise(terrainDimensions, terrainDimensions, maxTerrainHeight, seed),
            Noise.PerlinNoise => Perlin.Noise(terrainDimensions, terrainDimensions, maxTerrainHeight, scale, seed),
            Noise.SimplexNoise => Simplex.Noise(terrainDimensions, terrainDimensions, maxTerrainHeight, scale, influenceRadius, seed),
            Noise.SimplexNoiseV2 => SimplexNoise2D.GenerateHeightmap(terrainDimensions, terrainDimensions, scale, maxTerrainHeight, new Vector2(64, 64)),
            Noise.WaveletNoise => Wavelet.GenerateHeightmap(terrainDimensions, tileSize),
            Noise.WorleyNoise => Worley.GenerateHeightmap(terrainDimensions, terrainDimensions, maxTerrainHeight, scale, cellDensity, seed),
            //Noise.WorleyNoise => Worley.GenerateFractalHeightmap(terrainDimensions, terrainDimensions, maxTerrainHeight, cellDensity, scale),
            Noise.DiamondSquareNoise => DiamondSquare.GenerateHeightmap(terrainDimensions, maxTerrainHeight, roughness, seed),
            _ => throw new System.ArgumentException($"Unsupported noise type: {noiseType}")
        };
    }
    
    void RenderTerrain(float[,] heightmap)
    {
        switch (renderType)
        {
            case Render.Voxel:
                Voxel.GenerateTerrain(heightmap, texture, fixedToGrid);
                break;
            case Render.Triangulation:
                Triangulation.CreateVoxelObject(heightmap, texture);
                break;
            case Render.MarchingCubes:
            case Render.DelaunayTriangulation:
            default:
                throw new System.ArgumentException($"Unsupported render type: {renderType}");
        }
    }
}