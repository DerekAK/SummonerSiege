// TerrainDebugger.cs
using UnityEngine;
using Unity.Mathematics;

public class TerrainDebugger : MonoBehaviour
{
    // Drag your EndlessTerrain object here in the Inspector
    [SerializeField] private EndlessTerrain endlessTerrain;
    
    // We'll store the chunk we click on here
    private EndlessTerrain.TerrainChunk selectedChunk;
    private int2 selectedChunkCoord;

    public void FindChunkFromWorldPos(Vector3 worldPos)
    {
        // This is the same logic you use to find the viewer's current chunk
        int coordX = Mathf.RoundToInt(worldPos.x / endlessTerrain.ChunkDimensions.x);
        int coordZ = Mathf.RoundToInt(worldPos.z / endlessTerrain.ChunkDimensions.x);
        selectedChunkCoord = new int2(coordX, coordZ);

        // Try to get the chunk from the dictionary in EndlessTerrain
        selectedChunk = endlessTerrain.GetChunk(selectedChunkCoord);

        if (selectedChunk != null)
        {
            Debug.Log($"Selected Chunk: {selectedChunkCoord.x}, {selectedChunkCoord.y}");
        }
        else
        {
            Debug.LogWarning($"No chunk found at coordinate: {selectedChunkCoord.x}, {selectedChunkCoord.y}");
        }
    }

    // This function draws helpers in the Scene view
    private void OnDrawGizmos()
    {
        if (selectedChunk != null)
        {
            // Draw a bright green wireframe cube around the selected chunk's bounds
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(new Vector3(selectedChunk.Bounds.center.x, endlessTerrain.ChunkDimensions.y/2, selectedChunk.Bounds.center.z), selectedChunk.Bounds.size);
        }
    }
}