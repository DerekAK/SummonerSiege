using System;
using UnityEngine;

// This attribute allows you to create instances of this object in the Unity Editor.
// Right-click in the Project window -> Create -> Procedural -> Biome
[CreateAssetMenu(fileName = "NewBiome", menuName = "Procedural/Biome")]
public class BiomeSO : ScriptableObject
{

    [Tooltip("Multiplied by chunkheight, so 1 will get a value of chunkheight")]
    public float terrainAmplitudeFactor;

    [Tooltip("1 means no change, >1 means that higher 2d terrain heights will have higher 3d noise values hopefully for higher cave percentage")]
    public float heightBiasForCaves;

    public float caveStrength;

    [Tooltip("True to use same octave offsets for all 3 2d noise settings")]
    public bool sameOctaveOffsets;


    [Tooltip("Maps the low-frequency Continentalness noise to a height factor. Controls the largest landmass shapes.")]
    public AnimationCurve continentalnessCurve;

    [Tooltip("Maps the Erosion noise to a height factor. Controls the general roughness and smoothness of terrain.")]
    public AnimationCurve erosionCurve;

    [Tooltip("Maps the Peaks & Valleys noise to a height factor. Controls the fine details of hills, mountains, and valleys.")]
    public AnimationCurve peaksAndValleysCurve;

    [Tooltip("Controls the 3D noise influence based on world height (Y-axis, normalized 0-1). Allows making the ground solid at the bottom and removing noise high in the sky.")]
    public AnimationCurve verticalGradientCurve;

    [Tooltip("Controls the 3D noise influence based on world height (Y-axis, normalized 0-1). Allows making the ground solid at the bottom and removing noise high in the sky.")]
    public AnimationCurve worleyVerticalGradientCurve; // <-- Add this to shape the caves

    // --- Noise map settings ---
    public NoiseFunction[] continentalnessNoiseFunctions;
    public NoiseSettings continentalnessNoise;
    public NoiseFunction[] erosionNoiseFunctions;
    public NoiseSettings erosionNoise;
    public NoiseFunction[] peaksAndValleysNoiseFunctions;
    public NoiseSettings peaksAndValleysNoise;

    [Header("3D Detail Settings")]
    public NoiseSettings threeDNoise;
    public NoiseSettings cavernNoise; // <-- Add this for Worley noise
    public NoiseSettings warpNoise;

}

public enum NoiseFunction { Standard, Power, Billow, Ridged, Terraced }


[Serializable]
public struct NoiseSettings
{

    [Range(0, 1)]
    public float scale;

    [Header("2D Function Modifiers")]
    [Tooltip("For 'Power': a value of 1 is normal. > 1 creates sharper peaks, < 1 creates flatter plateaus.")]
    [Range(0.1f, 10f)]
    public float power;
    [Tooltip("For 'Terraced': the number of distinct steps or layers.")]
    [Range(2, 20)]
    public int terraceSteps;
    [Range(-1, 1)]
    [Tooltip("Only used for 3d noise")]
    public float carveBias;
    [Tooltip("Only used for cavern shape")]
    public float caveSharpness;

    [Header("Base Noise Attributes")]
    public int octaves;
    public float lacunarity;
    public float persistence;
    public float frequency;

    [Tooltip("Should always be 1 except for WarpSettings")]
    public float amplitude;

}

