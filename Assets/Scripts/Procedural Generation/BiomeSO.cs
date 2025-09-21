using System;
using UnityEngine;
using UnityEngine.VFX;

// This attribute allows you to create instances of this object in the Unity Editor.
// Right-click in the Project window -> Create -> Procedural -> Biome
[CreateAssetMenu(fileName = "NewBiome", menuName = "Procedural/Biome")]
public class BiomeSO : ScriptableObject
{

    [Tooltip("Multiplied by chunkheight, so 1 will get a value of chunkheight")]
    public float terrainAmplitudeFactor;

    [Tooltip("Maps the low-frequency Continentalness noise to a height factor. Controls the largest landmass shapes.")]
    public AnimationCurve continentalnessCurve;

    [Tooltip("Maps the Erosion noise to a height factor. Controls the general roughness and smoothness of terrain.")]
    public AnimationCurve erosionCurve;

    [Tooltip("Maps the Peaks & Valleys noise to a height factor. Controls the fine details of hills, mountains, and valleys.")]
    public AnimationCurve peaksAndValleysCurve;

    // --- Noise map settings ---
    public NoiseSettings continentalnessNoise;
    public NoiseSettings erosionNoise;
    public NoiseSettings peaksAndValleysNoise;

    [Header("3D Detail Settings")]
    public NoiseSettings threeDNoise;

    [Tooltip("Controls the 3D noise influence based on world height (Y-axis, normalized 0-1). Allows making the ground solid at the bottom and removing noise high in the sky.")]
    public AnimationCurve verticalGradientCurve;
    public NoiseSettings cavernNoise; // <-- Add this for Worley noise
    public AnimationCurve cavernShapeCurve; // <-- Add this to shape the caves
    public NoiseSettings warpNoise;

    [Header("Aesthetic & Spawning Settings")]
    [Tooltip("The main material for the terrain. Use a custom Shader Graph material for best results.")]
    public Material terrainMaterial;

    [Tooltip("The skybox to use for this biome.")]
    public Material skyboxMaterial;

    [Tooltip("Ambient particle effects like dust, leaves, or snow.")]
    public VisualEffectAsset vfxGraphAsset;

    [Tooltip("List of objects to spawn in this biome.")]
    public SpawnableObject[] spawnableObjects;

    [Header("Atmospherics")]
    public Color fogColor = Color.gray;

    [Tooltip("Controls the thickness of the fog.")]
    public float fogDensity = 0.01f;
}

public enum NoiseFunction { Standard, Power, Billow, Ridged, Terraced }


[Serializable]
public struct NoiseSettings
{
    [Header("2D Function Modifiers")]
    public NoiseFunction function; 
    [Tooltip("For 'Power': a value of 1 is normal. > 1 creates sharper peaks, < 1 creates flatter plateaus.")]
    [Range(0.1f, 5f)]
    public float power;
    [Tooltip("For 'Terraced': the number of distinct steps or layers.")]
    [Range(2, 20)]
    public int terraceSteps;
    [Range(-1, 1)]
    [Tooltip("Only used for 3d noise")]
    public float carveBias;
    [Tooltip("Only used for cavern shape")]
    public float caveSharpness;

    [Header("Base Noise Shape")]
    [Range(0, 1)]
    public float scale;
    public int octaves;
    public float lacunarity;
    public float persistence;
    public float frequency;
    
    [Tooltip("Should always be 1 except for WarpSettings")]
    public float amplitude;

}

// A class for defining rules for spawning objects like trees, rocks, etc.
[Serializable]
public class SpawnableObject
{
    public GameObject prefab;
    [Range(0, 1)]
    public float density; // 0-1 chance of spawning in a valid spot.

    [Header("Placement Rules")]
    [Tooltip("The valid range of surface slopes (in degrees) this can spawn on. 0 = flat, 90 = vertical cliff.")]
    public Vector2 validSlopeRange = new Vector2(0, 30);

    [Tooltip("The valid range of world heights this can spawn on.")]
    public Vector2 validHeightRange = new Vector2(0, 256);
}