using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    // Constants for chunk dimensions
    public const int ChunkSideLength = 128;
    public const int ChunkHeight = 512;
    public LayerMask groundLayers;
    
    [Header("Noise Settings")]
    public int seed = 1;
    public bool IsQuitting { get; private set; } = false;

    private void OnApplicationQuit()
    {
        IsQuitting = true;
    }
}