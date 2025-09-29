using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

// Make sure you have a static Noise class accessible to your jobs, like NoiseUtility.cs
// For example: public static class Noise { public static float FBM2D(...) { ... } }

[BurstCompile]
public struct ObjectPlacementJob : IJob
{
    // --- Input Data ---
    [ReadOnly] public NativeArray<float> densityField;
    public int3 chunkSize;
    public int2 chunkCoord;
    public uint seed;
    public float isoLevel;
    public int lod; // This makes the job aware of the density field's resolution

    // --- Object-Specific Rules (from PlaceableObject class) ---
    public float density;
    public NoiseSettings placementNoise;
    public float2 heightRange;
    public float2 slopeRange;
    public float2 scaleRange;

    // --- Reusable Noise Data ---
    [ReadOnly] public NativeArray<float2> octaveOffsets;

    // --- Output ---
    public NativeList<float4x4> objectMatrices;

    public void Execute()
    {
        var random = new Random(seed);

        // Calculate the correct grid dimensions and step size based on the LOD
        int step = 1 << lod;
        int3 numPointsPerAxis = chunkSize / step + 1;

        // Iterate over the LOD-adjusted grid, leaving a 1-unit border to prevent errors in CalculateNormal
        for (int x = 1; x < numPointsPerAxis.x - 1; x++)
        {
            for (int z = 1; z < numPointsPerAxis.z - 1; z++)
            {
                // Scale up the grid coordinates to get the world position for the noise check
                float3 worldPos = new float3(
                    chunkCoord.x * chunkSize.x + (x * step),
                    0,
                    chunkCoord.y * chunkSize.z + (z * step)
                );
                
                // Assuming you have a static Noise class with your FBM2D function
                float placementNoiseValue = Noise.FBM2D(worldPos, placementNoise, octaveOffsets);

                if (placementNoiseValue < (1f - density))
                {
                    continue; // Skip this point if it's not dense enough
                }

                // Raycast down the Y-axis of the grid to find the surface
                float surfaceY = -1;
                for (int y = numPointsPerAxis.y - 1; y >= 0; y--)
                {
                    float currentDensity = densityField[GetIndex(x, y, z, numPointsPerAxis)];
                    float belowDensity = (y > 0) ? densityField[GetIndex(x, y - 1, z, numPointsPerAxis)] : 1;

                    if (currentDensity < isoLevel && belowDensity >= isoLevel)
                    {
                        // Interpolate for a more accurate height in grid-space units
                        surfaceY = y - (currentDensity - isoLevel) / (belowDensity - currentDensity);
                        break;
                    }
                }

                if (surfaceY == -1) continue; // No surface found

                // Check Y-boundary before calculating normal
                int y_int = (int)surfaceY;
                if (y_int <= 0 || y_int >= numPointsPerAxis.y - 1)
                {
                    continue;
                }

                // Check placement rules (height is normalized against the full chunk height)
                float normalizedHeight = (surfaceY * step) / chunkSize.y;
                if (normalizedHeight < heightRange.x || normalizedHeight > heightRange.y)
                {
                    continue;
                }
                
                // It's now safe to calculate the normal
                float3 normal = CalculateNormal(x, y_int, z, numPointsPerAxis);
                float slope = math.degrees(math.acos(math.dot(normal, new float3(0, 1, 0))));

                if (slope < slopeRange.x || slope > slopeRange.y)
                {
                    continue;
                }

                // All checks passed! Create the final transformation matrix.
                // Scale the grid position (x, surfaceY, z) back up to the chunk's local space.
                float3 position = new float3(x * step, surfaceY * step, z * step);
                quaternion rotation = quaternion.LookRotation(normal, new float3(0, 1, 0)); // Align to surface normal
                rotation = math.mul(rotation, quaternion.RotateY(random.NextFloat(0, 2 * math.PI))); // Add random Y rotation
                float scale = random.NextFloat(scaleRange.x, scaleRange.y);

                objectMatrices.Add(float4x4.TRS(position, rotation, new float3(scale)));
            }
        }
    }

    /// Flattens a 3D grid coordinate into a 1D array index.
    private int GetIndex(int x, int y, int z, int3 numPointsPerAxis)
    {
        return (z * numPointsPerAxis.y * numPointsPerAxis.x) + (y * numPointsPerAxis.x) + x;
    }
    
    /// Calculates the surface normal by sampling the density of neighboring points.
    private float3 CalculateNormal(int x, int y, int z, int3 numPointsPerAxis)
    {
        float dx = densityField[GetIndex(x + 1, y, z, numPointsPerAxis)] - densityField[GetIndex(x - 1, y, z, numPointsPerAxis)];
        float dy = densityField[GetIndex(x, y + 1, z, numPointsPerAxis)] - densityField[GetIndex(x, y - 1, z, numPointsPerAxis)];
        float dz = densityField[GetIndex(x, y, z + 1, numPointsPerAxis)] - densityField[GetIndex(x, y, z - 1, numPointsPerAxis)];
        return -math.normalize(new float3(dx, dy, dz));
    }
}