using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public Renderer textureRenderer;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public void DrawTexture(float[,] noiseMap, Color[] colorMap)
    {
        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);

        Texture2D texture = TextureGenerator.GenerateTexture(colorMap, width, height);

        textureRenderer.sharedMaterial.mainTexture = texture; // use sharedMaterial instead of material so that we can change it outside of play mode
        textureRenderer.transform.localScale = new Vector3(width, 1, height);
    }

    public void DrawMesh(MeshData meshData, Texture2D texture)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
    }
}
