using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System;
using System.Linq;

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
            // Evaluate the curve at normalized time [0, 1]
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

            mapGenerator = mapGen;

            lodMeshes = new LODMesh[lodInfoList.Length];
            for (int i = 0; i < lodMeshes.Length; i++)
            {
                lodMeshes[i] = new LODMesh(lodInfoList[i].lod);
            }

            UpdateTerrainChunk(); // Initial update to determine visibility and start generation
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
                        lodIndex = i + 1;
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

                        // --- ADD THIS LOGIC ---
                        // Add or get a MeshCollider component
                        MeshCollider meshCollider = meshObject.GetComponent<MeshCollider>();
                        if (meshCollider == null)
                        {
                            meshCollider = meshObject.AddComponent<MeshCollider>();
                        }
                        // Update the collider's mesh to match the visual mesh
                        meshCollider.sharedMesh = lodMesh.mesh;
                        // --- END OF ADDED LOGIC ---


                    }
                    else if (!lodMesh.isGenerating)
                    {
                        lodMesh.RequestMesh(mapGenerator, chunkCoord, Bounds.size);
                    }
                }
            }

            SetVisible(visible);
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public void DestroyChunk()
        {
            foreach (var lodMesh in lodMeshes)
            {
                lodMesh.CancelJob();
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

        // Native Arrays for 3D generation
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

            // --- THIS IS THE FIX ---
            // Calculate the step and required grid size here, BEFORE allocating memory.
            int step = 1 << this.lod;
            int3 numPointsPerAxis = dimensions / step + 1;

            // Allocate the densityField with the CORRECT size for the current LOD.
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

                // biome settings
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
                jobHandle.Complete(); // This will re-throw any exception from the job.

                if (!isGenerating){ yield break; }

                CreateMesh();
            }
            finally
            {
                // This block is GUARANTEED to execute, regardless of exceptions.
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
            // Check if there's a job we need to handle.
            if (isGenerating)
            {
                try
                {
                    // We must wait for the job to finish before we can dispose its data.
                    jobHandle.Complete();
                }
                finally
                {
                    // This guarantees cleanup even if the job had an error.
                    isGenerating = false;
                    DisposeArrays();
                }
            }
            // If not generating, there's no active job or allocated memory to worry about.
        }

        // destroys arrays that only need to exist per job
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