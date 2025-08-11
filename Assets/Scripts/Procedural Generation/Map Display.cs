using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public Renderer textureRenderer;

    public void DrawNoiseMap(float[,] noiseMap)
    {
        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);

        Texture2D texture = new Texture2D(width, height);

        Color[] colorMap = new Color[width * height]; // 1d array storing data from 2d noise map
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                colorMap[y * width + x] = Color.Lerp(Color.black, Color.white, noiseMap[x, y]); // the color mapping is weird here because x represents the column index in picture data for some fucked up reason
            }
        }
        texture.SetPixels(colorMap);
        texture.Apply();

        textureRenderer.sharedMaterial.mainTexture = texture; // use sharedMaterial instead of material so that we can change it outside of play mode
        textureRenderer.transform.localScale = new Vector3(width, 1, height);
    }
}
