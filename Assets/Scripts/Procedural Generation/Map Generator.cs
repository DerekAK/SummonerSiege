using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    // Constants for chunk dimensions
    public const int ChunkSideLength = 64;
    public const int ChunkHeight = 128;
    public LayerMask groundLayers;
    
    [Header("Noise Settings")]
    public int seed = 1;
    public bool IsQuitting { get; private set; } = false;

    private void OnApplicationQuit()
    {
        IsQuitting = true;
    }
}