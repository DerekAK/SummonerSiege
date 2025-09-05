
using UnityEngine;

public static class TextureGenerator
{
    public static Texture2D GenerateTexture(Color[] colorMap, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colorMap);
        texture.Apply();

        return texture;
    }
}
