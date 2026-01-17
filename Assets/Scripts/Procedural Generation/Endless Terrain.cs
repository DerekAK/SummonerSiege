using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System;
using System.Linq;
using UnityEngine.AI;

public class EndlessTerrain : MonoBehaviour
{
    [Header("Core Settings")]
    [SerializeField] private float isoLevel = 0f;
    private Vector3 viewerPosition;
    public int3 ChunkDimensions;
    [SerializeField] private int seed = 1;
    [SerializeField] private bool shouldPlaceObjects;
    [SerializeField] private LODInfo[] lodInfoList;
    private int highestLOD;

    [SerializeField] private Transform viewer;
    [SerializeField] private Material terrainMaterial;
    [SerializeField] private BiomeSO activeBiomeSO;
    [SerializeField] GameObject pfTerrainChunk;
    private List<int2> previouslyVisibleChunks = new();

    public Dictionary<int2, TerrainChunk> TerrainChunkDict = new Dictionary<int2, TerrainChunk>();

    [Header("Global NavMesh Settings")]
    [SerializeField] private bool shouldBakeNavMesh = true;
    [SerializeField] private int navMeshAgentTypeID = 0; // 0 = Humanoid
    [SerializeField] private float navMeshUpdateInterval;
    [SerializeField] private int navMeshDefaultArea = 0;
    [SerializeField] private LayerMask navMeshLayerMask;
    
    // Global NavMesh data
    private NavMeshData globalNavMeshData;
    private NavMeshDataInstance globalNavMeshInstance;
    private Dictionary<int2, List<NavMeshBuildSource>> chunkNavMeshSources = new Dictionary<int2, List<NavMeshBuildSource>>();
    private bool navMeshNeedsUpdate = false;
    private AsyncOperation activeNavMeshBuild;
    private Coroutine navMeshUpdateCoroutine;

    // Native Arrays for noise curves
    private NativeArray<float> continentalnessSamples;
    private NativeArray<float> erosionSamples;
    private NativeArray<float> peaksAndValleysSamples;
    private NativeArray<float> verticalGradientSamples;
    private NativeArray<float> worleyVerticalGradientSamples;
    private NativeArray<float2> octaveOffsetsContinentalness;
    private NativeArray<float2> octaveOffsetsErosion;
    private NativeArray<float2> octaveOffsetsPeaksAndValleys;
    private NativeArray<float3> octaveOffsets3D;
    private NativeArray<float3> octaveOffsetsWarp;
    private NativeArray<NoiseFunction> continentalnessNoiseFunctions;
    private NativeArray<NoiseFunction> erosionNoiseFunctions;
    private NativeArray<NoiseFunction> peaksAndValleysNoiseFunctions;
    private Dictionary<int, NativeArray<float3>> octaveOffsetsPlaceableObjectsDict = new();

    private float lastUpdateTime;
    private const float MIN_UPDATE_INTERVAL = 0.1f;

    [Header("Navigation Settings")]
    [SerializeField] private AgentNavigationProfileSO[] agentProfiles;

    public void SetViewerTransform(Transform viewerTransform)
    {
        viewer = viewerTransform;
    }

    private void Start()
    {
        int numChunks = InitializeChunkPool();
        InitializePlacementPools(numChunks);
        viewerPosition = viewer ? viewer.position : Vector3.zero;
        GenerateNewBiomeArrays();
        highestLOD = lodInfoList[0].lod;
        lastUpdateTime = Time.time;
        
        // Initialize global NavMesh
        if (shouldBakeNavMesh)
        {
            InitializeGlobalNavMesh();
            navMeshUpdateCoroutine = StartCoroutine(NavMeshUpdateCoroutine());
        }
        
        UpdateVisibleChunks();
    }

    private void Update()
    {
        if (!viewer) return;
        
        float timeSinceLastUpdate = Time.time - lastUpdateTime;
        
        if (Vector3.Distance(viewerPosition, viewer.position) > 0.01f && 
            timeSinceLastUpdate >= MIN_UPDATE_INTERVAL)
        {
            UpdateVisibleChunks();
            lastUpdateTime = Time.time;
        }
        
        viewerPosition = viewer.position;
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

        for (int xOffset = -maxChunksFromViewer; xOffset <= maxChunksFromViewer; xOffset++)
        {
            for (int zOffset = -maxChunksFromViewer; zOffset <= maxChunksFromViewer; zOffset++)
            {
                int2 chunkCoord = new int2(viewerCoordX + xOffset, viewerCoordZ + zOffset);
                Vector3 position = new(chunkCoord.x * ChunkDimensions.x, 0, chunkCoord.y * ChunkDimensions.z);
                Vector3 boundsPosition = new(
                    position.x + ChunkDimensions.x / 2f,
                    ChunkDimensions.y / 2f,
                    position.z + ChunkDimensions.z / 2f
                );
                Bounds tempBounds = new(boundsPosition, new Vector3(ChunkDimensions.x, ChunkDimensions.y, ChunkDimensions.z));
                float viewerDstFromChunk = Mathf.Sqrt(tempBounds.SqrDistance(viewerPosition));

                if (viewerDstFromChunk <= maxViewDist)
                {
                    previouslyVisibleChunks.Remove(chunkCoord);
                    if (TerrainChunkDict.ContainsKey(chunkCoord))
                    {
                        TerrainChunkDict[chunkCoord].UpdateTerrainChunk();
                    }
                    else
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
        Physics.SyncTransforms();
    }

    #region Global NavMesh Management

    private void InitializeGlobalNavMesh()
    {
        globalNavMeshData = new NavMeshData(navMeshAgentTypeID);
        globalNavMeshInstance = NavMesh.AddNavMeshData(globalNavMeshData);
    }

    /// <summary>
    /// Register a chunk's NavMesh sources with the global system
    /// </summary>
    public void RegisterChunkNavMeshSources(int2 chunkCoord, List<NavMeshBuildSource> sources)
    {
        if (!shouldBakeNavMesh) return;

        chunkNavMeshSources[chunkCoord] = sources;
        navMeshNeedsUpdate = true;
    }

    /// <summary>
    /// Unregister a chunk's NavMesh sources when it despawns
    /// </summary>
    public void UnregisterChunkNavMeshSources(int2 chunkCoord)
    {
        if (chunkNavMeshSources.ContainsKey(chunkCoord))
        {
            chunkNavMeshSources.Remove(chunkCoord);
            navMeshNeedsUpdate = true;
        }
    }

    /// <summary>
    /// Coroutine that periodically updates the global NavMesh when dirty
    /// </summary>
    private IEnumerator NavMeshUpdateCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(navMeshUpdateInterval);

            if (navMeshNeedsUpdate && activeNavMeshBuild == null)
            {
                UpdateGlobalNavMesh();
            }
        }
    }

    /// <summary>
    /// Trigger an async update of the global NavMesh
    /// </summary>
    private void UpdateGlobalNavMesh()
    {
        if (activeNavMeshBuild != null && !activeNavMeshBuild.isDone)
        {
            return;
        }

        // Build aggregated source list
        List<NavMeshBuildSource> allSources = BuildAggregatedSourcesList();
        
        if (allSources.Count == 0)
        {
            navMeshNeedsUpdate = false;
            return;
        }

        // Calculate bounds covering all active chunks
        Bounds aggregateBounds = CalculateAggregateBounds();

        // Get build settings
        NavMeshBuildSettings buildSettings = NavMesh.GetSettingsByID(navMeshAgentTypeID);
        // PERFORMANCE OPTIMIZATIONS
        buildSettings.overrideVoxelSize = true;
        buildSettings.voxelSize = 0.6f; // Larger = faster (was ~0.16 by default)
        buildSettings.minRegionArea = 2f; // Ignore tiny NavMesh islands (faster)
        buildSettings.overrideTileSize = true;
        buildSettings.tileSize = 512; // Larger tiles = fewer tiles = faster
        buildSettings.buildHeightMesh = false;

        // Start async update
        activeNavMeshBuild = NavMeshBuilder.UpdateNavMeshDataAsync(
            globalNavMeshData,
            buildSettings,
            allSources,
            aggregateBounds
        );

        activeNavMeshBuild.completed += OnNavMeshUpdateCompleted;
        navMeshNeedsUpdate = false;
    }

    private void OnNavMeshUpdateCompleted(AsyncOperation operation)
    {
        activeNavMeshBuild = null;
    }

    /// <summary>
    /// Build a single list of all NavMesh sources from all chunks, in consistent order
    /// </summary>
    private List<NavMeshBuildSource> BuildAggregatedSourcesList()
    {
        List<NavMeshBuildSource> allSources = new List<NavMeshBuildSource>();

        // Sort chunks by coordinate to ensure consistent ordering
        var sortedChunkCoords = chunkNavMeshSources.Keys.OrderBy(c => c.x).ThenBy(c => c.y).ToList();

        foreach (int2 coord in sortedChunkCoords)
        {
            allSources.AddRange(chunkNavMeshSources[coord]);
        }

        return allSources;
    }

    /// <summary>
    /// Calculate bounds that encompass all active chunks
    /// </summary>
    private Bounds CalculateAggregateBounds()
    {
        if (chunkNavMeshSources.Count == 0)
        {
            return new Bounds(Vector3.zero, Vector3.one);
        }

        // Find min/max chunk coordinates
        int minX = int.MaxValue, maxX = int.MinValue;
        int minZ = int.MaxValue, maxZ = int.MinValue;

        foreach (int2 coord in chunkNavMeshSources.Keys)
        {
            minX = Mathf.Min(minX, coord.x);
            maxX = Mathf.Max(maxX, coord.x);
            minZ = Mathf.Min(minZ, coord.y);
            maxZ = Mathf.Max(maxZ, coord.y);
        }

        // Calculate world-space bounds
        Vector3 minCorner = new Vector3(
            minX * ChunkDimensions.x,
            0,
            minZ * ChunkDimensions.z
        );

        Vector3 maxCorner = new Vector3(
            (maxX + 1) * ChunkDimensions.x,
            ChunkDimensions.y,
            (maxZ + 1) * ChunkDimensions.z
        );

        Vector3 center = (minCorner + maxCorner) / 2f;
        Vector3 size = maxCorner - minCorner;

        return new Bounds(center, size);
    }

    #endregion

    private NativeArray<float> BakeCurve(AnimationCurve curve, int resolution)
    {
        var samples = new NativeArray<float>(resolution, Allocator.Persistent);
        for (int i = 0; i < resolution; i++)
        {
            float time = (float)i / (resolution - 1);
            samples[i] = curve.Evaluate(time);
        }
        return samples;
    }

    private int InitializeChunkPool()
    {
        int chunkBuffer = 0;
        float maxDistancePerSide = lodInfoList[lodInfoList.Length - 1].visibleDistanceThreshold;
        int maxChunksX = (int)Mathf.Ceil(maxDistancePerSide / ChunkDimensions.x) * 2 + 1;
        int maxChunksZ = (int)Mathf.Ceil(maxDistancePerSide / ChunkDimensions.x) * 2 + 1;

        int numChunks = maxChunksX * maxChunksZ + chunkBuffer;

        SimpleObjectPool.Singleton.CreatePool(pfTerrainChunk, numChunks, numChunks);

        return numChunks;
    }

    private void InitializePlacementPools(int numChunks)
    {
        int initialAmountPerPf = 1000;
        foreach (PlaceableObject placeable in activeBiomeSO.placeableObjects)
        {
            SimpleObjectPool.Singleton.CreatePool(placeable.prefab, initialAmountPerPf, initialAmountPerPf);
        }
    }

    private void GenerateNewBiomeArrays()
    {
        int curveResolution = 256;
        continentalnessSamples = BakeCurve(activeBiomeSO.continentalnessCurve, curveResolution);
        erosionSamples = BakeCurve(activeBiomeSO.erosionCurve, curveResolution);
        peaksAndValleysSamples = BakeCurve(activeBiomeSO.peaksAndValleysCurve, curveResolution);
        verticalGradientSamples = BakeCurve(activeBiomeSO.verticalGradientCurve, curveResolution);
        worleyVerticalGradientSamples = BakeCurve(activeBiomeSO.worleyVerticalGradientCurve, curveResolution);

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

    public TerrainChunk GetChunk(int2 coords)
    {
        return TerrainChunkDict[coords];
    }

    private void OnDestroy()
    {
        // Stop NavMesh update routine
        if (navMeshUpdateCoroutine != null)
        {
            StopCoroutine(navMeshUpdateCoroutine);
        }

        // Clean up global NavMesh
        if (globalNavMeshInstance.valid)
        {
            NavMesh.RemoveNavMeshData(globalNavMeshInstance);
        }

        // Dispose native arrays
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
        public int2 ChunkCoord => chunkCoord;
        public int previousLOD = -1;
        private EndlessTerrain endlessTerrain;
        public bool IsDestroyed;
        private NativeCollectionManager nativeManager;
       
        // Placement fields
        private List<(GameObject, PlaceableObject)> activePlaceableObjects = new();
        public List<(GameObject, PlaceableObject)> ActivePlaceableObjects => activePlaceableObjects;

        private Dictionary<int, Transform> objectParents = new();
        private Coroutine placementCoroutine;
        private List<(JobHandle, NativeList<PlacementData>, int, GameObject)> placementJobs;
        private bool areObjectsPlaced = false;

        public TerrainChunk(EndlessTerrain endlessTerrain, int2 coord)
        {
            nativeManager = new NativeCollectionManager();
            this.endlessTerrain = endlessTerrain;
            chunkCoord = coord;
            lodInfoList = endlessTerrain.lodInfoList;
            int3 dimensions = endlessTerrain.ChunkDimensions;

            IsDestroyed = false;
            placementJobs = new();

            Vector3 position = new(coord.x * dimensions.x, 0, coord.y * dimensions.z);
            Vector3 boundsPosition = new(
                position.x + dimensions.x / 2f,  
                dimensions.y / 2f,                
                position.z + dimensions.z / 2f
            );
            Bounds = new Bounds(boundsPosition, new Vector3(dimensions.x, dimensions.y, dimensions.z));

            MeshObject = SimpleObjectPool.Singleton.GetObject(endlessTerrain.pfTerrainChunk, position, Quaternion.identity);
            MeshObject.transform.SetParent(endlessTerrain.transform);

            MeshFilter = MeshObject.GetComponent<MeshFilter>();
            meshRenderer = MeshObject.GetComponent<MeshRenderer>();
            meshRenderer.material = endlessTerrain.terrainMaterial;

            lodMeshes = new LODMesh[lodInfoList.Length];
            for (int i = 0; i < lodMeshes.Length; i++)
            {
                lodMeshes[i] = new LODMesh(this, lodInfoList[i].lod);
            }

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

                if (lodMeshes[lodIndex].lod != previousLOD)
                {
                    LODMesh lodMesh = lodMeshes[lodIndex];
                    if (lodMesh.hasMesh)
                    {
                        int oldLOD = previousLOD;
                        int newLOD = lodMesh.lod;
                        
                        MeshFilter.mesh = lodMesh.mesh;
                        previousLOD = newLOD;


                        // Only register NavMesh sources for highest LOD chunks
                        if (endlessTerrain.shouldBakeNavMesh)
                        {
                            bool enteringHighestLOD = (newLOD == endlessTerrain.highestLOD) && 
                                                    (oldLOD != endlessTerrain.highestLOD);
                            bool leavingHighestLOD = (newLOD != endlessTerrain.highestLOD) && 
                                                    (oldLOD == endlessTerrain.highestLOD);


                            if (enteringHighestLOD)
                            {
                                RegisterNavMeshSources();
                            }
                            else if (leavingHighestLOD)
                            {
                                endlessTerrain.UnregisterChunkNavMeshSources(chunkCoord);
                            }

                        }
                    }
                    else if (!lodMesh.isGenerating)
                    {
                        areObjectsPlaced = false;
                        lodMesh.RequestMesh(endlessTerrain, chunkCoord, endlessTerrain.ChunkDimensions, endlessTerrain.shouldPlaceObjects);
                    }
                }
            }
        }

        /// <summary>
        /// Collect NavMesh build sources for this chunk and register with EndlessTerrain
        /// </summary>
        private void RegisterNavMeshSources()
        {
            List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
            List<NavMeshBuildMarkup> markups = new List<NavMeshBuildMarkup>();


            // Collect sources from this chunk's mesh and colliders
            NavMeshBuilder.CollectSources(
                Bounds,
                endlessTerrain.navMeshLayerMask,
                NavMeshCollectGeometry.RenderMeshes,
                endlessTerrain.navMeshDefaultArea,
                markups,
                sources
            );

            // Register with EndlessTerrain
            endlessTerrain.RegisterChunkNavMeshSources(chunkCoord, sources);
        }

        public void SchedulePlacementJobs(NativeArray<float> densityField, int lod)
        {
            if (IsDestroyed) return;

            foreach (PlaceableObject placeable in endlessTerrain.activeBiomeSO.placeableObjects)
            {
                NativeArray<float3> octaveOffsets = endlessTerrain.octaveOffsetsPlaceableObjectsDict[placeable.prefab.GetHashCode()];
                
                NativeList<PlacementData> placementData = nativeManager.CreateTrackedList<PlacementData>(Allocator.Persistent);

                ObjectPlacementJob placementJob = new ObjectPlacementJob
                {
                    densityField = densityField,
                    chunkSize = endlessTerrain.ChunkDimensions,
                    chunkCoord = this.chunkCoord,
                    seed = (uint)(endlessTerrain.seed + this.chunkCoord.x * 100 + this.chunkCoord.y + placeable.prefab.GetHashCode()),
                    isoLevel = endlessTerrain.isoLevel,
                    lod = lod,
                    density = placeable.density,
                    placementNoise = placeable.placementNoise,
                    heightRange = placeable.heightRange,
                    slopeRange = placeable.slopeRange,
                    scaleRange = placeable.scaleRange,
                    yOffsetRange = placeable.yOffsetRange,
                    randomYRotation = placeable.randomYRotation,
                    placeVertical = placeable.placeVertical,
                    octaveOffsets3D = octaveOffsets,
                    objectDataList = placementData
                };

                JobHandle handle = nativeManager.TrackJob(placementJob.Schedule());
                placementJobs.Add((handle, placementData, placeable.prefab.GetHashCode(), placeable.prefab));
            }
            placementCoroutine = endlessTerrain.StartCoroutine(WaitForPlacementJobs(placementJobs, densityField));
        }

        private IEnumerator WaitForPlacementJobs(List<(JobHandle, NativeList<PlacementData>, int, GameObject)> jobs, NativeArray<float> densityField)
        {
            var jobsCopy = new List<(JobHandle, NativeList<PlacementData>, int, GameObject)>(jobs);

            foreach (var job in jobsCopy)
            {
                JobHandle jobHandle = job.Item1;
                NativeList<PlacementData> placementData = job.Item2;
                int prefabHash = job.Item3;
                GameObject prefab = job.Item4;

                if (IsDestroyed)
                {
                    yield break;
                }

                PlaceableObject placeableConfig = endlessTerrain.activeBiomeSO.placeableObjects.FirstOrDefault(p => p.prefab == prefab);

                while (!jobHandle.IsCompleted)
                {
                    yield return null;
                    
                    if (IsDestroyed)
                    {
                        yield break;
                    }
                }

                jobHandle.Complete();

                if (IsDestroyed)
                {
                    yield break;
                }

                if (!nativeManager.TryAccess(placementData, out int dataLength) || dataLength == 0)
                {
                    continue;
                }

                if (!objectParents.TryGetValue(prefabHash, out Transform parentTransform))
                {
                    if (MeshObject == null || IsDestroyed)
                    {
                        yield break;
                    }
                    
                    GameObject parentObject = new GameObject(prefab.name + "s");
                    parentTransform = parentObject.transform;
                    parentTransform.SetParent(MeshObject.transform, false);
                    objectParents[prefabHash] = parentTransform;
                }

                int maxObjectsPerFrame = 25;
                int objectsPlacedThisFrame = 0;

                for (int i = 0; i < placementData.Length; i++)
                {
                    if (IsDestroyed)
                    {
                        yield break;
                    }

                    if (!nativeManager.TryGetElement(placementData, i, out PlacementData data))
                    {
                        yield break;
                    }
                    GameObject newObject = SimpleObjectPool.Singleton.GetObject(prefab, Vector3.zero, data.rotation);
                    newObject.transform.SetParent(parentTransform, false);
                    newObject.transform.localPosition = data.position;
                    newObject.transform.localScale = Vector3.one * data.scale;
                    activePlaceableObjects.Add((newObject, placeableConfig));

                    if (placeableConfig != null && placeableConfig.isNavMeshObstacle)
                    {
                        NavMeshObstacle obstacle = newObject.GetComponent<NavMeshObstacle>();
                        if (obstacle == null)
                        {
                            obstacle = newObject.AddComponent<NavMeshObstacle>();
                        }

                        obstacle.carving = placeableConfig.carveNavMesh;

                        if (newObject.TryGetComponent<Collider>(out var collider))
                        {
                            obstacle.shape = NavMeshObstacleShape.Box;
                            obstacle.size = Vector3.Scale(collider.bounds.size, newObject.transform.localScale);
                        }
                    }

                    objectsPlacedThisFrame++;
                    if (objectsPlacedThisFrame >= maxObjectsPerFrame)
                    {
                        objectsPlacedThisFrame = 0;
                        yield return null;

                        if (IsDestroyed)
                        {
                            yield break;
                        }
                    }
                }
                nativeManager.DisposeEarly(placementData);
            }
            
            if (densityField.IsCreated)
            {
                nativeManager.DisposeEarly(densityField);
            }
            
            placementJobs.Clear();
            placementCoroutine = null;

            areObjectsPlaced = true;
        }

        public void HandleChunkRemoval()
        {
            if (IsDestroyed) return;
            IsDestroyed = true;

            // Unregister NavMesh sources
            endlessTerrain.UnregisterChunkNavMeshSources(chunkCoord);

            nativeManager?.Dispose();

            areObjectsPlaced = false;

            if (placementCoroutine != null)
            {
                endlessTerrain.StopCoroutine(placementCoroutine);
                placementCoroutine = null;
            }

            placementJobs.Clear();

            foreach (var lodMesh in lodMeshes)
            {
                lodMesh.CancelJob();
            }

            if (MeshObject != null)
            {
                SimpleObjectPool.Singleton.ReturnObject(MeshObject, endlessTerrain.pfTerrainChunk);
            }
            
            foreach ((GameObject obj, PlaceableObject config) in activePlaceableObjects)
            {
                if (obj != null)
                {
                    SimpleObjectPool.Singleton.ReturnObject(obj, config.prefab);
                }
            }
            
            foreach (var parentTransform in objectParents.Values)
            {
                if (parentTransform != null && parentTransform.gameObject != null)
                {
                    UnityEngine.Object.Destroy(parentTransform.gameObject);
                }
            }
            
            objectParents.Clear();
            activePlaceableObjects.Clear();
        }
    }

    class LODMesh
    {
        private NativeCollectionManager nativeManager;
        private TerrainChunk parentChunk;
        public Mesh mesh;
        public bool hasMesh;
        public bool isGenerating;
        public int lod;
        private JobHandle jobHandle;

        public LODMesh(TerrainChunk parentChunk, int lod)
        {
            this.nativeManager = new NativeCollectionManager();
            this.parentChunk = parentChunk;
            this.lod = lod;
        }

        public void RequestMesh(EndlessTerrain endlessTerrain, int2 chunkCoord, int3 chunkDimensions, bool shouldPlaceObjects)
        {
            isGenerating = true;
            Request3DMesh(endlessTerrain, chunkCoord, chunkDimensions, shouldPlaceObjects);
        }

        private void Request3DMesh(EndlessTerrain endlessTerrain, int2 chunkCoord, int3 chunkDimensions, bool shouldPlaceObjects)
        {
            var vertices = nativeManager.CreateTrackedList<float3>(Allocator.Persistent);
            var triangles = nativeManager.CreateTrackedList<int>(Allocator.Persistent);

            int step = 1 << lod;
            int3 extendedPoints = chunkDimensions / step + 2;
            
            var densityField = nativeManager.CreateTracked<float>(extendedPoints.x * extendedPoints.y * extendedPoints.z, Allocator.Persistent);
            var cubeDensities = nativeManager.CreateTracked<float>(8, Allocator.Persistent);
            var edgeVertices = nativeManager.CreateTracked<float3>(12, Allocator.Persistent);

            var job = new ThreeDJob
            {
                chunkCoord = chunkCoord,
                chunkSize = chunkDimensions,
                isoLevel = endlessTerrain.isoLevel,
                caveStrength = endlessTerrain.activeBiomeSO.caveStrength,
                lod = lod,
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

            jobHandle = nativeManager.TrackJob(job.Schedule());
            endlessTerrain.StartCoroutine(WaitForJobCompletion(shouldPlaceObjects, vertices, triangles, densityField));
        }

        private IEnumerator WaitForJobCompletion(bool shouldPlaceObjects, NativeList<float3> vertices, NativeList<int> triangles, NativeArray<float> densityField)
        {
            while (!jobHandle.IsCompleted)
            {
                yield return null;
                if (!isGenerating)
                {
                    yield break;
                }
            }

            jobHandle.Complete();
            
            if (!isGenerating) yield break;
            
            CreateMesh(vertices, triangles);

            if (hasMesh && shouldPlaceObjects && isGenerating)
            {
                parentChunk.SchedulePlacementJobs(densityField, lod);
            }
            else
            {
                nativeManager.DisposeEarly(densityField);
            }
            
            isGenerating = false;
        }

        private void CreateMesh(NativeList<float3> vertices, NativeList<int> triangles)
        {
            if (!nativeManager.TryAccess(vertices, out int vertCount) || vertCount == 0)
            {
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

            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh, UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds);
            
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            
            MeshCollider meshCollider = parentChunk.MeshObject.GetComponent<MeshCollider>();
            
            meshCollider.sharedMesh = mesh;
            
            hasMesh = true;

            parentChunk.UpdateTerrainChunk();
        }

        public void CancelJob()
        {
            if (isGenerating)
            {
                isGenerating = false;
                nativeManager?.Dispose();
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