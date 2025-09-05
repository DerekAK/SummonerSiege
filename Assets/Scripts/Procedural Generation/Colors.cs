using System.Collections.Generic;
using UnityEngine;

public static class Colors
{
    public static Color[] GenerateColorMap(float[,] noiseMap, int width, int height, List<BiomeSO2> biomesList)
    {
        // to edit for now, just specify a biome
        BiomeSO2 chosenBiome = biomesList[0];

        Color[] colorMap = new Color[width * height]; // 1d array storing data from 2d noise map
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                foreach (BiomeSO2.ColorInterval colorInterval in chosenBiome.ColorIntervals)
                {
                    float noise = noiseMap[x, y];
                    if (noise >= colorInterval.StartNoise && noise <= colorInterval.EndNoise)
                    {
                        colorMap[y * width + x] = colorInterval.IntervalColor;
                        break;
                    }
                }
            }
        }
        return colorMap;
    }

    public static Color[] GenerateColorMapFromDensityGrid(float[,,] densityGrid, int width, int height, int length, List<BiomeSO2> biomesList)
    {
        BiomeSO2 chosenBiome = biomesList[0];
        Color[] colorMap = new Color[width * height]; // 1d array storing data from 2d noise map
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < length; z++)
                {
                    foreach (BiomeSO2.ColorInterval colorInterval in chosenBiome.ColorIntervals)
                    {
                        float noise = densityGrid[x, y, z];
                        if (noise >= colorInterval.StartNoise && noise <= colorInterval.EndNoise)
                        {
                            colorMap[y * width + x] = colorInterval.IntervalColor;
                            break;
                        }
                    }
                }
            }
        }
        return colorMap;
    }

}
