using UnityEngine;

public enum Noise { White, Perlin, Simplex, Wavelet, Worley, DiamondSquare }
public enum Render { Voxel, Triangulation, MarchingCubes, DelaunayTriangulation }

public class TerrainGeneration : MonoBehaviour
{
    [Header("Noise Algorithm")]
    [SerializeField] private Noise noiseType;

    [ShowIfEnum("noiseType", Noise.Worley), Min(1)]
    public float scale = 3;

    [ShowIfEnum("noiseType", Noise.Perlin, Noise.Simplex), Min(2.5f)]
    public float Scale = 3;

    [ShowIfEnum("noiseType", Noise.Wavelet), Min(1)]
    public int tileSize = 32;

    [ShowIfEnum("noiseType", Noise.Worley), Min(0.1f)]
    public float cellDensity = 3;

    [ShowIfEnum("noiseType", Noise.DiamondSquare), Range(0.5f, 1)]
    public float roughness = 0.5f;

    [ShowIfEnum("noiseType", Noise.DiamondSquare)]
    public bool forceResize;

    [ShowIfEnum("noiseType", Noise.White, Noise.Simplex, Noise.Wavelet, Noise.Worley, Noise.DiamondSquare), Min(0)]
    public int seed;

    [ShowIfEnum("noiseType", Noise.Simplex), Range(0.2f, 1)]
    public float falloffRadius;

    [ShowIfEnum("noiseType", Noise.Simplex), Min(4)]
    public int gradientCount;


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
            Noise.Perlin => Perlin.Noise(terrainDimensions, terrainDimensions, maxTerrainHeight, seed, Scale),
            Noise.Simplex => Simplex.Noise(terrainDimensions, terrainDimensions, maxTerrainHeight, seed, Scale, falloffRadius, gradientCount),
            Noise.Wavelet => Wavelet.Noise(terrainDimensions, maxTerrainHeight, seed, tileSize),
            Noise.Worley => Worley.Noise(terrainDimensions, terrainDimensions, maxTerrainHeight, seed, scale, cellDensity),
            //Noise.Worley => Worley.GenerateFractalHeightmap(terrainDimensions, terrainDimensions, maxTerrainHeight, cellDensity, scale),
            Noise.DiamondSquare => DiamondSquare.Noise(terrainDimensions, maxTerrainHeight, seed, roughness, forceResize),
            _ => throw new System.ArgumentException($"Unsupported noise type: {noiseType}")
        };
    }
    
    void RenderTerrain(float[,] heightmap)
    {
        switch (renderType)
        {
            case Render.Voxel:
                Voxel.GenerateTerrain(heightmap, texture, cubeSize, fixedToGrid);
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