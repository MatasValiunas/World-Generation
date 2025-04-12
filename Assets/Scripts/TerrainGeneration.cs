using UnityEngine;

public enum Noise { White, Perlin, Simplex, Wavelet, Worley, DiamondSquare }
public enum Render { Voxel, Triangulation }

public class TerrainGeneration : MonoBehaviour
{
    [Header("Noise Algorithm")]
    public Noise noiseType;

    [ShowIfEnum("noiseType", Noise.Perlin, Noise.Simplex, Noise.Worley), Min(3)]
    public float scale = 3;

    [ShowIfEnum("noiseType", Noise.Wavelet), Min(1)]
    public int tileSize = 32;

    [ShowIfEnum("noiseType", Noise.Worley), Min(0.1f)]
    public float cellDensity = 3;

    [ShowIfEnum("noiseType", Noise.DiamondSquare), Range(0.5f, 1)]
    public float roughness = 0.5f;

    [ShowIfEnum("noiseType", Noise.DiamondSquare)]
    public bool forceResize = true;

    [ShowIfEnum("noiseType", Noise.White, Noise.Simplex, Noise.Wavelet, Noise.Worley, Noise.DiamondSquare), Min(0)]
    public int seed = 0;

    [ShowIfEnum("noiseType", Noise.Simplex), Range(0.2f, 1)]
    public float falloffRadius = 1;

    [ShowIfEnum("noiseType", Noise.Simplex), Min(4)]
    public int gradientCount = 16;

    [ShowIfEnum("noiseType", Noise.Worley)]
    public Worley.Pattern function;


    [Header("Terrain Rendering")]
    [SerializeField] private Render renderType;

    [Min(0.1f)]
    public float terrainScale = 1;

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
        if (Input.GetKeyDown(KeyCode.R))
        {
            RegenerateTerrain();
        }
    }
    
    [ContextMenu("Regenerate Terrain")]
    void RegenerateTerrain()
    {
        RemoveExistingTerrain();
        
        float[,] heightmap = GenerateHeightmap();
        
        RenderTerrain(heightmap);
        
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
            Noise.White => White.Noise(terrainDimensions, terrainDimensions, maxTerrainHeight, seed),
            Noise.Perlin => Perlin.Noise(terrainDimensions, terrainDimensions, maxTerrainHeight, seed, scale),
            Noise.Simplex => Simplex.Noise(terrainDimensions, terrainDimensions, maxTerrainHeight, seed, scale, falloffRadius, gradientCount),
            Noise.Wavelet => Wavelet.Noise(terrainDimensions, maxTerrainHeight, seed, tileSize),
            Noise.Worley => Worley.Noise(terrainDimensions, terrainDimensions, maxTerrainHeight, seed, scale, cellDensity, Worley.GetPattern(function)),
            Noise.DiamondSquare => DiamondSquare.Noise(terrainDimensions, maxTerrainHeight, seed, roughness, forceResize),
            _ => throw new System.ArgumentException($"Unsupported noise type: {noiseType}")
        };
    }
    
    void RenderTerrain(float[,] heightmap)
    {
        switch (renderType)
        {
            case Render.Voxel:
                Voxel.GenerateTerrain(heightmap, terrainScale, texture, fixedToGrid);
                break;
            case Render.Triangulation:
                Triangulation.GenerateTerrain(heightmap, terrainScale, texture);
                break;
            default:
                throw new System.ArgumentException($"Unsupported render type: {renderType}");
        }
    }
}