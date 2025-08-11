using UnityEngine;

public static class Noise
{
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, float scale, int octaves, float lacunarity, float persistence, int seed, Vector2 scrollOffset)
    {
        // lacunarity should generally be >1 for more frequency, while persistence should be <1 for less amplitude with more octaves

        Vector2[] octaveOffsets = GenerateOctaveOffsets(octaves, seed, scrollOffset);


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
                    // increasing scale will decrease frequency. Making sampleX and sampleY smaller over this 2d array
                    // means you are sampling points for perlin noise in a smaller window, so the change from one pixel to the other
                    // will change more gradually and appear more zoomed in. so smaller frequency, greater scale means zoomed in

                    float sampleX = x / scale * frequency + octaveOffsets[i].x;
                    float sampleY = y / scale * frequency + octaveOffsets[i].y;

                    float noiseValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1; // to allow for negatives, that way heightmap isn't just super tall
                    noiseHeight += noiseValue * amplitude;

                    frequency *= lacunarity;
                    amplitude *= persistence;
                }

                minNoiseValue = Mathf.Min(minNoiseValue, noiseHeight);
                maxNoiseValue = Mathf.Max(maxNoiseValue, noiseHeight);

                noiseMap[x, y] = noiseHeight; // un-normalized right now
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

    private static Vector2[] GenerateOctaveOffsets(int octaves, int seed, Vector2 scrollOffset)
    {
        Vector2[] octaveOffsets = new Vector2[octaves];
        System.Random prng = new System.Random(seed);

        for (int i = 0; i < octaves; i++)
        {
            float octaveOffsetX = prng.Next(-100000, 100000) + scrollOffset.x;
            float octaveOffsetY = prng.Next(-100000, 100000) + scrollOffset.y;

            octaveOffsets[i] = new Vector2(octaveOffsetX, octaveOffsetY);
        }
        return octaveOffsets;
    }
}
