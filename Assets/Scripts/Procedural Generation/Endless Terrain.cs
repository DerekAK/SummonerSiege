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
using Unity.Entities;

public class EndlessTerrain : MonoBehaviour
{
    public float isoLevel = 0f;
    public Vector3 viewerPosition;

    [Header("Core Settings")]
    public int3 ChunkDimensions;
    [SerializeField] private int seed = 1;
    [SerializeField] private bool shouldBakeNavMesh;
    [SerializeField] private bool shouldPlaceObjects;
    [SerializeField] private LODInfo[] lodInfoList;
    private int highestLOD;

    [SerializeField] private Transform viewer;
    [SerializeField] private Material terrainMaterial;
    [SerializeField] private BiomeSO activeBiomeSO;
    [SerializeField] GameObject pfTerrainChunk;
    private List<int2> previouslyVisibleChunks = new();

    [Header("NavMesh Management")]
    [Tooltip("The single NavMeshSurface component for the entire terrain. Set CollectObjects to 'Children'.")]
    private NavMeshSurface globalNavMeshSurface;
    [Tooltip("How often (in seconds) the NavMesh can be rebuilt.")]
    [SerializeField] private float navMeshUpdateTime = 0.1f; // Reduced time as bakes are much smaller
    private float lastNavMeshUpdateTime;
    public Dictionary<int2, TerrainChunk> TerrainChunkDict = new Dictionary<int2, TerrainChunk>();
    private Queue<Bounds> dirtyNavMeshBoundsQueue = new Queue<Bounds>();

    // Native Arrays for noise curves
    private NativeArray<float> continentalnessSamples;
    private  NativeArray<float> erosionSamples;
    private  NativeArray<float> peaksAndValleysSamples;
    private  NativeArray<float> verticalGradientSamples;
    private  NativeArray<float> worleyVerticalGradientSamples;
    private  NativeArray<float2> octaveOffsetsContinentalness;
    private  NativeArray<float2> octaveOffsetsErosion;
    private  NativeArray<float2> octaveOffsetsPeaksAndValleys;
    private  NativeArray<float3> octaveOffsets3D;
    private  NativeArray<float3> octaveOffsetsWarp;
    private  NativeArray<NoiseFunction> continentalnessNoiseFunctions;
    private  NativeArray<NoiseFunction> erosionNoiseFunctions;
    private  NativeArray<NoiseFunction> peaksAndValleysNoiseFunctions;
    private  Dictionary<int, NativeArray<float3>> octaveOffsetsPlaceableObjectsDict = new();
    private Dictionary<AsyncOperation, Bounds> activeBakeOperations = new();

    private void Start()
    {
        int numChunks = InitializeChunkPool();
        InitializePlacementPools(numChunks);
        globalNavMeshSurface = GetComponent<NavMeshSurface>();
        globalNavMeshSurface.navMeshData = new NavMeshData();
        viewerPosition = viewer.position;
        GenerateNewBiomeArrays();
        UpdateVisibleChunks();
        highestLOD = lodInfoList[0].lod;
    }

    private int InitializeChunkPool()
    {
        int chunkBuffer = 0;
        float maxDistancePerSide = lodInfoList[lodInfoList.Length - 1].visibleDistanceThreshold;
        int maxChunksX = (int)Mathf.Ceil(maxDistancePerSide / ChunkDimensions.x) * 2 + 1;
        int maxChunksZ = (int)Mathf.Ceil(maxDistancePerSide / ChunkDimensions.x) * 2 + 1;

        int numChunks = maxChunksX * maxChunksZ + chunkBuffer;

        ObjectPoolManager.Singleton.RegisterPrefab(pfTerrainChunk, numChunks);

        return numChunks;
    }

    private void InitializePlacementPools(int numChunks)
    {
        int initialAmountPerPf = 1000;
        foreach (PlaceableObject placeable in activeBiomeSO.placeableObjects)
        {
            ObjectPoolManager.Singleton.RegisterPrefab(placeable.prefab, initialAmountPerPf);
        }
    }


    public TerrainChunk GetChunk(int2 coords)
    {
        return TerrainChunkDict[coords];
    }

    private void GenerateNewBiomeArrays()
    {

        // create native arrays for animation curves
        int curveResolution = 256;
        continentalnessSamples = BakeCurve(activeBiomeSO.continentalnessCurve, curveResolution);
        erosionSamples = BakeCurve(activeBiomeSO.erosionCurve, curveResolution);
        peaksAndValleysSamples = BakeCurve(activeBiomeSO.peaksAndValleysCurve, curveResolution);
        verticalGradientSamples = BakeCurve(activeBiomeSO.verticalGradientCurve, curveResolution);
        worleyVerticalGradientSamples = BakeCurve(activeBiomeSO.worleyVerticalGradientCurve, curveResolution);


        // create native arrays for octave offsets
        if (activeBiomeSO.sameOctaveOffsets)
        {
            int maxOctaves = Mathf.Max(
                activeBiomeSO.continentalnessNoise.octaves,
                activeBiomeSO.erosionNoise.octaves,
                activeBiomeSO.peaksAndValleysNoise.octaves
            );
            octaveOffsetsContinentalness = Noise.Get2DOctaveOffsets(seed, maxOctaves);
            octaveOffsetsErosion = Noise.Get2DOctaveOffsets(seed, maxOctaves);
            octaveOffsetsPeaksAndValleys = Noise.Get2DOctaveOffsets(seed, maxOctaves);
        }
        else
        {
            octaveOffsetsContinentalness = Noise.Get2DOctaveOffsets(seed, activeBiomeSO.continentalnessNoise.octaves);
            octaveOffsetsErosion = Noise.Get2DOctaveOffsets(seed + 10, activeBiomeSO.erosionNoise.octaves);
            octaveOffsetsPeaksAndValleys = Noise.Get2DOctaveOffsets(seed + 20, activeBiomeSO.peaksAndValleysNoise.octaves);
        }
        octaveOffsets3D = Noise.Get3DOctaveOffsets(seed + 30, activeBiomeSO.threeDNoise.octaves);
        octaveOffsetsWarp = Noise.Get3DOctaveOffsets(seed + 40, activeBiomeSO.warpNoise.octaves);

        // create native arrays for noise functions
        continentalnessNoiseFunctions = new NativeArray<NoiseFunction>(activeBiomeSO.continentalnessNoiseFunctions, Allocator.Persistent);
        erosionNoiseFunctions = new NativeArray<NoiseFunction>(activeBiomeSO.erosionNoiseFunctions, Allocator.Persistent);
        peaksAndValleysNoiseFunctions = new NativeArray<NoiseFunction>(activeBiomeSO.peaksAndValleysNoiseFunctions, Allocator.Persistent);

        int seedOffset = 50;
        foreach (PlaceableObject placeableObject in activeBiomeSO.placeableObjects)
        {
            octaveOffsetsPlaceableObjectsDict[placeableObject.prefab.GetHashCode()] = Noise.Get3DOctaveOffsets(seedOffset, placeableObject.placementNoise.octaves);
            seedOffset += 10;
        }

    }

    private void Update()
    {
        if (Vector3.Distance(viewerPosition, viewer.position) > 0.01f)
        {
            UpdateVisibleChunks();
        }
        viewerPosition = viewer.position;

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

        activeBakeOperations.Add(operation, boundsToBake); 
    }

    private void CleanupFinishedBakeOperations()
    {
        if (activeBakeOperations.Count == 0) return;
        var finishedOperations = new List<AsyncOperation>(); 
        foreach (var kvp in activeBakeOperations)
        {
            if (kvp.Key.isDone)
            {
                finishedOperations.Add(kvp.Key);
            }
        }
        foreach (var op in finishedOperations)
        {
            activeBakeOperations.Remove(op);
        }
    }

    private void UpdateVisibleChunks()
    {

        previouslyVisibleChunks.Clear();
        foreach (int2 coord in TerrainChunkDict.Keys)
        {
            previouslyVisibleChunks.Add(coord);
        }

        int maxViewDist = lodInfoList.Last().visibleDistanceThreshold;
        int viewerCoordX = Mathf.RoundToInt(viewerPosition.x / ChunkDimensions.x);
        int viewerCoordZ = Mathf.RoundToInt(viewerPosition.z / ChunkDimensions.z);
        int maxChunksFromViewer = Mathf.RoundToInt((float)maxViewDist / ChunkDimensions.x);

        // Create new chunks or update existing ones
        for (int xOffset = -maxChunksFromViewer; xOffset <= maxChunksFromViewer; xOffset++)
        {
            for (int zOffset = -maxChunksFromViewer; zOffset <= maxChunksFromViewer; zOffset++)
            {
                int2 chunkCoord = new int2(viewerCoordX + xOffset, viewerCoordZ + zOffset);
                Vector3 position = new(chunkCoord.x * ChunkDimensions.x, 0, chunkCoord.y * ChunkDimensions.z);
                Bounds tempBounds = new Bounds(position, new Vector3(ChunkDimensions.x, ChunkDimensions.y, ChunkDimensions.z));
                float viewerDstFromChunk = Mathf.Sqrt(tempBounds.SqrDistance(viewerPosition));

                if (viewerDstFromChunk <= maxViewDist)
                {
                    previouslyVisibleChunks.Remove(chunkCoord);
                    if (TerrainChunkDict.ContainsKey(chunkCoord)) // was visible last rendering and should be visible this rendering, but might be different lod
                    {
                        TerrainChunkDict[chunkCoord].UpdateTerrainChunk();
                    }
                    else // was not visible last rendering but should be, so create a new chunk (not new gameobject, get one from object pool)
                    {
                        var newChunk = new TerrainChunk(this, chunkCoord);
                        TerrainChunkDict.Add(chunkCoord, newChunk);
                    }
                }
            }
        }
        foreach (var coord in previouslyVisibleChunks)
        {
            TerrainChunk chunkToUnrender = TerrainChunkDict[coord];
            chunkToUnrender.HandleChunkRemoval();
            TerrainChunkDict.Remove(coord);
        }
    }

    private  NativeArray<float> BakeCurve(AnimationCurve curve, int resolution)
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

        foreach (var octaveOffsets in octaveOffsetsPlaceableObjectsDict)
        {
            octaveOffsets.Value.Dispose();
        }
        octaveOffsetsPlaceableObjectsDict.Clear();

        // Clean up NavMesh data
        if (globalNavMeshSurface != null && globalNavMeshSurface.navMeshData != null)
        {
            globalNavMeshSurface.RemoveData();
        }
    }

    public class TerrainChunk
    {
        public Bounds Bounds { get; private set; }
        public GameObject MeshObject;
        public MeshFilter MeshFilter;
        private MeshRenderer meshRenderer;
        private LODInfo[] lodInfoList;
        private LODMesh[] lodMeshes;
        private int2 chunkCoord;
        private int previousLODIndex = -1;
        private EndlessTerrain endlessTerrain; // Reference to the parent manager
        private bool hasNotifiedForNavMesh = false; // Flag to prevent spamming updates
        public Dictionary<int, List<float4x4>> ObjectData = new Dictionary<int, List<float4x4>>();
        private List<(GameObject, GameObject)> activeGameObjects = new();
        private Dictionary<int, Transform> objectParents = new();
        private Coroutine placementCoroutine; // NEW: Store coroutine handle for object placement
        private List<(JobHandle, NativeList<PlacementData>, int, GameObject)> placementJobs;
        private bool isDestroyed; // NEW: Flag to track chunk destruction
        private int[] neighborLODs = new int[4]; // 0=North, 1=South, 2=East, 3=West
    
        public int GetNeighborLOD(int direction)
        {
            return neighborLODs[direction];
        }


        public TerrainChunk(EndlessTerrain endlessTerrain, int2 coord)
        {
            this.endlessTerrain = endlessTerrain; // Store the reference
            chunkCoord = coord;
            lodInfoList = endlessTerrain.lodInfoList;
            int3 dimensions = endlessTerrain.ChunkDimensions;

            isDestroyed = false;
            placementJobs = new();

            Vector3 position = new(coord.x * dimensions.x, 0, coord.y * dimensions.z);
            Bounds = new Bounds(position, new Vector3(dimensions.x, dimensions.y, dimensions.z));

            MeshObject = ObjectPoolManager.Singleton.GetObject(endlessTerrain.pfTerrainChunk, position, Quaternion.identity);
            MeshObject.transform.SetParent(endlessTerrain.transform);

            MeshFilter = MeshObject.GetComponent<MeshFilter>();
            meshRenderer = MeshObject.GetComponent<MeshRenderer>();
            meshRenderer.material = endlessTerrain.terrainMaterial;

            lodMeshes = new LODMesh[lodInfoList.Length];
            for (int i = 0; i < lodMeshes.Length; i++)
            {
                lodMeshes[i] = new LODMesh(this, lodInfoList[i].lod);
            }

            // Set layer for physics, raycasting, etc.
            int groundLayerIndex = LayerMask.NameToLayer("Obstacle");
            MeshObject.layer = groundLayerIndex;

            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk()
        {
            float viewerDstFromNearestEdge = Mathf.Sqrt(Bounds.SqrDistance(endlessTerrain.viewerPosition));
            bool shouldBeVisible = viewerDstFromNearestEdge <= lodInfoList.Last().visibleDistanceThreshold;

            if (shouldBeVisible)
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

                UpdateNeighborLODs();

                if (lodIndex != previousLODIndex)
                {
                    LODMesh lodMesh = lodMeshes[lodIndex];
                    if (lodMesh.hasMesh)
                    {
                        previousLODIndex = lodIndex;

                        MeshFilter.mesh = lodMesh.mesh;

                        // If this is the highest detail LOD, queue its bounds for a NavMesh bake
                        if (lodMesh.lod == endlessTerrain.highestLOD && !hasNotifiedForNavMesh)
                        {
                            endlessTerrain.MarkNavMeshAsDirty(this.Bounds);
                            hasNotifiedForNavMesh = true; // Only notify once
                        }
                    }
                    else if (!lodMesh.isGenerating)
                    {
                        lodMesh.RequestMesh(endlessTerrain, chunkCoord, endlessTerrain.ChunkDimensions, neighborLODs, endlessTerrain.shouldPlaceObjects);
                    }
                }
            }
        }

        private void UpdateNeighborLODs()
        {
            // North neighbor (z+1)
            int2 northCoord = new int2(chunkCoord.x, chunkCoord.y + 1);
            neighborLODs[0] = endlessTerrain.TerrainChunkDict.TryGetValue(northCoord, out var northChunk) 
                ? northChunk.previousLODIndex : previousLODIndex;
            
            // South neighbor (z-1)
            int2 southCoord = new int2(chunkCoord.x, chunkCoord.y - 1);
            neighborLODs[1] = endlessTerrain.TerrainChunkDict.TryGetValue(southCoord, out var southChunk) 
                ? southChunk.previousLODIndex : previousLODIndex;
            
            // East neighbor (x+1)
            int2 eastCoord = new int2(chunkCoord.x + 1, chunkCoord.y);
            neighborLODs[2] = endlessTerrain.TerrainChunkDict.TryGetValue(eastCoord, out var eastChunk) 
                ? eastChunk.previousLODIndex : previousLODIndex;
            
            // West neighbor (x-1)
            int2 westCoord = new int2(chunkCoord.x - 1, chunkCoord.y);
            neighborLODs[3] = endlessTerrain.TerrainChunkDict.TryGetValue(westCoord, out var westChunk) 
                ? westChunk.previousLODIndex : previousLODIndex;
        }

        public void SchedulePlacementJobs(NativeArray<float> densityField, int lod)
        {
            if (isDestroyed) return;

            foreach (PlaceableObject placeable in endlessTerrain.activeBiomeSO.placeableObjects)
            {
                NativeArray<float3> octaveOffsets = endlessTerrain.octaveOffsetsPlaceableObjectsDict[placeable.prefab.GetHashCode()];
                NativeList<PlacementData> placementData = new(Allocator.Persistent);

                // Here is the fully populated job struct
                ObjectPlacementJob placementJob = new ObjectPlacementJob
                {
                    densityField = densityField,
                    chunkSize = endlessTerrain.ChunkDimensions,
                    chunkCoord = this.chunkCoord,
                    seed = (uint)(endlessTerrain.seed + this.chunkCoord.x * 100 + this.chunkCoord.y + placeable.prefab.GetHashCode()),
                    isoLevel = endlessTerrain.isoLevel,
                    lod = lod,

                    // --- Rules from the PlaceableObject ---
                    density = placeable.density,
                    placementNoise = placeable.placementNoise,
                    heightRange = placeable.heightRange, // Implicit Vector2 -> float2 conversion
                    slopeRange = placeable.slopeRange,   // Implicit Vector2 -> float2 conversion
                    scaleRange = placeable.scaleRange,   // Implicit Vector2 -> float2 conversion

                    randomYRotation = placeable.randomYRotation,
                    placeVertical = placeable.placeVertical,

                    octaveOffsets3D = octaveOffsets,
                    objectDataList = placementData
                };

                JobHandle handle = placementJob.Schedule();
                placementJobs.Add((handle, placementData, placeable.prefab.GetHashCode(), placeable.prefab));
            }
            placementCoroutine = endlessTerrain.StartCoroutine(WaitForPlacementJobs(placementJobs, densityField));
        }

        private IEnumerator WaitForPlacementJobs(List<(JobHandle, NativeList<PlacementData>, int, GameObject)> jobs, NativeArray<float> densityField)
        {
            foreach (var job in jobs)
            {

                JobHandle jobHandle = job.Item1;
                NativeList<PlacementData> placementData = job.Item2;
                int prefabHash = job.Item3;
                GameObject prefab = job.Item4;

                yield return new WaitUntil(() => jobHandle.IsCompleted);

                if (isDestroyed)
                {
                    jobHandle.Complete();
                    if (placementData.IsCreated) placementData.Dispose();
                    continue;
                }

                jobHandle.Complete();

                if (!objectParents.TryGetValue(prefabHash, out Transform parentTransform))
                {
                    GameObject parentObject = new GameObject(prefab.name + "s"); // e.g., "OakTrees"
                    parentTransform = parentObject.transform;
                    parentTransform.SetParent(MeshObject.transform, false); // Parent it to the chunk
                    objectParents[prefabHash] = parentTransform; // Add it to the dictionary for next time
                }
                int maxObjectsPerFrame = 25;
                int objectsPlacedThisFrame = 0;
                for (int i = 0; i < placementData.Length; i++)
                {
                    PlacementData data = placementData[i];
                    GameObject newObject = ObjectPoolManager.Singleton.GetObject(prefab, Vector3.zero, data.rotation); // Assuming your pooler can take a prefab reference
                    newObject.transform.SetParent(parentTransform, false);
                    newObject.transform.localPosition = data.position;
                    newObject.transform.localScale = Vector3.one * data.scale;
                    activeGameObjects.Add((newObject, prefab)); // Keep track of it

                    objectsPlacedThisFrame++;
                    if (objectsPlacedThisFrame >= maxObjectsPerFrame)
                    {
                        objectsPlacedThisFrame = 0;
                        yield return null;
                    }
                    Debug.Log("Still placing objects");
                }
                Debug.Log("Finished placing objects");

                if (placementData.IsCreated) placementData.Dispose();
            }
            if (densityField.IsCreated && !isDestroyed) densityField.Dispose();
            placementJobs.Clear();
            placementCoroutine = null;
        }

        public void HandleChunkRemoval()
        {
            if (isDestroyed) return;

            isDestroyed = true;

            // Stop placement coroutine if running
            if (placementCoroutine != null)
            {
                endlessTerrain.StopCoroutine(placementCoroutine);
                placementCoroutine = null;
            }

            // Clean up any pending placement jobs
            foreach (var job in placementJobs)
            {
                job.Item1.Complete(); // Ensure job is finished
                if (job.Item2.IsCreated) job.Item2.Dispose(); // Dispose matrices
            }
            placementJobs.Clear();

            // Clean up LOD meshes
            foreach (var lodMesh in lodMeshes)
            {
                lodMesh.CancelJob();
            }

            // Queue bounds for NavMesh update
            endlessTerrain.MarkNavMeshAsDirty(Bounds);

            if (MeshObject != null)
            {
                ObjectPoolManager.Singleton.ReturnObject(MeshObject, endlessTerrain.pfTerrainChunk);
            }
            foreach ((GameObject obj, GameObject pf) in activeGameObjects)
            {
                ObjectPoolManager.Singleton.ReturnObject(obj, pf);
            }
            foreach (var parentTransform in objectParents.Values)
            {
                Destroy(parentTransform.gameObject);
            }
            objectParents.Clear();
            activeGameObjects.Clear();
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
        private NativeArray<int> neighborLODsNative;

        public LODMesh(TerrainChunk parentChunk, int lod)
        {
            this.parentChunk = parentChunk;
            this.lod = lod;
        }

        public void RequestMesh(EndlessTerrain endlessTerrain, int2 chunkCoord, int3 chunkDimensions, int[] neighborLODs, bool shouldPlaceObjects)
        {
            isGenerating = true;
            Request3DMesh(endlessTerrain, chunkCoord, chunkDimensions, neighborLODs, shouldPlaceObjects);
        }

        private void Request3DMesh(EndlessTerrain endlessTerrain, int2 chunkCoord, int3 chunkDimensions, int[] neighborLODs, bool shouldPlaceObjects)
        {
            vertices = new NativeList<float3>(Allocator.Persistent);
            triangles = new NativeList<int>(Allocator.Persistent);

            int step = 1 << lod;
            //int3 numPointsPerAxis = chunkDimensions / step + 1;

            int3 extendedPoints = (chunkDimensions / step + 2); // +2 instead of +1
            densityField = new NativeArray<float>(extendedPoints.x * extendedPoints.y * extendedPoints.z, Allocator.Persistent);
            cubeDensities = new NativeArray<float>(8, Allocator.Persistent);
            edgeVertices = new NativeArray<float3>(12, Allocator.Persistent);
            neighborLODsNative = new NativeArray<int>(neighborLODs, Allocator.Persistent);

            var job = new ThreeDJob
            {
                chunkCoord = chunkCoord,
                chunkSize = chunkDimensions,
                isoLevel = endlessTerrain.isoLevel,
                caveStrength = endlessTerrain.activeBiomeSO.caveStrength,
                lod = lod,
                neighborLODs = neighborLODsNative, // NEW
                vertices = vertices,
                triangles = triangles,
                densityField = densityField,
                cubeDensities = cubeDensities,
                edgeVertices = edgeVertices,
                terrainAmplitudeFactor = endlessTerrain.activeBiomeSO.terrainAmplitudeFactor,
                continentalnessCurveSamples = endlessTerrain.continentalnessSamples,
                erosionCurveSamples = endlessTerrain.erosionSamples,
                peaksAndValleysCurveSamples = endlessTerrain.peaksAndValleysSamples,
                continentalnessNoise = endlessTerrain.activeBiomeSO.continentalnessNoise,
                erosionNoise = endlessTerrain.activeBiomeSO.erosionNoise,
                peaksAndValleysNoise = endlessTerrain.activeBiomeSO.peaksAndValleysNoise,
                threeDNoiseSettings = endlessTerrain.activeBiomeSO.threeDNoise,
                cavernNoiseSettings = endlessTerrain.activeBiomeSO.cavernNoise,
                warpNoiseSettings = endlessTerrain.activeBiomeSO.warpNoise,
                heightBiasForCaves = endlessTerrain.activeBiomeSO.heightBiasForCaves,
                verticalGradientCurveSamples = endlessTerrain.verticalGradientSamples,
                worleyVerticalGradientSamples = endlessTerrain.worleyVerticalGradientSamples,
                octaveOffsetsContinentalness = endlessTerrain.octaveOffsetsContinentalness,
                octaveOffsetsErosion = endlessTerrain.octaveOffsetsErosion,
                octaveOffsetsPeaksAndValleys = endlessTerrain.octaveOffsetsPeaksAndValleys,
                octaveOffsets3D = endlessTerrain.octaveOffsets3D,
                octaveOffsetsWarp = endlessTerrain.octaveOffsetsWarp,
                continentalnessNoiseFunctions = endlessTerrain.continentalnessNoiseFunctions,
                erosionNoiseFunctions = endlessTerrain.erosionNoiseFunctions,
                peaksAndValleysNoiseFunctions = endlessTerrain.peaksAndValleysNoiseFunctions
            };

            jobHandle = job.Schedule();
            endlessTerrain.StartCoroutine(WaitForJobCompletion(shouldPlaceObjects));
        }

        private IEnumerator WaitForJobCompletion(bool shouldPlaceObjects)
        {
            while (!jobHandle.IsCompleted)
            {
                yield return null;
            }

            try
            {
                jobHandle.Complete();
                if (!isGenerating)
                {
                    if (densityField.IsCreated) densityField.Dispose();
                    // need to dispose of this here, because if we put it in the finally block, it would be destroyed before the object placement job can use it
                    yield break;
                }
                CreateMesh();

                if (hasMesh && shouldPlaceObjects)
                {
                    parentChunk.SchedulePlacementJobs(densityField, lod);
                }
                else
                {
                    if (densityField.IsCreated) densityField.Dispose();
                }
            }
            finally
            {
                if (vertices.IsCreated) vertices.Dispose();
                if (triangles.IsCreated) triangles.Dispose();
                if (cubeDensities.IsCreated) cubeDensities.Dispose();
                if (edgeVertices.IsCreated) edgeVertices.Dispose();
                if (neighborLODsNative.IsCreated) neighborLODsNative.Dispose();
                isGenerating = false;
            }
        }

        private void CreateMesh()
        {
            if (vertices.Length == 0)
            {
                // If the job produced no vertices, create a blank mesh.
                // This prevents the chunk from trying to generate it again.
                mesh = new Mesh(); 
                hasMesh = true;
                parentChunk.UpdateTerrainChunk();
                return;
            }

            if (mesh == null)
            {
                mesh = new Mesh();
            }

            var meshDataArray = Mesh.AllocateWritableMeshData(1);
            var meshData = meshDataArray[0];

            meshData.SetVertexBufferParams(vertices.Length, 
                new UnityEngine.Rendering.VertexAttributeDescriptor(UnityEngine.Rendering.VertexAttribute.Position, stream: 0));

            UnityEngine.Rendering.IndexFormat indexFormat = vertices.Length > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            meshData.SetIndexBufferParams(triangles.Length, indexFormat);

            var vertexData = meshData.GetVertexData<float3>(stream: 0);
            vertices.AsArray().CopyTo(vertexData);

            if (indexFormat == UnityEngine.Rendering.IndexFormat.UInt16)
            {
                var triangleData = meshData.GetIndexData<ushort>();
                for (int i = 0; i < triangles.Length; i++)
                {
                    triangleData[i] = (ushort)triangles[i];
                }
            }
            else
            {
                var triangleData = meshData.GetIndexData<int>();
                triangles.AsArray().CopyTo(triangleData);
            }

            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new UnityEngine.Rendering.SubMeshDescriptor(0, triangles.Length));

            // We pass a flag to tell the engine not to worry about the bounds yet.
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh, UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds);
            
            // ✅ ADD THIS BACK: This is the critical fix.
            // It calculates the bounding box so the rendering engine knows how big the mesh is.
            mesh.RecalculateBounds(); 
            
            mesh.RecalculateNormals();
            
            hasMesh = true;

            MeshCollider meshCollider = parentChunk.MeshObject.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                meshCollider = parentChunk.MeshObject.AddComponent<MeshCollider>();
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
                if (cubeDensities.IsCreated) cubeDensities.Dispose();
                if (edgeVertices.IsCreated) edgeVertices.Dispose();
                if (densityField.IsCreated) densityField.Dispose();
                if (neighborLODsNative.IsCreated) neighborLODsNative.Dispose(); 

            }
        }
    }

    [Serializable]
    public struct LODInfo
    {
        public int lod;
        public int visibleDistanceThreshold;
    }
}