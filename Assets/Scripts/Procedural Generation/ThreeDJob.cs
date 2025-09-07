using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using System.Runtime.CompilerServices;

[BurstCompile]
public struct ThreeDJob : IJob
{
    // --- Input Data ---
    public int2 chunkCoord;
    public int3 chunkSize;
    public float isoLevel;
    public int lod;
    [ReadOnly] public float2 noiseOffset;

    // --- Reusable Input/Output Arrays ---
    public NativeArray<float> cubeDensities;
    public NativeArray<float3> edgeVertices;
    public NativeArray<float> densityField;

    // --- Output Lists ---
    public NativeList<float3> vertices;
    public NativeList<int> triangles;

    // --- Biome Data ---  
    public float terrainAmplitude;
    [ReadOnly] public NativeArray<float> continentalnessCurveSamples;
    [ReadOnly] public NativeArray<float> erosionCurveSamples;
    [ReadOnly] public NativeArray<float> peaksAndValleysCurveSamples;
    public NoiseSettings continentalnessNoise;
    public NoiseSettings erosionNoise;
    public NoiseSettings peaksAndValleysNoise;
    public NoiseSettings threeDNoiseSettings;
    [ReadOnly] public NativeArray<float> verticalGradientCurveSamples;
    [ReadOnly] public NativeArray<float2> octaveOffsetsContinentalness;
    [ReadOnly] public NativeArray<float2> octaveOffsetsErosion;
    [ReadOnly] public NativeArray<float2> octaveOffsetsPeaksAndValleys;
    [ReadOnly] public NativeArray<float3> octaveOffsets3D;

    public void Execute()
    {
        // NEW: Calculate step size based on LOD. 0=1, 1=2, 2=4, etc.
        int step = 1 << lod;

        int3 numPointsPerAxis = chunkSize / step + 1;

        // Step 1: Calculate the density field at the correct LOD
        for (int x = 0; x < numPointsPerAxis.x; x++)
        {
            for (int y = 0; y < numPointsPerAxis.y; y++)
            {
                for (int z = 0; z < numPointsPerAxis.z; z++)
                {
                    float3 worldPos = new float3(
                        chunkCoord.x * chunkSize.x + (x * step),
                        y * step, // Use absolute world Y
                        chunkCoord.y * chunkSize.z + (z * step)
                    );

                    int index = GetIndex(x, y, z, numPointsPerAxis);
                    densityField[index] = CalculateDensity(
                        worldPos,
                        chunkSize.y,
                        terrainAmplitude,
                        continentalnessCurveSamples,
                        erosionCurveSamples,
                        peaksAndValleysCurveSamples,
                        continentalnessNoise,
                        erosionNoise,
                        peaksAndValleysNoise,
                        threeDNoiseSettings,
                        verticalGradientCurveSamples
                    );
                }
            }
        }

        // Step 2: Generate the mesh
        int3 numCubes = numPointsPerAxis - 1;
        for (int x = 0; x < numCubes.x; x++)
        {
            for (int y = 0; y < numCubes.y; y++)
            {
                for (int z = 0; z < numCubes.z; z++)
                {
                    cubeDensities[0] = densityField[GetIndex(x, y, z, numPointsPerAxis)];
                    cubeDensities[1] = densityField[GetIndex(x + 1, y, z, numPointsPerAxis)];
                    cubeDensities[2] = densityField[GetIndex(x + 1, y, z + 1, numPointsPerAxis)];
                    cubeDensities[3] = densityField[GetIndex(x, y, z + 1, numPointsPerAxis)];
                    cubeDensities[4] = densityField[GetIndex(x, y + 1, z, numPointsPerAxis)];
                    cubeDensities[5] = densityField[GetIndex(x + 1, y + 1, z, numPointsPerAxis)];
                    cubeDensities[6] = densityField[GetIndex(x + 1, y + 1, z + 1, numPointsPerAxis)];
                    cubeDensities[7] = densityField[GetIndex(x, y + 1, z + 1, numPointsPerAxis)];

                    // Pass the step size to Marching Cubes so the mesh is scaled correctly
                    MarchingCubes.March(
                        new float3(x * step, y * step, z * step),
                        isoLevel,
                        step, // Pass the step value here
                        cubeDensities,
                        vertices,
                        triangles,
                        edgeVertices
                    );
                }
            }
        }
    }


    private int GetIndex(int x, int y, int z, int3 numPointsPerAxis)
    {
        // A more stable, "flattened" version of the indexing formula
        return (z * numPointsPerAxis.y * numPointsPerAxis.x) + (y * numPointsPerAxis.x) + x;
    }

    private float CalculateDensity(
        float3 worldPos,
        float chunkHeight, // Needed for normalizing Y for the vertical gradient

        // --- Biome Parameters Passed from the BiomeSO ---
        float terrainAmplitude,
        NativeArray<float> continentalnessCurveSamples,
        NativeArray<float> erosionCurveSamples,
        NativeArray<float> peaksAndValleysCurveSamples,
        NoiseSettings continentalnessNoise,
        NoiseSettings erosionNoise,
        NoiseSettings peaksAndValleysNoise,
        NoiseSettings threeDNoiseSettings,
        NativeArray<float> verticalGradientCurveSamples)
    {
        // 1: Calculate the 2D Base Shape, these will be normalized from [0,1]

        float continentalness = EvaluateUsingCurveArray(FBM2D(worldPos, continentalnessNoise, octaveOffsetsContinentalness), continentalnessCurveSamples) * continentalnessNoise.scale;
        float erosion = EvaluateUsingCurveArray(FBM2D(worldPos, erosionNoise, octaveOffsetsErosion), erosionCurveSamples) * erosionNoise.scale;
        float peaksAndValleys = EvaluateUsingCurveArray(FBM2D(worldPos, peaksAndValleysNoise, octaveOffsetsPeaksAndValleys), peaksAndValleysCurveSamples) * peaksAndValleysNoise.scale;

        // Combine the remapped values to get the final terrain shape.
        // A simple addition is a good start. You can get creative here (e.g., multiplication).
        float divideFactor = continentalnessNoise.scale + erosionNoise.scale + peaksAndValleysNoise.scale;
        float normalizedNoise = (continentalness + erosion + peaksAndValleys) / divideFactor;

        // Calculate the final height of the 2D surface.
        float surfaceHeight = normalizedNoise * (terrainAmplitude-1);
        float clampedSurfaceHeight = Mathf.Clamp(surfaceHeight, 0, chunkHeight - 1);
        float baseDensity = clampedSurfaceHeight - worldPos.y; // in range of [-terrain amplitude, terrain amplitude], which should just be [-chunkheight, chunkheight] (assuming terrain amplitude is set to chunk height)

        // --- Step 2: Calculate the 3D Detail Modifier ---
        // Get a raw 3D noise value. This will be our "carving" value.

        // returns [0, 1]
        float centered3DNoise = FBM3D(worldPos, threeDNoiseSettings, octaveOffsets3D); // Using the full 3D position

        // Apply the vertical gradient "squashing" curve.
        // This makes 3D noise weaker deep underground and high in the sky.
        float normalizedY = worldPos.y / chunkHeight; // Assumes chunk starts at y=0

        float gradient = EvaluateUsingCurveArray(normalizedY, verticalGradientCurveSamples);
        float clampedGradient = math.saturate(gradient);
        // The final 3D modifier is scaled by the biome's overall influence and the gradient.
        // We multiply by amplitude to make the carving proportional to the terrain height.

        float threeDModifier = centered3DNoise * threeDNoiseSettings.scale * clampedGradient * terrainAmplitude;

        // --- Step 3: Combine 2D and 3D ---
        // Add the 3D modifier to the base density. This will push the surface inwards
        // (carving caves) or outwards (creating overhangs) from its original 2D position.
        float finalDensity = baseDensity - threeDModifier;
        //  THIS WILL HAVE A CARVING EFFECT INTO THE TERRAIN WHEN THE THREEDMODIFER IS POSITIVE,
        // WHILE IT WILL HAVE AN ADDING EFFECT INTO THE TERRAIN WHEN THE THREEDMODIFER IS NEGATIVE
        // SO MAKE IT SUPER POSITIVE WHERE YOU WANT CAVES, AND NEGATIVE WHERE YOU WANT OVERHANGS (in the vertical gradient animation curve)

        return finalDensity;
    }

    // this function is meant to simulate the .evaluate() function for animation curves, but instead its using a native<float> array
    private float EvaluateUsingCurveArray(float value, NativeArray<float> curves)
    {
        float floatIndex = value * (curves.Length - 1);
        int floorIndex = (int)floatIndex;
        int ceilIndex = math.min(floorIndex + 1, curves.Length - 1);
        float floor = curves[floorIndex];
        float ceil = curves[ceilIndex];
        float fraction = floatIndex - floorIndex;
        return math.lerp(floor, ceil, fraction);
    }

    private float FBM2D(float3 worldPos, NoiseSettings noiseSettings, NativeArray<float2> octaveOffsets)
    {
        float lacunarity = noiseSettings.lacunarity;
        float persistence = noiseSettings.persistence;
        float frequency = noiseSettings.frequency;
        float amplitude = 1;
        int octaves = noiseSettings.octaves;

        float noiseHeight = 0f;
        float maxPossibleAmplitude = 0f; // Keep track of the max possible value'

        for (int i = 0; i < octaves; i++)
        {
            float noiseValue = noise.snoise((new float2(worldPos.x, worldPos.z) + octaveOffsets[i] + noiseOffset) * frequency);
            noiseHeight += noiseValue * amplitude;

            maxPossibleAmplitude += amplitude; // Add the current amplitude to the max

            frequency *= lacunarity;
            amplitude *= persistence;
        }

        // --- Remapping Logic ---

        // 1. Normalize the noiseHeight.
        // We shift the [-max, +max] range to [0, 2*max] and then divide to get [0, 1].
        float normalizedNoise = (noiseHeight + maxPossibleAmplitude) / (2 * maxPossibleAmplitude);

        return normalizedNoise;
    }

    private float FBM3D(float3 worldPos, NoiseSettings noiseSettings, NativeArray<float3> octaveOffsets)
    {
        float lacunarity = noiseSettings.lacunarity;
        float persistence = noiseSettings.persistence;
        float frequency = noiseSettings.frequency;
        float amplitude = 1;
        int octaves = noiseSettings.octaves;

        float noiseValue = 0f;
        float maxPossibleAmplitude = 0f;

        for (int i = 0; i < octaves; i++)
        {
            // Sample 3D noise. We add a large number to the z-component of the offset
            // to ensure it samples a different "slice" of 2D offset noise.
            float3 offset = new float3(octaveOffsets[i].x + noiseOffset.x, octaveOffsets[i].y, octaveOffsets[i].z + noiseOffset.y);

            noiseValue += noise.snoise(worldPos * frequency + offset) * amplitude;
            maxPossibleAmplitude += amplitude;
            frequency *= lacunarity;
            amplitude *= persistence;
        }

        // Remap the noise from [-max, +max] to [0, 1]
        float normalizedNoise = (noiseValue + maxPossibleAmplitude) / (2 * maxPossibleAmplitude);

        // remap the noise from [0, 1] to [-1, 1]
        return (normalizedNoise - 0.5f) * 2f;
        // return normalizedNoise;
    }
}