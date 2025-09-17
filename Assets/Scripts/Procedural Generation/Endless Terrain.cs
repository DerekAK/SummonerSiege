using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System;
using System.Linq;
using UnityEngine.AI;
using Unity.AI.Navigation;
// --- CHANGE: No longer need System.Threading.Tasks
// using System.Threading.Tasks;

public class EndlessTerrain : MonoBehaviour
{
    public static float isoLevel = 0f;
    public static Vector3 currViewerPosition;

    [SerializeField] private LODInfo[] lodInfoList;
    [SerializeField] private Transform viewer;
    [SerializeField] private Material terrainMaterial;
    [SerializeField] private BiomeSO activeBiomeSO;
    private static BiomeSO staticActiveBiomeSO;

    // Native Arrays for noise curves
    private static NativeArray<float> continentalnessSamples;
    private static NativeArray<float> erosionSamples;
    private static NativeArray<float> peaksAndValleysSamples;
    private static NativeArray<float> verticalGradientSamples;
    private static NativeArray<float> cavernShapeCurveSamples;
    private static NativeArray<float2> octaveOffsetsContinentalness;
    private static NativeArray<float2> octaveOffsetsErosion;
    private static NativeArray<float2> octaveOffsetsPeaksAndValleys;
    private static NativeArray<float3> octaveOffsets3D;
    private static NativeArray<float3> octaveOffsetsWarp;

    private Dictionary<int2, TerrainChunk> terrainChunkDict = new();
    private int3 chunkDimensions;
    private MapGenerator mapGenerator;

    private void Start()
    {
        staticActiveBiomeSO = activeBiomeSO;
        chunkDimensions.x = MapGenerator.ChunkSideLength;
        chunkDimensions.y = MapGenerator.ChunkHeight;
        chunkDimensions.z = MapGenerator.ChunkSideLength;
        mapGenerator = GetComponent<MapGenerator>();
        GenerateNewBiomeArrays();

        UpdateVisibleChunks();
    }


    private void GenerateNewBiomeArrays()
    {
        int curveResolution = 256;
        continentalnessSamples = BakeCurve(staticActiveBiomeSO.continentalnessCurve, curveResolution);
        erosionSamples = BakeCurve(staticActiveBiomeSO.erosionCurve, curveResolution);
        peaksAndValleysSamples = BakeCurve(staticActiveBiomeSO.peaksAndValleysCurve, curveResolution);
        verticalGradientSamples = BakeCurve(staticActiveBiomeSO.verticalGradientCurve, curveResolution);
        cavernShapeCurveSamples = BakeCurve(staticActiveBiomeSO.cavernShapeCurve, curveResolution);

        octaveOffsetsContinentalness = Get2DOctaveOffsets(mapGenerator.seed, staticActiveBiomeSO.continentalnessNoise.octaves);
        octaveOffsetsErosion = Get2DOctaveOffsets(mapGenerator.seed + 10, staticActiveBiomeSO.erosionNoise.octaves);
        octaveOffsetsPeaksAndValleys = Get2DOctaveOffsets(mapGenerator.seed + 20, staticActiveBiomeSO.peaksAndValleysNoise.octaves);
        octaveOffsets3D = Get3DOctaveOffsets(mapGenerator.seed + 30, staticActiveBiomeSO.threeDNoise.octaves);
        octaveOffsetsWarp = Get3DOctaveOffsets(mapGenerator.seed + 40, staticActiveBiomeSO.warpNoise.octaves);
    }

    private void Update()
    {
        currViewerPosition = viewer.position;
        UpdateVisibleChunks();
    }

    public TerrainChunk GetChunk(int2 coord)
    {
        terrainChunkDict.TryGetValue(coord, out TerrainChunk chunk);
        return chunk;
    }

    private void UpdateVisibleChunks()
    {
        int maxViewDist = lodInfoList.Last().visibleDistanceThreshold;
        int viewerCoordX = Mathf.RoundToInt(currViewerPosition.x / chunkDimensions.x);
        int viewerCoordZ = Mathf.RoundToInt(currViewerPosition.z / chunkDimensions.z);
        int maxChunksFromViewer = Mathf.RoundToInt((float)maxViewDist / chunkDimensions.x);

        for (int xOffset = -maxChunksFromViewer; xOffset <= maxChunksFromViewer; xOffset++)
        {
            for (int zOffset = -maxChunksFromViewer; zOffset <= maxChunksFromViewer; zOffset++)
            {
                int2 chunkCoord = new int2(viewerCoordX + xOffset, viewerCoordZ + zOffset);

                if (terrainChunkDict.ContainsKey(chunkCoord))
                {
                    terrainChunkDict[chunkCoord].UpdateTerrainChunk();
                }
                else
                {
                    var newChunk = new TerrainChunk(mapGenerator, chunkCoord, chunkDimensions, lodInfoList, transform, terrainMaterial);
                    terrainChunkDict.Add(chunkCoord, newChunk);
                }
            }
        }

        List<int2> allChunkCoords = terrainChunkDict.Keys.ToList();
        foreach (var coord in allChunkCoords)
        {
            float viewerDstFromChunk = Mathf.Sqrt(terrainChunkDict[coord].Bounds.SqrDistance(currViewerPosition));
            if (viewerDstFromChunk > maxViewDist)
            {
                terrainChunkDict[coord].DestroyChunk();
                terrainChunkDict.Remove(coord);
            }
        }

    }

    private static NativeArray<float> BakeCurve(AnimationCurve curve, int resolution)
    {
        var samples = new NativeArray<float>(resolution, Allocator.Persistent);
        for (int i = 0; i < resolution; i++)
        {
            float time = (float)i / (resolution - 1);
            samples[i] = curve.Evaluate(time);
        }
        return samples;
    }

    private void OnDestroy()
    {
        if (continentalnessSamples.IsCreated) continentalnessSamples.Dispose();
        if (erosionSamples.IsCreated) erosionSamples.Dispose();
        if (peaksAndValleysSamples.IsCreated) peaksAndValleysSamples.Dispose();
        if (verticalGradientSamples.IsCreated) verticalGradientSamples.Dispose();
        if (cavernShapeCurveSamples.IsCreated) cavernShapeCurveSamples.Dispose();
        if (octaveOffsetsContinentalness.IsCreated) octaveOffsetsContinentalness.Dispose();
        if (octaveOffsetsErosion.IsCreated) octaveOffsetsErosion.Dispose();
        if (octaveOffsetsPeaksAndValleys.IsCreated) octaveOffsetsPeaksAndValleys.Dispose();
        if (octaveOffsets3D.IsCreated) octaveOffsets3D.Dispose();
        if (octaveOffsetsWarp.IsCreated) octaveOffsetsWarp.Dispose();
    }

    public class TerrainChunk
    {
        public Bounds Bounds { get; private set; }
        private GameObject meshObject;
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private LODInfo[] lodInfoList;
        private LODMesh[] lodMeshes;
        private int2 chunkCoord;
        private int previousLODIndex = -1;
        private MapGenerator mapGenerator;
        private NavMeshSurface navMeshSurface;
        private bool hasNavMeshBeenBaked = false;
        private bool isBakingNavMesh = false;

        public TerrainChunk(MapGenerator mapGen, int2 coord, int3 dimensions, LODInfo[] lodInfoList, Transform parent, Material material)
        {
            this.chunkCoord = coord;
            this.lodInfoList = lodInfoList;

            Vector3 position = new Vector3(coord.x * dimensions.x, 0, coord.y * dimensions.z);
            this.Bounds = new Bounds(position, new Vector3(dimensions.x, dimensions.y, dimensions.z));

            meshObject = new GameObject($"Chunk {coord.x},{coord.y}");
            meshObject.transform.SetParent(parent);
            meshObject.transform.position = position;

            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;

            navMeshSurface = meshObject.AddComponent<NavMeshSurface>();
            navMeshSurface.collectObjects = CollectObjects.All;
            navMeshSurface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            
            // --- FIX: Instantiate an empty NavMeshData object on the main thread here.
            // This prevents the null reference exception when updating asynchronously.
            navMeshSurface.navMeshData = new NavMeshData();
            
            // --- Add the (currently empty) data to the NavMesh system.
            navMeshSurface.AddData();

            mapGenerator = mapGen;

            lodMeshes = new LODMesh[lodInfoList.Length];
            for (int i = 0; i < lodMeshes.Length; i++)
            {
                lodMeshes[i] = new LODMesh(lodInfoList[i].lod);
            }

            int groundLayerIndex = LayerMask.NameToLayer("Obstacle");

            meshObject.layer = groundLayerIndex;

            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk()
        {
            float viewerDstFromNearestEdge = Mathf.Sqrt(Bounds.SqrDistance(currViewerPosition));
            bool visible = viewerDstFromNearestEdge <= lodInfoList.Last().visibleDistanceThreshold;

            if (visible)
            {
                int lodIndex = 0;
                for (int i = 0; i < lodInfoList.Length - 1; i++)
                {
                    if (viewerDstFromNearestEdge > lodInfoList[i].visibleDistanceThreshold)
                    {
                        lodIndex += 1;
                    }
                    else
                    {
                        break;
                    }
                }

                if (lodIndex != previousLODIndex)
                {
                    LODMesh lodMesh = lodMeshes[lodIndex];
                    if (lodMesh.hasMesh)
                    {
                        previousLODIndex = lodIndex;
                        meshFilter.mesh = lodMesh.mesh;

                        MeshCollider meshCollider = meshObject.GetComponent<MeshCollider>();
                        if (meshCollider == null)
                        {
                            meshCollider = meshObject.AddComponent<MeshCollider>();
                        }
                        meshCollider.sharedMesh = lodMesh.mesh;

                        if (!hasNavMeshBeenBaked && lodIndex == 0 && !isBakingNavMesh)
                        {
                            mapGenerator.StartCoroutine(BakeNavMeshAsync());
                        }
                    }
                    else if (!lodMesh.isGenerating)
                    {
                        lodMesh.RequestMesh(mapGenerator, chunkCoord, Bounds.size);
                    }
                }
            }

            SetVisible(visible);
        }

        // --- CHANGE: Reverted to the simpler, safer coroutine.
        private IEnumerator BakeNavMeshAsync()
        {
            if (navMeshSurface == null || !navMeshSurface.enabled)
            {
                yield break;
            }

            isBakingNavMesh = true;

            // This is the intended high-level async API for NavMeshSurface.
            // It runs in the background and will not throw an error because navMeshData now exists.
            AsyncOperation operation = navMeshSurface.UpdateNavMesh(navMeshSurface.navMeshData);
            
            // Wait for the operation to complete without blocking the main thread.
            yield return operation;

            hasNavMeshBeenBaked = true;
            isBakingNavMesh = false;
        }


        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public void DestroyChunk()
        {
            if (meshObject == null) return;
            
            foreach (var lodMesh in lodMeshes)
            {
                lodMesh.CancelJob();
            }
            if (navMeshSurface != null && navMeshSurface.navMeshData != null)
            {
                navMeshSurface.RemoveData();
            }
            Destroy(meshObject);
        }

    }

    class LODMesh
    {
        public Mesh mesh;
        public bool hasMesh;
        public bool isGenerating;
        public int lod;
        private JobHandle jobHandle;

        private NativeList<float3> vertices;
        private NativeList<int> triangles;
        private NativeArray<float> densityField;
        private NativeArray<float> cubeDensities;
        private NativeArray<float3> edgeVertices;

        public LODMesh(int lod)
        {
            this.lod = lod;
        }

        public void RequestMesh(MapGenerator mapGen, int2 chunkCoord, Vector3 chunkDimensions)
        {
            isGenerating = true;
            Request3DMesh(mapGen, chunkCoord, chunkDimensions);
        }

        private void Request3DMesh(MapGenerator mapGen, int2 chunkCoord, Vector3 chunkDimensions)
        {
            vertices = new NativeList<float3>(Allocator.Persistent);
            triangles = new NativeList<int>(Allocator.Persistent);

            int3 dimensions = new int3((int)chunkDimensions.x, (int)chunkDimensions.y, (int)chunkDimensions.z);

            int step = 1 << this.lod;
            int3 numPointsPerAxis = dimensions / step + 1;

            densityField = new NativeArray<float>(numPointsPerAxis.x * numPointsPerAxis.y * numPointsPerAxis.z, Allocator.Persistent);
            cubeDensities = new NativeArray<float>(8, Allocator.Persistent);
            edgeVertices = new NativeArray<float3>(12, Allocator.Persistent);

            var job = new ThreeDJob
            {
                chunkCoord = chunkCoord,
                chunkSize = dimensions,
                isoLevel = EndlessTerrain.isoLevel,
                lod = this.lod,
                vertices = vertices,
                triangles = triangles,
                densityField = densityField,
                cubeDensities = cubeDensities,
                edgeVertices = edgeVertices,
                terrainAmplitude = staticActiveBiomeSO.terrainAmplitude,
                continentalnessCurveSamples = continentalnessSamples,
                erosionCurveSamples = erosionSamples,
                peaksAndValleysCurveSamples = peaksAndValleysSamples,
                continentalnessNoise = staticActiveBiomeSO.continentalnessNoise,
                erosionNoise = staticActiveBiomeSO.erosionNoise,
                peaksAndValleysNoise = staticActiveBiomeSO.peaksAndValleysNoise,
                threeDNoiseSettings = staticActiveBiomeSO.threeDNoise,
                cavernNoiseSettings = staticActiveBiomeSO.cavernNoise,
                warpNoiseSettings = staticActiveBiomeSO.warpNoise,
                verticalGradientCurveSamples = verticalGradientSamples,
                cavernShapeCurveSamples = cavernShapeCurveSamples,
                octaveOffsetsContinentalness = octaveOffsetsContinentalness,
                octaveOffsetsErosion = octaveOffsetsErosion,
                octaveOffsetsPeaksAndValleys = octaveOffsetsPeaksAndValleys,
                octaveOffsets3D = octaveOffsets3D,
                octaveOffsetsWarp = octaveOffsetsWarp
            };

            jobHandle = job.Schedule();
            mapGen.StartCoroutine(WaitForJobCompletion());
        }

        private IEnumerator WaitForJobCompletion()
        {
            while (!jobHandle.IsCompleted)
            {
                yield return null;
            }

            try
            {
                jobHandle.Complete();

                if (!isGenerating) { yield break; }

                CreateMesh();
            }
            finally
            {
                DisposeArrays();
                isGenerating = false;
            }
        }

        private void CreateMesh()
        {
            if (vertices.Length == 0) return;

            mesh = new Mesh();

            if (vertices.Length > 65535)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            Vector3[] unityVertices = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                unityVertices[i] = vertices[i];
            }
            mesh.vertices = unityVertices;
            mesh.triangles = triangles.AsArray().ToArray();
            mesh.RecalculateNormals();
            hasMesh = true;
        }

        public void CancelJob()
        {
            if (isGenerating)
            {
                try
                {
                    jobHandle.Complete();
                }
                finally
                {
                    isGenerating = false;
                    DisposeArrays();
                }
            }
        }

        private void DisposeArrays()
        {
            if (vertices.IsCreated) vertices.Dispose();
            if (triangles.IsCreated) triangles.Dispose();
            if (densityField.IsCreated) densityField.Dispose();
            if (cubeDensities.IsCreated) cubeDensities.Dispose();
            if (edgeVertices.IsCreated) edgeVertices.Dispose();
        }
    }

    private static NativeArray<float2> Get2DOctaveOffsets(int seed, int octaves)
    {
        System.Random prng = new(seed);
        NativeArray<float2> octaveOffsets = new NativeArray<float2>(octaves, Allocator.Persistent);
        for (int i = 0; i < octaves; i++)
        {
            float xOffset = prng.Next(-100000, 100000);
            float yOffset = prng.Next(-100000, 100000);

            octaveOffsets[i] = new float2(xOffset, yOffset);
        }

        return octaveOffsets;
    }

    private static NativeArray<float3> Get3DOctaveOffsets(int seed, int octaves)
    {
        System.Random prng = new(seed);
        NativeArray<float3> octaveOffsets = new NativeArray<float3>(octaves, Allocator.Persistent);

        for (int i = 0; i < octaves; i++)
        {
            float xOffset = prng.Next(-100000, 100000);
            float yOffset = prng.Next(-100000, 100000);
            float zOffset = prng.Next(-100000, 100000);
            octaveOffsets[i] = new float3(xOffset, yOffset, zOffset);
        }
        return octaveOffsets;
    }

    [Serializable]
    public struct LODInfo
    {
        public int lod;
        public int visibleDistanceThreshold;
    }
}