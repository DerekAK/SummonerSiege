using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

[BurstCompile]
public struct ObjectPlacementJob : IJob
{
    // --- Input Data ---
    [ReadOnly] public NativeArray<float> densityField;
    public int3 chunkSize;
    public int2 chunkCoord;
    public uint seed;
    public float isoLevel;
    public int lod;

    // --- Object-Specific Rules (from PlaceableObject class) ---
    public float density;
    public NoiseSettings placementNoise;
    public float2 heightRange;
    public float2 slopeRange;
    public float2 scaleRange;
    public float yOffset;
    public bool randomYRotation;
    public bool placeVertical;

    // --- Reusable Noise Data ---
    // This now uses float3 offsets for 3D noise sampling
    [ReadOnly] public NativeArray<float3> octaveOffsets3D;

    // --- Output ---
    public NativeList<PlacementData> objectDataList;


    public void Execute()
    {
        var random = new Random(seed);
        int step = 1 << lod;
        int3 numPointsPerAxis = chunkSize / step + 1;

        // Iterate through the entire 3D volume, leaving a 1-unit border to prevent errors
        for (int x = 1; x < numPointsPerAxis.x - 1; x++)
        {
            for (int z = 1; z < numPointsPerAxis.z - 1; z++)
            {
                // We loop from the bottom-up to find all possible surfaces in this column
                for (int y = 1; y < numPointsPerAxis.y - 1; y++)
                {
                    // --- STEP 1: Identify a "floor" surface ---
                    float currentDensity = densityField[GetIndex(x, y, z, numPointsPerAxis)];
                    float aboveDensity = densityField[GetIndex(x, y + 1, z, numPointsPerAxis)];

                    // A "floor" is a transition from a solid voxel (current) to an air voxel (above)
                    if (currentDensity >= isoLevel && aboveDensity < isoLevel)
                    {
                        // --- STEP 2: Use 3D Noise for Clustering ---
                        // We use the world position of the found surface for the noise check
                        float3 worldPos = new float3(
                            chunkCoord.x * chunkSize.x + (x * step),
                            y * step,
                            chunkCoord.y * chunkSize.z + (z * step)
                        );

                        // Use 3D noise to create clusters that wrap around 3D surfaces
                        float placementNoiseValue = Noise.FBM3D(worldPos, placementNoise, octaveOffsets3D);
                        if (placementNoiseValue < (1f - density))
                        {
                            continue; // This spot isn't dense enough according to the 3D noise
                        }

                        // --- STEP 3: Perform Final Rule Checks ---
                        // Interpolate for a more precise Y position on the isosurface
                        float surfaceY = y + (isoLevel - currentDensity) / (aboveDensity - currentDensity);
                        
                        // Check height range (normalized against full chunk height)
                        float normalizedHeight = (surfaceY * step) / chunkSize.y;
                        if (normalizedHeight < heightRange.x || normalizedHeight > heightRange.y)
                        {
                            continue;
                        }

                        float3 normal = CalculateNormal(x, y, z, numPointsPerAxis);
                        float slope = math.degrees(math.acos(math.dot(normal, new float3(0, 1, 0))));
                        if (slope < slopeRange.x || slope > slopeRange.y)
                        {
                            continue;
                        }

                        // Sanity Check: Ensure this isn't a thin, floating island
                        int checkDepth = 2;
                        if (y > checkDepth)
                        {
                            float densityBelow = densityField[GetIndex(x, y - checkDepth, z, numPointsPerAxis)];
                            if (densityBelow < isoLevel)
                            {
                                continue; // The ground below is air, so skip this floating point
                            }
                        }

                        // --- STEP 4: All checks passed, create the object data ---
                        quaternion baseRotation;
                        if (placeVertical)
                        {
                            baseRotation = quaternion.identity; // Straight up
                        }
                        else
                        {
                            baseRotation = quaternion.LookRotation(normal, new float3(0, 1, 0)); // Aligned to slope
                        }

                        if (randomYRotation)
                        {
                            baseRotation = math.mul(baseRotation, quaternion.RotateY(random.NextFloat(0, 2 * math.PI)));
                        }

                        float3 position = new float3(x * step, (surfaceY * step) + yOffset, z * step);
                        float scale = random.NextFloat(scaleRange.x, scaleRange.y);

                        objectDataList.Add(new PlacementData
                        {
                            position = position,
                            rotation = baseRotation,
                            scale = scale
                        });
                    }
                }
            }
        }
    }

    private int GetIndex(int x, int y, int z, int3 numPointsPerAxis)
    {
        return (z * numPointsPerAxis.y * numPointsPerAxis.x) + (y * numPointsPerAxis.x) + x;
    }

    private float3 CalculateNormal(int x, int y, int z, int3 numPointsPerAxis)
    {
        float dx = densityField[GetIndex(x + 1, y, z, numPointsPerAxis)] - densityField[GetIndex(x - 1, y, z, numPointsPerAxis)];
        float dy = densityField[GetIndex(x, y + 1, z, numPointsPerAxis)] - densityField[GetIndex(x, y - 1, z, numPointsPerAxis)];
        float dz = densityField[GetIndex(x, y, z + 1, numPointsPerAxis)] - densityField[GetIndex(x, y, z - 1, numPointsPerAxis)];
        return -math.normalize(new float3(dx, dy, dz));
    }
}

public struct PlacementData
{
    public float3 position;
    public quaternion rotation;
    public float scale;
}