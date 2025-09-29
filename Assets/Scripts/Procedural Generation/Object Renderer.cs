using System.Collections.Generic;
using UnityEngine;

public class ObjectRenderer : MonoBehaviour
{
    // A dictionary to map prefabs to their mesh and material
    private Dictionary<int, (Mesh mesh, Material material)> renderData = new Dictionary<int, (Mesh, Material)>();
    private EndlessTerrain endlessTerrain;

    private void Start()
    {
        endlessTerrain = GetComponent<EndlessTerrain>();
        CacheRenderData();
    }

    // Pre-process the prefabs to get their mesh and material
    private void CacheRenderData()
    {
        foreach (PlaceableObject placeable in EndlessTerrain.StaticActiveBiomeSO.placeableObjects)
        {
            var prefab = placeable.prefab;
            var meshFilter = prefab.GetComponentInChildren<MeshFilter>();
            var meshRenderer = prefab.GetComponentInChildren<MeshRenderer>();

            if (meshFilter != null && meshRenderer != null)
            {
                renderData[prefab.GetHashCode()] = (meshFilter.sharedMesh, meshRenderer.sharedMaterial);
            }
        }
    }
    
    private void Update()
    {
        // The max number of instances per draw call is 1023
        Matrix4x4[] batch = new Matrix4x4[1023];

        foreach (var item in renderData)
        {
            int prefabHash = item.Key;
            var mesh = item.Value.mesh;
            var material = item.Value.material;

            int batchCount = 0;

            // Iterate through all loaded chunks
            foreach (EndlessTerrain.TerrainChunk chunk in endlessTerrain.TerrainChunkDict.Values)
            {

                if (chunk.meshObject.activeSelf && chunk.ObjectData.ContainsKey(prefabHash))
                {
                    Matrix4x4 chunkTransform = chunk.meshObject.transform.localToWorldMatrix;
                    foreach (var matrix_f4x4 in chunk.ObjectData[prefabHash])
                    {
                        // IMPORTANT: The matrix from the job is local to the chunk.
                        // We must add the chunk's world position.
                        batch[batchCount++] = chunkTransform * (Matrix4x4)matrix_f4x4;
                        if (batchCount == 1023)
                        {
                            Graphics.DrawMeshInstanced(mesh, 0, material, batch, batchCount);
                            batchCount = 0;
                        }
                    }
                }
            }

            // Draw any remaining instances in the last batch
            if (batchCount > 0)
            {
                Graphics.DrawMeshInstanced(mesh, 0, material, batch, batchCount);
            }
        }
    }
}