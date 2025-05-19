using Unity.Mathematics;
using UnityEngine;

public enum Noise { White, Perlin, Simplex, Wavelet, Worley, DiamondSquare }
public enum Render { Voxel, Triangulation }

public class TerrainGeneration : MonoBehaviour
{
    [Header("Noise Algorithm")]
    public Noise noiseType;

    [ShowIfEnum("noiseType", Noise.Perlin, Noise.Simplex, Noise.Worley), Min(3)]
    public float scale = 3;

    [ShowIfEnum("noiseType", Noise.DiamondSquare), Min(0.01f)]
    public float Scale = 3;

    [ShowIfEnum("noiseType", Noise.Wavelet), Min(1)]
    public int tileSize = 32;

    [ShowIfEnum("noiseType", Noise.Worley), Min(0.1f)]
    public float cellDensity = 3;

    [ShowIfEnum("noiseType", Noise.DiamondSquare), Range(0.5f, 1)]
    public float roughness = 0.5f;

    [ShowIfEnum("noiseType", Noise.DiamondSquare)]
    public bool forceResize = true;

    [ShowIfEnum("noiseType", Noise.Perlin, Noise.Simplex), Range(0, 1)]
    public float variation;
    

    [Range(0.001f, 3)]
    public float influence1;
    public bool mixing = false;
    public Noise noise2;
    [Range(0, 3)]
    public float influence2 = 0.5f;
    [Min(0.1f)]
    public float scale2;

    public Noise noise3;
    [Range(0, 1)]
    public float influence3 = 0.25f;
    [Min(0.1f)]
    public float scale3;

    [Min(0)]
    public int seed = 0;

    [ShowIfEnum("noiseType", Noise.Simplex), Range(0.2f, 1)]
    public float falloffRadius = 1;

    [ShowIfEnum("noiseType", Noise.Simplex), Min(4)]
    public int gradientCount = 16;


    [ShowIfEnum("noiseType", Noise.Worley), Range(-5, 5)]
    public float[] functions = new float[2];

    [ShowIfEnum("noiseType", Noise.Worley)]
    public bool multiplication;


    [Header("Terrain Rendering")]
    public bool rendering;
    [SerializeField] private Render renderType;

    [Min(0.1f)]
    public float terrainScale = 1;

    [ShowIfEnum("renderType", Render.Voxel)]
    public bool fixedToGrid = true;


    [Header("Terrain Settings")]
    [Min(2)]
    public int terrainDimensions = 15;

    [Min(1)]
    public float maxTerrainHeight = 8;

    [Space(15)]
    [Header("References")]
    public Material texture;
    public Heighmap heightmapScript;
    public Color[] Colors;


    void Start()
    {
        RegenerateNoise();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) { RegenerateNoise(); }

        if (Input.GetKeyDown(KeyCode.N)) { TakeScreenshot(); }
    }

    [ContextMenu("Regenerate Terrain")]
    void RegenerateNoise()
    {
        float[,] heightmap = mixing ? Generation() : GenerateHeightmap();

        RemoveExistingTerrain();
        if (rendering) { RenderTerrain(heightmap); }

        heightmapScript.SetHeightmapImage(heightmap, noiseType.ToString(), Colors);
    }

    float[,] Generation()
    {
        float[,] heightmap1 = GenerateHeightmap();
        float[,] heightmap2 = GenerateLayer(noise2, scale2);
        float[,] heightmap3 = GenerateLayer(noise3, scale3);
        
        // Create weighted combination instead of multiplication
        float[,] heightmap = new float[heightmap1.GetLength(0), heightmap1.GetLength(1)];
        for (int x = 0; x < heightmap.GetLength(0); x++)
        {
            for (int y = 0; y < heightmap.GetLength(1); y++)
            {
                // Full influence for heightmap1, half for heightmap2, quarter for heightmap3
                heightmap[x, y] = (influence1 * heightmap1[x, y]) + (influence2 * heightmap2[x, y]) + (influence3 * heightmap3[x, y]);
            }
        }

        MethodHelper.NormalizeValues(heightmap, maxTerrainHeight);
        return heightmap;
    }

    float[,] GenerateLayer(Noise noise, float scale)
    {
        Noise tempNoise = noiseType;
        float tempScale = this.scale;

        noiseType = noise;
        this.scale = scale;

        float[,] heightmap = GenerateHeightmap();

        noiseType = tempNoise;
        this.scale = tempScale;

        return heightmap;
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
            Noise.Perlin => Perlin.Noise(terrainDimensions, terrainDimensions, maxTerrainHeight, seed, scale, variation),
            Noise.Simplex => Simplex.Noise(terrainDimensions, terrainDimensions, maxTerrainHeight, seed, scale, falloffRadius, gradientCount, variation),
            Noise.Wavelet => Wavelet.Noise(terrainDimensions, maxTerrainHeight, seed, tileSize),
            Noise.Worley => Worley.Noise(terrainDimensions, terrainDimensions, maxTerrainHeight, seed, scale, cellDensity, multiplication, functions),
            Noise.DiamondSquare => DiamondSquare.Noise(terrainDimensions, maxTerrainHeight, seed, roughness, forceResize, Scale),
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
                Triangulation.GenerateTerrain(heightmap, terrainScale, texture, Colors);
                break;
            default:
                throw new System.ArgumentException($"Unsupported render type: {renderType}");
        }
    }

    private void OnValidate()
    {
        FunctionValidator.ValidateFunctions(functions, multiplication);
    }

    void TakeScreenshot()
    {
        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string fileName = $"Screenshot_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
        string path = System.IO.Path.Combine(desktopPath, fileName);

        ScreenCapture.CaptureScreenshot(path);

        Debug.Log($"Screenshot saved to: {path}");
    }
}