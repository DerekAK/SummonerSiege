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


public class EndlessTerrain : MonoBehaviour
{
    public static float isoLevel = 0f;
    public static Vector3 currViewerPosition;

    [Header("Core Settings")]
    public int3 ChunkDimensions;
    [SerializeField] private int seed = 1;
    [SerializeField] private bool shouldBakeNavMesh = false;
    [SerializeField] private LODInfo[] lodInfoList;

    [SerializeField] private Transform viewer;
    [SerializeField] private Material terrainMaterial;
    [SerializeField] private BiomeSO activeBiomeSO;
    private static BiomeSO staticActiveBiomeSO;

    [Header("NavMesh Management")]
    [Tooltip("The single NavMeshSurface component for the entire terrain. Set CollectObjects to 'Children'.")]
    private NavMeshSurface globalNavMeshSurface;
    [Tooltip("How often (in seconds) the NavMesh can be rebuilt.")]
    [SerializeField] private float navMeshUpdateTime = 0.1f; // Reduced time as bakes are much smaller
    private float lastNavMeshUpdateTime;
    private Dictionary<int2, TerrainChunk> terrainChunkDict = new Dictionary<int2, TerrainChunk>();
    private Queue<Bounds> dirtyNavMeshBoundsQueue = new Queue<Bounds>();
    private Queue<int2> chunkCoordsMemoryQueue = new Queue<int2>();
    [SerializeField] private int chunkMemoryLimit;

    // Native Arrays for noise curves
    private static NativeArray<float> continentalnessSamples;
    private static NativeArray<float> erosionSamples;
    private static NativeArray<float> peaksAndValleysSamples;
    private static NativeArray<float> verticalGradientSamples;
    private static NativeArray<float> worleyVerticalGradientSamples;
    private static NativeArray<float2> octaveOffsetsContinentalness;
    private static NativeArray<float2> octaveOffsetsErosion;
    private static NativeArray<float2> octaveOffsetsPeaksAndValleys;
    private static NativeArray<float3> octaveOffsets3D;
    private static NativeArray<float3> octaveOffsetsWarp;
    private static NativeArray<NoiseFunction> continentalnessNoiseFunctions;
    private static NativeArray<NoiseFunction> erosionNoiseFunctions;
    private static NativeArray<NoiseFunction> peaksAndValleysNoiseFunctions;

    private Dictionary<AsyncOperation, Bounds> activeBakeOperations = new Dictionary<AsyncOperation, Bounds>();

    private void Start()
    {
        staticActiveBiomeSO = activeBiomeSO;
        GenerateNewBiomeArrays();
        globalNavMeshSurface = GetComponent<NavMeshSurface>();
        globalNavMeshSurface.navMeshData = new NavMeshData();
    
        currViewerPosition = viewer.position;
        UpdateVisibleChunks();
        StartCoroutine(CheckChunksInRAMCoroutine());
    }

    public TerrainChunk GetChunk(int2 coords)
    {
        return terrainChunkDict[coords];
    }

    private void GenerateNewBiomeArrays()
    {

        // create native arrays for animation curves
        int curveResolution = 256;
        continentalnessSamples = BakeCurve(staticActiveBiomeSO.continentalnessCurve, curveResolution);
        erosionSamples = BakeCurve(staticActiveBiomeSO.erosionCurve, curveResolution);
        peaksAndValleysSamples = BakeCurve(staticActiveBiomeSO.peaksAndValleysCurve, curveResolution);
        verticalGradientSamples = BakeCurve(staticActiveBiomeSO.verticalGradientCurve, curveResolution);
        worleyVerticalGradientSamples = BakeCurve(staticActiveBiomeSO.worleyVerticalGradientCurve, curveResolution);


        // create native arrays for octave offsets
        if (activeBiomeSO.sameOctaveOffsets)
        {
            int maxOctaves = Mathf.Max(
                staticActiveBiomeSO.continentalnessNoise.octaves,
                staticActiveBiomeSO.erosionNoise.octaves,
                staticActiveBiomeSO.peaksAndValleysNoise.octaves
            );
            octaveOffsetsContinentalness = Get2DOctaveOffsets(seed, maxOctaves);
            octaveOffsetsErosion = Get2DOctaveOffsets(seed, maxOctaves);
            octaveOffsetsPeaksAndValleys = Get2DOctaveOffsets(seed, maxOctaves);
        }
        else
        {
            octaveOffsetsContinentalness = Get2DOctaveOffsets(seed, staticActiveBiomeSO.continentalnessNoise.octaves);
            octaveOffsetsErosion = Get2DOctaveOffsets(seed + 10, staticActiveBiomeSO.erosionNoise.octaves);
            octaveOffsetsPeaksAndValleys = Get2DOctaveOffsets(seed + 20, staticActiveBiomeSO.peaksAndValleysNoise.octaves);
        }
        octaveOffsets3D = Get3DOctaveOffsets(seed + 30, staticActiveBiomeSO.threeDNoise.octaves);
        octaveOffsetsWarp = Get3DOctaveOffsets(seed + 40, staticActiveBiomeSO.warpNoise.octaves);


        // create native arrays for noise functions
        continentalnessNoiseFunctions = new NativeArray<NoiseFunction>(staticActiveBiomeSO.continentalnessNoiseFunctions, Allocator.Persistent);
        erosionNoiseFunctions = new NativeArray<NoiseFunction>(staticActiveBiomeSO.erosionNoiseFunctions, Allocator.Persistent);
        peaksAndValleysNoiseFunctions = new NativeArray<NoiseFunction>(staticActiveBiomeSO.peaksAndValleysNoiseFunctions, Allocator.Persistent);
    }

    private void Update()
    {
        if (Vector3.Distance(currViewerPosition, viewer.position) > 0.01f)
        {
            UpdateVisibleChunks();
        }
        currViewerPosition = viewer.position;

        if (shouldBakeNavMesh)
        {
            CleanupFinishedBakeOperations();

            if (dirtyNavMeshBoundsQueue.Count > 0 && Time.time - lastNavMeshUpdateTime > navMeshUpdateTime)
            {
                lastNavMeshUpdateTime = Time.time;
                BakeGlobalNavMeshAsync();
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Draw chunks that are currently being baked in red
        Gizmos.color = Color.red;
        foreach (var bounds in activeBakeOperations.Values)
        {
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        // Draw chunks that are waiting in the queue in yellow
        Gizmos.color = Color.yellow;
        foreach (var bounds in dirtyNavMeshBoundsQueue)
        {
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }

    public void EnqueueChunkCoordsRAM(int2 coords)
    {
        chunkCoordsMemoryQueue.Enqueue(coords);
    }

    private IEnumerator CheckChunksInRAMCoroutine()
    {
        if (chunkCoordsMemoryQueue.Count > chunkMemoryLimit)
        {
            int2 coordsToRemove = chunkCoordsMemoryQueue.Dequeue();
            TerrainChunk chunkToRemove = terrainChunkDict[coordsToRemove];
            chunkToRemove.DestroyChunk();
            terrainChunkDict.Remove(coordsToRemove);
        }
        yield return null;
    }

    public void MarkNavMeshAsDirty(Bounds chunkBounds)
    {
        dirtyNavMeshBoundsQueue.Enqueue(chunkBounds);
    }

    private void BakeGlobalNavMeshAsync()
    {
        if (globalNavMeshSurface == null || dirtyNavMeshBoundsQueue.Count == 0)
        {
            return;
        }

        Bounds boundsToBake = dirtyNavMeshBoundsQueue.Dequeue();

        var sources = new List<NavMeshBuildSource>();

        var markups = new List<NavMeshBuildMarkup>();

        NavMeshBuilder.CollectSources(
            boundsToBake,
            globalNavMeshSurface.layerMask,
            globalNavMeshSurface.useGeometry,
            globalNavMeshSurface.defaultArea,
            markups,
            sources
        );

        // Get the build settings from our surface component
        var buildSettings = globalNavMeshSurface.GetBuildSettings();

        // Call the async builder with the correctly collected sources and specific bounds
        var operation = NavMeshBuilder.UpdateNavMeshDataAsync(
            globalNavMeshSurface.navMeshData,
            buildSettings,
            sources,
            boundsToBake
        );

        activeBakeOperations.Add(operation, boundsToBake); // ✅ ADD THIS LINE
    }

    private void CleanupFinishedBakeOperations()
    {
        if (activeBakeOperations.Count == 0) return;

        // Find all operations that are done
        var finishedOperations = activeBakeOperations.Keys.Where(op => op.isDone).ToList();

        // Remove them from the dictionary
        foreach (var op in finishedOperations)
        {
            activeBakeOperations.Remove(op);
        }
    }
    
    private void UpdateVisibleChunks()
    {
        int maxViewDist = lodInfoList.Last().visibleDistanceThreshold;
        int viewerCoordX = Mathf.RoundToInt(currViewerPosition.x / ChunkDimensions.x);
        int viewerCoordZ = Mathf.RoundToInt(currViewerPosition.z / ChunkDimensions.z);
        int maxChunksFromViewer = Mathf.RoundToInt((float)maxViewDist / ChunkDimensions.x);

        // Create new chunks or update existing ones
        for (int xOffset = -maxChunksFromViewer; xOffset <= maxChunksFromViewer; xOffset++)
        {
            for (int zOffset = -maxChunksFromViewer; zOffset <= maxChunksFromViewer; zOffset++)
            {
                int2 chunkCoord = new int2(viewerCoordX + xOffset, viewerCoordZ + zOffset);

                if (terrainChunkDict.ContainsKey(chunkCoord)) // cached in memory but not necessarily visible or correct LOD
                {
                    terrainChunkDict[chunkCoord].UpdateTerrainChunk();
                }
                else
                {
                    var newChunk = new TerrainChunk(this, chunkCoord, ChunkDimensions, lodInfoList, transform, terrainMaterial);
                    terrainChunkDict.Add(chunkCoord, newChunk);
                }
            }
        }
        List<int2> chunksToUnrender = new List<int2>();
        // Check for chunks to remove
        foreach (var chunkPair in terrainChunkDict)
        {
            float viewerDstFromChunk = Mathf.Sqrt(chunkPair.Value.Bounds.SqrDistance(currViewerPosition));
            if (viewerDstFromChunk > maxViewDist)
            {
                chunksToUnrender.Add(chunkPair.Key);
            }
        }

        // Unrender the chunks that are out of range and put them in recency RAMqueue to be removed later 
        foreach (var coord in chunksToUnrender)
        {
            EnqueueChunkCoordsRAM(coord);
            TerrainChunk chunkToUnrender = terrainChunkDict[coord];
            chunkToUnrender.SetVisible(false);
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
        if (worleyVerticalGradientSamples.IsCreated) worleyVerticalGradientSamples.Dispose();
        if (octaveOffsetsContinentalness.IsCreated) octaveOffsetsContinentalness.Dispose();
        if (octaveOffsetsErosion.IsCreated) octaveOffsetsErosion.Dispose();
        if (octaveOffsetsPeaksAndValleys.IsCreated) octaveOffsetsPeaksAndValleys.Dispose();
        if (octaveOffsets3D.IsCreated) octaveOffsets3D.Dispose();
        if (octaveOffsetsWarp.IsCreated) octaveOffsetsWarp.Dispose();
        if (continentalnessNoiseFunctions.IsCreated) continentalnessNoiseFunctions.Dispose();
        if (erosionNoiseFunctions.IsCreated) erosionNoiseFunctions.Dispose();
        if (peaksAndValleysNoiseFunctions.IsCreated) peaksAndValleysNoiseFunctions.Dispose();

        // Clean up NavMesh data
        if (globalNavMeshSurface != null && globalNavMeshSurface.navMeshData != null)
        {
            globalNavMeshSurface.RemoveData();
        }
    }

    public class TerrainChunk
    {
        public Bounds Bounds { get; private set; }
        public GameObject meshObject;
        private MeshRenderer meshRenderer;
        public MeshFilter meshFilter;
        private LODInfo[] lodInfoList;
        private LODMesh[] lodMeshes;
        private int2 chunkCoord;
        private int previousLODIndex = -1;
        private EndlessTerrain endlessTerrain; // Reference to the parent manager
        private bool hasNotifiedForNavMesh = false; // Flag to prevent spamming updates

        public TerrainChunk(EndlessTerrain endlessTerrain, int2 coord, int3 dimensions, LODInfo[] lodInfoList, Transform parent, Material material)
        {
            this.endlessTerrain = endlessTerrain; // Store the reference
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

            lodMeshes = new LODMesh[lodInfoList.Length];
            for (int i = 0; i < lodMeshes.Length; i++)
            {
                lodMeshes[i] = new LODMesh(this, lodInfoList[i].lod);
            }

            // Set layer for physics, raycasting, etc.
            int groundLayerIndex = LayerMask.NameToLayer("Obstacle");
            meshObject.layer = groundLayerIndex;

            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk()
        {
            float viewerDstFromNearestEdge = Mathf.Sqrt(Bounds.SqrDistance(currViewerPosition));
            bool shouldBeVisible = viewerDstFromNearestEdge <= lodInfoList.Last().visibleDistanceThreshold;

            if (shouldBeVisible)
            {
                SetVisible(true);

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

                        // If this is the highest detail LOD, queue its bounds for a NavMesh bake
                        if (lodIndex == 0 && !hasNotifiedForNavMesh)
                        {
                            endlessTerrain.MarkNavMeshAsDirty(this.Bounds);
                            hasNotifiedForNavMesh = true; // Only notify once
                        }
                    }
                    else if (!lodMesh.isGenerating)
                    {
                        lodMesh.RequestMesh(endlessTerrain, chunkCoord, endlessTerrain.ChunkDimensions);
                    }
                }
            }
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

            // Queue its bounds for an update so the walkable area is removed.
            endlessTerrain.MarkNavMeshAsDirty(this.Bounds);

            Destroy(meshObject);
        }
    }

    class LODMesh
    {
        private TerrainChunk parentChunk;
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

        public LODMesh(TerrainChunk parentChunk, int lod)
        {
            this.parentChunk = parentChunk;
            this.lod = lod;
        }

        public void RequestMesh(EndlessTerrain endlessTerrain, int2 chunkCoord, int3 chunkDimensions)
        {
            isGenerating = true;
            Request3DMesh(endlessTerrain, chunkCoord, chunkDimensions);
        }

        private void Request3DMesh(EndlessTerrain endlessTerrain, int2 chunkCoord, int3 chunkDimensions)
        {
            vertices = new NativeList<float3>(Allocator.TempJob);
            triangles = new NativeList<int>(Allocator.TempJob);

            int step = 1 << lod;
            int3 numPointsPerAxis = chunkDimensions / step + 1;

            densityField = new NativeArray<float>(numPointsPerAxis.x * numPointsPerAxis.y * numPointsPerAxis.z, Allocator.TempJob);
            cubeDensities = new NativeArray<float>(8, Allocator.TempJob);
            edgeVertices = new NativeArray<float3>(12, Allocator.TempJob);

            var job = new ThreeDJob
            {
                chunkCoord = chunkCoord,
                chunkSize = chunkDimensions,
                isoLevel = isoLevel,
                caveStrength = staticActiveBiomeSO.caveStrength,
                lod = lod,
                vertices = vertices,
                triangles = triangles,
                densityField = densityField,
                cubeDensities = cubeDensities,
                edgeVertices = edgeVertices,
                terrainAmplitudeFactor = staticActiveBiomeSO.terrainAmplitudeFactor,
                continentalnessCurveSamples = continentalnessSamples,
                erosionCurveSamples = erosionSamples,
                peaksAndValleysCurveSamples = peaksAndValleysSamples,
                continentalnessNoise = staticActiveBiomeSO.continentalnessNoise,
                erosionNoise = staticActiveBiomeSO.erosionNoise,
                peaksAndValleysNoise = staticActiveBiomeSO.peaksAndValleysNoise,
                threeDNoiseSettings = staticActiveBiomeSO.threeDNoise,
                cavernNoiseSettings = staticActiveBiomeSO.cavernNoise,
                warpNoiseSettings = staticActiveBiomeSO.warpNoise,
                heightBiasForCaves = staticActiveBiomeSO.heightBiasForCaves,
                verticalGradientCurveSamples = verticalGradientSamples,
                worleyVerticalGradientSamples = worleyVerticalGradientSamples,
                octaveOffsetsContinentalness = octaveOffsetsContinentalness,
                octaveOffsetsErosion = octaveOffsetsErosion,
                octaveOffsetsPeaksAndValleys = octaveOffsetsPeaksAndValleys,
                octaveOffsets3D = octaveOffsets3D,
                octaveOffsetsWarp = octaveOffsetsWarp,
                continentalnessNoiseFunctions = continentalnessNoiseFunctions,
                erosionNoiseFunctions = erosionNoiseFunctions,
                peaksAndValleysNoiseFunctions = peaksAndValleysNoiseFunctions
            };

            jobHandle = job.Schedule();
            endlessTerrain.StartCoroutine(WaitForJobCompletion());
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
                if (vertices.IsCreated) vertices.Dispose();
                if (triangles.IsCreated) triangles.Dispose();
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

            mesh.SetVertices(vertices.AsArray());
            mesh.SetTriangles(triangles.AsArray().ToArray(), 0);

            mesh.RecalculateNormals();
            hasMesh = true;

            // parentChunk.meshFilter.mesh = mesh;

            MeshCollider meshCollider = parentChunk.meshObject.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                meshCollider = parentChunk.meshObject.AddComponent<MeshCollider>();
            }
            meshCollider.sharedMesh = mesh;
            parentChunk.UpdateTerrainChunk();
        }

        public void CancelJob()
        {
            if (isGenerating)
            {
                jobHandle.Complete();
                isGenerating = false;
                if (vertices.IsCreated) vertices.Dispose();
                if (triangles.IsCreated) triangles.Dispose();
            }
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