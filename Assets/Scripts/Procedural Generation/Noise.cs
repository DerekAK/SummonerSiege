using Unity.Mathematics;
using Unity.Collections;

public static class Noise
{
    public static float FBM2D(float3 worldPos, NoiseSettings noiseSettings, NativeArray<float2> octaveOffsets)
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

    public static float FBM3D(float3 worldPos, NoiseSettings noiseSettings, NativeArray<float3> octaveOffsets)
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

    // A simple hashing function to get a deterministic "random" float3 from an int3 position.
    private static float3 Hash(int3 p)
    {
        // This is a common, more chaotic hash function used in procedural generation
        // to break up grid-like artifacts.
        float3 p3 = math.frac((float3)p * new float3(.1031f, .1030f, .0973f));
        p3 += math.dot(p3, p3.yzx + 33.33f);
        return math.frac((p3.xxy + p3.yzz) * p3.zyx);
    }
    public static NativeArray<float2> Get2DOctaveOffsets(int seed, int octaves)
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

    public static NativeArray<float3> Get3DOctaveOffsets(int seed, int octaves)
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
}
