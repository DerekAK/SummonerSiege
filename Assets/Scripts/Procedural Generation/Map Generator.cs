using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] private int mapWidth;
    [SerializeField] private int mapHeight;
    [SerializeField] private float scale;
    [SerializeField] private int octaves;
    [SerializeField] private float lacunarity = 2;
    [SerializeField] private float persistence = 0.5f;
    [SerializeField] private int seed;
    [SerializeField] private Vector2 scrollOffset;
    public bool AutoUpdate;

    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, scale, octaves, lacunarity, persistence, seed, scrollOffset);

        MapDisplay mapDisplay = GetComponent<MapDisplay>();

        mapDisplay.DrawNoiseMap(noiseMap);
    }
    
}
