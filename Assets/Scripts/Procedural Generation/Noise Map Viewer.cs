using UnityEngine;
using Unity.Mathematics;

public class NoiseMapViewer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag your EndlessTerrain object here.")]
    public EndlessTerrain endlessTerrain;

    [Header("Noise Settings")]
    [Range(-10000, 10000)]
    public float xOffset = 0f;
    [Range(-10000, 10000)]
    public float zOffset = 0f;

    /// <summary>
    /// This public method will be called by our custom editor button.
    /// </summary>
    public void GenerateMap()
    {
        if (endlessTerrain == null)
        {
            Debug.LogError("EndlessTerrain reference is not set in the NoiseMapViewer!");
            return;
        }

        // Clear any existing chunks from previous generations
        endlessTerrain.ClearAllChunks();

        // Tell the EndlessTerrain script to generate a single chunk at coordinate (0,0)
        // using our custom noise offset.
        float2 noiseOffset = new float2(xOffset, zOffset);
        endlessTerrain.RequestChunkGeneration(int2.zero, noiseOffset);
    }
}
