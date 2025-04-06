using UnityEngine;
using UnityEngine.UI;

public class Heighmap : MonoBehaviour
{
    public RawImage heightmapImage;
    
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
    }

    public void SetHeightmapImage(float[,] heightmap)
    {
        Algorithms.NormalizeValues(heightmap);

        int width = heightmap.GetLength(0), height = heightmap.GetLength(1);

        Texture2D texture = new(width, height){ filterMode = FilterMode.Point };

        // Convert heightmap data into grayscale texture
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float value = heightmap[x, y];
                Color color = new(value, value, value);
                texture.SetPixel(x, y, color);
            }
        }
        
        texture.Apply();
        heightmapImage.texture = texture;
    }
}