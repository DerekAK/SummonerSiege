using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class Noise2
{

    public static Vector2[] GenerateOctaveOffsets(int octaves, int seed, Vector2 scrollOffset)
    {
        Vector2[] octaveOffsets = new Vector2[octaves];
        System.Random prng = new System.Random(seed);

        for (int i = 0; i < octaves; i++)
        {
            float octaveOffsetX = prng.Next(-100000, 100000) + scrollOffset.x;
            float octaveOffsetY = prng.Next(-100000, 100000) - scrollOffset.y;

            octaveOffsets[i] = new Vector2(octaveOffsetX, octaveOffsetY);
        }
        return octaveOffsets;
    }

    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, float scale, int octaves, float lacunarity, float persistence, int seed, Vector2 center, List<BiomeSO2> biomesList)
    {
        // lacunarity should generally be >1 for more frequency, while persistence should be <1 for less amplitude with more octaves

        Vector2[] octaveOffsets = GenerateOctaveOffsets(octaves, seed, center);

        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        float maxNoiseValue = float.MinValue;
        float minNoiseValue = float.MaxValue;

        float[,] noiseMap = new float[mapWidth, mapHeight];

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                // per pixel values
                float frequency = 1;
                float amplitude = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    // // Think of this as sampling Perlin noise values (sampleX and sampleY) over your 2D terrain grid.
                    // If your sample coordinates increase at a smaller rate as x and y increase (i.e., increasing 'scale'),
                    // then you’re sampling from a smaller portion of the Perlin noise space for the same terrain size.
                    // This means the noise will change more gradually across the map and appear more zoomed in.
                    //
                    // Increasing 'frequency' makes the noise vary more rapidly, so you’ll see more peaks and valleys
                    // within the same terrain range — a zoomed-out look in terms of noise detail.
                    //
                    // 'Amplitude' scales the contribution of each octave. Higher amplitude makes that octave’s
                    // features more pronounced, lower amplitude makes them subtler.


                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                    float noiseValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1; // to allow for negatives, that way heightmap isn't just super tall
                    noiseHeight += noiseValue * amplitude;

                    frequency *= lacunarity;
                    amplitude *= persistence;
                }

                minNoiseValue = Mathf.Min(minNoiseValue, noiseHeight);
                maxNoiseValue = Mathf.Max(maxNoiseValue, noiseHeight);

                noiseMap[x, y] = noiseHeight; // non-normalized right now
            }
        }

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseValue, maxNoiseValue, noiseMap[x, y]);
            }
        }
        return noiseMap;
    }
}
