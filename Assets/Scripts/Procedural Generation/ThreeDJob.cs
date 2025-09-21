using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

[BurstCompile]
public struct ThreeDJob : IJob
{
    // --- Input Data ---
    public int2 chunkCoord;
    public int3 chunkSize;
    public float isoLevel;
    public int lod;

    // --- Reusable Input/Output Arrays ---
    public NativeArray<float> cubeDensities;
    public NativeArray<float3> edgeVertices;
    public NativeArray<float> densityField;

    // --- Output Lists ---
    public NativeList<float3> vertices;
    public NativeList<int> triangles;

    // --- Biome Data ---  
    public float terrainAmplitudeFactor;
    [ReadOnly] public NativeArray<float> continentalnessCurveSamples;
    [ReadOnly] public NativeArray<float> erosionCurveSamples;
    [ReadOnly] public NativeArray<float> peaksAndValleysCurveSamples;
    public NoiseSettings continentalnessNoise;
    public NoiseSettings erosionNoise;
    public NoiseSettings peaksAndValleysNoise;
    public NoiseSettings threeDNoiseSettings;
    public NoiseSettings cavernNoiseSettings;
    public NoiseSettings warpNoiseSettings;
    [ReadOnly] public NativeArray<float> verticalGradientCurveSamples;
    [ReadOnly] public NativeArray<float> cavernShapeCurveSamples;
    [ReadOnly] public NativeArray<float2> octaveOffsetsContinentalness;
    [ReadOnly] public NativeArray<float2> octaveOffsetsErosion;
    [ReadOnly] public NativeArray<float2> octaveOffsetsPeaksAndValleys;
    [ReadOnly] public NativeArray<float3> octaveOffsets3D;
    [ReadOnly] public NativeArray<float3> octaveOffsetsWarp;

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
                        terrainAmplitudeFactor,
                        continentalnessCurveSamples,
                        erosionCurveSamples,
                        peaksAndValleysCurveSamples,
                        continentalnessNoise,
                        erosionNoise,
                        peaksAndValleysNoise,
                        threeDNoiseSettings,
                        cavernNoiseSettings,
                        warpNoiseSettings,
                        verticalGradientCurveSamples,
                        cavernShapeCurveSamples
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
        float terrainAmplitudeFactor,
        NativeArray<float> continentalnessCurveSamples,
        NativeArray<float> erosionCurveSamples,
        NativeArray<float> peaksAndValleysCurveSamples,
        NoiseSettings continentalnessNoise,
        NoiseSettings erosionNoise,
        NoiseSettings peaksAndValleysNoise,
        NoiseSettings threeDNoiseSettings,
        NoiseSettings cavernNoiseSettings,
        NoiseSettings warpNoiseSettings,
        NativeArray<float> verticalGradientCurveSamples,
        NativeArray<float> cavernShapeCurveSamples)
    {
        // --- CONTINENTALNESS ---
        float continentalnessRaw = FBM2D(worldPos, continentalnessNoise, octaveOffsetsContinentalness);
        float continentalnessModified = ApplyNoiseFunction(continentalnessRaw, continentalnessNoise); // Apply function
        float continentalness = EvaluateUsingCurveArray(continentalnessModified, continentalnessCurveSamples) * continentalnessNoise.scale;

        // --- EROSION ---
        float erosionRaw = FBM2D(worldPos, erosionNoise, octaveOffsetsErosion);
        float erosionModified = ApplyNoiseFunction(erosionRaw, erosionNoise); // Apply function
        float erosion = EvaluateUsingCurveArray(erosionModified, erosionCurveSamples) * erosionNoise.scale;

        // --- PEAKS & VALLEYS ---
        float peaksAndValleysRaw = FBM2D(worldPos, peaksAndValleysNoise, octaveOffsetsPeaksAndValleys);
        float peaksAndValleysModified = ApplyNoiseFunction(peaksAndValleysRaw, peaksAndValleysNoise); // Apply function
        float peaksAndValleys = EvaluateUsingCurveArray(peaksAndValleysModified, peaksAndValleysCurveSamples) * peaksAndValleysNoise.scale;

        // Combine the remapped values to get the final terrain shape.
        // A simple addition is a good start. You can get creative here (e.g., multiplication).
        float divideFactor = continentalnessNoise.scale + erosionNoise.scale + peaksAndValleysNoise.scale;
        float normalizedNoise = (continentalness + erosion + peaksAndValleys) / divideFactor;

        // Calculate the final height of the 2D surface.
        float surfaceHeight = normalizedNoise * terrainAmplitudeFactor * (chunkHeight - 1);
        float clampedSurfaceHeight = Mathf.Clamp(surfaceHeight, 0, chunkHeight - 1);
        float baseDensity = clampedSurfaceHeight - worldPos.y; // in range of [-terrain amplitude, terrain amplitude], which should just be [-chunkheight, chunkheight] (assuming terrain amplitude is set to chunk height)

        // returns [-1, 1]
        float centered3DNoise = FBM3D(worldPos, threeDNoiseSettings, octaveOffsets3D); // Using the full 3D position
        float normalizedY = worldPos.y / chunkHeight; // Assumes chunk starts at y=0
        float gradient = EvaluateUsingCurveArray(normalizedY, verticalGradientCurveSamples);
        float clampedGradient = math.saturate(gradient);
        float threeDModifier = centered3DNoise * threeDNoiseSettings.scale * clampedGradient * terrainAmplitudeFactor * (chunkHeight - 1);


        float3 warpOffset = FBM3D(worldPos, warpNoiseSettings, octaveOffsetsWarp) * warpNoiseSettings.amplitude * warpNoiseSettings.scale;
        float3 warpPos = worldPos + warpOffset;
        float2 worleyValues = GetWorleyF1F2(warpPos * cavernNoiseSettings.frequency);
        float f1 = worleyValues.x;
        float f2 = worleyValues.y;

        // This value is low on the ridges and high in the cell centers
        float ridgeValue = f2 - f1;

        // We invert it so the ridges have a high value (close to 1)
        float invertedRidgeValue = 1.0f - ridgeValue;

        float cavernGradient = EvaluateUsingCurveArray(normalizedY, cavernShapeCurveSamples);
        float sharpenedWorley = math.pow(invertedRidgeValue, cavernNoiseSettings.caveSharpness);
        float cavernCarvingValue = sharpenedWorley * terrainAmplitudeFactor * (chunkHeight-1) * cavernGradient * cavernNoiseSettings.scale;

        float final3DModifier = threeDModifier + cavernCarvingValue;

        // --- Step 3: Combine 2D and 3D ---
        // Add the 3D modifier to the base density. This will push the surface inwards
        // (carving caves) or outwards (creating overhangs) from its original 2D position.
        float finalDensity = baseDensity - final3DModifier;

        return finalDensity;
    }

    // this function is meant to simulate the .evaluate() function for animation curves, but instead its using a native<float> array
    private float EvaluateUsingCurveArray(float value, NativeArray<float> curves)
    {
        value = math.saturate(value);
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
        float amplitude = noiseSettings.amplitude;
        int octaves = noiseSettings.octaves;

        float noiseHeight = 0f;
        float maxPossibleAmplitude = 0f; // Keep track of the max possible value'

        for (int i = 0; i < octaves; i++)
        {
            float noiseValue = noise.snoise((new float2(worldPos.x, worldPos.z) + octaveOffsets[i]) * frequency);
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
        float amplitude = noiseSettings.amplitude;
        int octaves = noiseSettings.octaves;

        float noiseValue = 0f;
        float maxPossibleAmplitude = 0f;

        for (int i = 0; i < octaves; i++)
        {
            // Sample 3D noise. We add a large number to the z-component of the offset
            // to ensure it samples a different "slice" of 2D offset noise.
            float3 offset = new float3(octaveOffsets[i].x, octaveOffsets[i].y, octaveOffsets[i].z);

            noiseValue += noise.snoise(worldPos * frequency + offset) * amplitude;
            maxPossibleAmplitude += amplitude;
            frequency *= lacunarity;
            amplitude *= persistence;
        }

        float carveBias = noiseSettings.carveBias;
        float center = carveBias * 0.5f;
        float carveScale = 1f - 0.5f * math.abs(carveBias);

        // Remap the noise from [-max, +max] to [0, 1]
        float normalizedNoise = (noiseValue + maxPossibleAmplitude) / (2 * maxPossibleAmplitude);

        float initialNoise = (normalizedNoise - 0.5f) * 2f;
        return initialNoise * carveScale + center;
    }

    // A simple hashing function to get a deterministic "random" float3 from an int3 position.
    private static float3 Hash(int3 p)
    {
        // This is a common, more chaotic hash function used in procedural generation
        // to break up grid-like artifacts.
        float3 p3 = math.frac((float3)p * new float3(.1031f, .1030f, .0973f));
        p3 += math.dot(p3, p3.yzx + 33.33f);
        return math.frac((p3.xxy + p3.yzz) * p3.zyx);
    }

    // returns normalized value [0,1]
    public static float2 GetWorleyF1F2(float3 pos)
    {
        int3 cell = (int3)math.floor(pos);
        float2 minDistance = new float2(2.0f, 2.0f); // Initialize F1 and F2 to a high value

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    int3 neighborCell = cell + new int3(x, y, z);
                    float3 featurePoint = neighborCell + Hash(neighborCell);

                    float dist = math.distance(pos, featurePoint);

                    // Check if this distance is a new F1 or F2
                    if (dist < minDistance.x)
                    {
                        minDistance.y = minDistance.x; // The old F1 becomes the new F2
                        minDistance.x = dist;          // We have a new F1
                    }
                    else if (dist < minDistance.y)
                    {
                        minDistance.y = dist;          // We have a new F2
                    }
                }
            }
        }
        return minDistance;
    }
    
    private float ApplyNoiseFunction(float normalizedNoise, NoiseSettings settings)
    {
        switch (settings.function)
        {
            case NoiseFunction.Standard:
                return normalizedNoise;

            case NoiseFunction.Power:
                return math.pow(normalizedNoise, settings.power);

            case NoiseFunction.Billow:
                // Billow creates puffy, cloud-like shapes. It's the absolute value of noise remapped from [-1, 1].
                float remappedBillow = normalizedNoise * 2f - 1f; // Remap [0, 1] to [-1, 1]
                return math.abs(remappedBillow);

            case NoiseFunction.Ridged:
                // Ridged is the inverse of Billow, creating sharp ridges.
                float remappedRidged = normalizedNoise * 2f - 1f; // Remap [0, 1] to [-1, 1]
                return 1f - math.abs(remappedRidged);

            case NoiseFunction.Terraced:
                // Creates distinct steps or terraces in the terrain.
                return math.floor(normalizedNoise * settings.terraceSteps) / settings.terraceSteps;
        }
        return normalizedNoise; // Default case
    }
    
    
}