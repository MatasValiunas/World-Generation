using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class Heighmap : MonoBehaviour
{
    public RawImage heightmapImage;
    private string noiseType;

    void Awake()
    {
        heightmapImage.enabled = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            heightmapImage.enabled = !heightmapImage.enabled;
        }

        // Save texture to Desktop when pressing M key
        if (Input.GetKeyDown(KeyCode.M) && heightmapImage.enabled && heightmapImage.texture != null)
        {
            SaveTextureToDesktop(heightmapImage.texture as Texture2D, $"{noiseType} {System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png");
        }
    }

    public void SetHeightmapImage(float[,] heightmap, string noise = "Default", Color[] colors = null)
    {
        noiseType = noise;

        MethodHelper.NormalizeValues(heightmap);
        int width = heightmap.GetLength(0), height = heightmap.GetLength(1);
        Texture2D texture = new Texture2D(width, height) { filterMode = FilterMode.Point };

        if (colors == null || colors.Length == 0) { colors = new Color[] { Color.black, Color.white }; }
        else if (colors.Length == 1) { colors = new Color[] { Color.black, colors[0] }; }

        Gradient gradient = CreateGradient(colors);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float value = Mathf.Clamp01(heightmap[x, y]);
                Color pixelColor = gradient.Evaluate(value);
                pixelColor.a = 1.0f; // force opaque alpha
                texture.SetPixel(x, y, pixelColor);
            }
        }

        texture.Apply();
        heightmapImage.texture = texture;
    }

    private Gradient CreateGradient(Color[] colors)
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


    private void SaveTextureToDesktop(Texture2D originalTexture, string filename)
    {
        int targetWidth = 512, targetHeight = 512;
        Texture2D resizedTexture = new(targetWidth, targetHeight, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };

        int srcWidth = originalTexture.width, srcHeight = originalTexture.height;

        for (int x = 0; x < targetWidth; x++)
        {
            for (int y = 0; y < targetHeight; y++)
            {
                // Use nearest-neighbor sampling
                int srcX = Mathf.FloorToInt((float)x / targetWidth * srcWidth);
                int srcY = Mathf.FloorToInt((float)y / targetHeight * srcHeight);
                srcX = Mathf.Clamp(srcX, 0, srcWidth - 1);
                srcY = Mathf.Clamp(srcY, 0, srcHeight - 1);

                Color color = originalTexture.GetPixel(srcX, srcY);
                resizedTexture.SetPixel(x, y, color);
            }
        }

        resizedTexture.Apply();

        byte[] pngData = resizedTexture.EncodeToPNG();
        if (pngData != null)
        {
            string desktopPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), filename);
            File.WriteAllBytes(desktopPath, pngData);
            Debug.Log($"Heightmap saved to {desktopPath}");
        }
        else
        {
            Debug.LogError("Failed to encode texture to PNG.");
        }
    }
}