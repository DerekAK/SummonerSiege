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
    public float caveStrength;
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
    public float heightBiasForCaves;
    [ReadOnly] public NativeArray<float> verticalGradientCurveSamples;
    [ReadOnly] public NativeArray<float> worleyVerticalGradientSamples;
    [ReadOnly] public NativeArray<float2> octaveOffsetsContinentalness;
    [ReadOnly] public NativeArray<float2> octaveOffsetsErosion;
    [ReadOnly] public NativeArray<float2> octaveOffsetsPeaksAndValleys;
    [ReadOnly] public NativeArray<float3> octaveOffsets3D;
    [ReadOnly] public NativeArray<float3> octaveOffsetsWarp;

    [ReadOnly] public NativeArray<NoiseFunction> continentalnessNoiseFunctions;
    [ReadOnly] public NativeArray<NoiseFunction> erosionNoiseFunctions;
    [ReadOnly] public NativeArray<NoiseFunction> peaksAndValleysNoiseFunctions;

    public void Execute()
    {
        int step = 1 << lod;
        
        // Add 1 extra point on each border that will overlap with neighbors
        int3 numPointsPerAxis = chunkSize / step + 2; // Changed from +1 to +2

        // Step 1: Calculate the density field with extra border
        for (int x = 0; x < numPointsPerAxis.x; x++)
        {
            for (int y = 0; y < numPointsPerAxis.y; y++)
            {
                for (int z = 0; z < numPointsPerAxis.z; z++)
                {
                    // Offset by -1 step to create overlap
                    float3 worldPos = new float3(
                        chunkCoord.x * chunkSize.x + ((x - 1) * step),
                        (y - 1) * step,
                        chunkCoord.y * chunkSize.z + ((z - 1) * step)
                    );

                    int index = GetIndex(x, y, z, numPointsPerAxis);
                    densityField[index] = CalculateDensity(
                        worldPos,
                        chunkSize.y,
                        terrainAmplitudeFactor,
                        caveStrength,
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
                        worleyVerticalGradientSamples,
                        continentalnessNoiseFunctions,
                        erosionNoiseFunctions,
                        peaksAndValleysNoiseFunctions
                    );
                }
            }
        }

        // Step 2: Generate the mesh with the extended grid
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

                    // Offset back to local coordinates
                    MarchingCubes.March(
                        new float3((x - 1) * step, (y - 1) * step, (z - 1) * step),
                        isoLevel,
                        step,
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
        float caveStrength,
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
        NativeArray<float> worleyVerticalGradientSamples,
        NativeArray<NoiseFunction> continentalnessNoiseFunctions,
        NativeArray<NoiseFunction> erosionNoiseFunctions,
        NativeArray<NoiseFunction> peaksAndValleysNoiseFunctions
        )
    {
        // --- CONTINENTALNESS ---
        float continentalnessRaw = Noise.FBM2D(worldPos, continentalnessNoise, octaveOffsetsContinentalness);
        float continentalnessModified = ApplyNoiseFunctions(continentalnessRaw, continentalnessNoise, continentalnessNoiseFunctions); // Apply function
        float continentalness = EvaluateUsingCurveArray(continentalnessModified, continentalnessCurveSamples) * continentalnessNoise.scale;

        // --- EROSION ---
        float erosionRaw = Noise.FBM2D(worldPos, erosionNoise, octaveOffsetsErosion);
        float erosionModified = ApplyNoiseFunctions(erosionRaw, erosionNoise, erosionNoiseFunctions); // Apply function
        float erosion = EvaluateUsingCurveArray(erosionModified, erosionCurveSamples) * erosionNoise.scale;

        // --- PEAKS & VALLEYS ---
        float peaksAndValleysRaw = Noise.FBM2D(worldPos, peaksAndValleysNoise, octaveOffsetsPeaksAndValleys);
        float peaksAndValleysModified = ApplyNoiseFunctions(peaksAndValleysRaw, peaksAndValleysNoise, peaksAndValleysNoiseFunctions); // Apply function
        float peaksAndValleys = EvaluateUsingCurveArray(peaksAndValleysModified, peaksAndValleysCurveSamples) * peaksAndValleysNoise.scale;

        // Combine the remapped values to get the final terrain shape.
        // A simple addition is a good start. You can get creative here (e.g., multiplication).
        float divideFactor = continentalnessNoise.scale + erosionNoise.scale + peaksAndValleysNoise.scale;
        float normalizedNoise = (continentalness + erosion + peaksAndValleys) / divideFactor;

        float caveHeightMask = math.pow(normalizedNoise, heightBiasForCaves) * caveStrength;

        // Calculate the final height of the 2D surface.
        float surfaceHeight = normalizedNoise * terrainAmplitudeFactor * (chunkHeight - 1);
        float clampedSurfaceHeight = Mathf.Clamp(surfaceHeight, 0, chunkHeight - 1);
        float baseDensity = clampedSurfaceHeight - worldPos.y; // in range of [-terrain amplitude, terrain amplitude], which should just be [-chunkheight, chunkheight] (assuming terrain amplitude is set to chunk height)

        // ----- Start of 3D Noise -------

        // returns [-1, 1]
        float centered3DNoise = Noise.FBM3D(worldPos, threeDNoiseSettings, octaveOffsets3D); // Using the full 3D position
        //float centered3DNoise = 0;
        float normalizedY = worldPos.y / chunkHeight; // Assumes chunk starts at y=0
        float gradient = math.saturate(EvaluateUsingCurveArray(normalizedY, verticalGradientCurveSamples));
        float threeDModifier = centered3DNoise * threeDNoiseSettings.scale * gradient * terrainAmplitudeFactor * (chunkHeight - 1);


        float3 warpOffset = Noise.FBM3D(worldPos, warpNoiseSettings, octaveOffsetsWarp) * warpNoiseSettings.amplitude * warpNoiseSettings.scale;
        float3 warpPos = worldPos + warpOffset;
        float2 worleyValues = Noise.GetWorleyF1F2(warpPos * cavernNoiseSettings.frequency);
        float f1 = worleyValues.x;
        float f2 = worleyValues.y;

        // This value is low on the ridges and high in the cell centers
        float ridgeValue = f2 - f1;

        // We invert it so the ridges have a high value (close to 1)
        float invertedRidgeValue = 1.0f - ridgeValue;

        float cavernGradient = EvaluateUsingCurveArray(normalizedY, worleyVerticalGradientSamples);
        float sharpenedWorley = math.pow(invertedRidgeValue, cavernNoiseSettings.caveSharpness);
        float cavernCarvingValue = sharpenedWorley * terrainAmplitudeFactor * (chunkHeight-1) * cavernGradient * cavernNoiseSettings.scale;

        float final3DModifier = (threeDModifier + cavernCarvingValue) * caveHeightMask;

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

    
    private float ApplyNoiseFunctions(float normalizedNoise, NoiseSettings settings, NativeArray<NoiseFunction> functions)
    {
        // If there are no functions, just return the base noise.
        if (functions.Length == 0)
        {
            return normalizedNoise;
        }

        float calculatedNoise = 0f;
        float maxPossibleValue = 0f;

        // Loop through each function and accumulate its result.
        foreach (NoiseFunction function in functions)
        {
            float functionResult = 0f;
            switch (function)
            {
                case NoiseFunction.Standard:
                    functionResult = normalizedNoise;
                    break; // Use break to prevent falling through to the next case.

                case NoiseFunction.Power:
                    functionResult = math.pow(normalizedNoise, settings.power);
                    break;

                case NoiseFunction.Billow:
                    float remappedBillow = normalizedNoise * 2f - 1f; // Remap [0, 1] to [-1, 1]
                    functionResult = math.abs(remappedBillow); // Result is [0, 1]
                    break;

                case NoiseFunction.Ridged:
                    float remappedRidged = normalizedNoise * 2f - 1f; // Remap [0, 1] to [-1, 1]
                    functionResult = 1f - math.abs(remappedRidged); // Result is [0, 1]
                    break;

                case NoiseFunction.Terraced:
                    functionResult = math.floor(normalizedNoise * settings.terraceSteps) / settings.terraceSteps;
                    break;
            }

            // Add the result of the current function to our total.
            calculatedNoise += functionResult;

            // Keep track of the max possible total to normalize it later.
            // Since all our functions output in the [0, 1] range, we just add 1.
            maxPossibleValue += 1f;
        }

        // After the loop, normalize the combined value and return it.
        // This ensures the final output is still in the [0, 1] range.
        return calculatedNoise / maxPossibleValue;
    }
    
    
}