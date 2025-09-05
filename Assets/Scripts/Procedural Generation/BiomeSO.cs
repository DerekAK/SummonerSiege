using UnityEngine;
using UnityEngine.VFX;

// This attribute allows you to create instances of this object in the Unity Editor.
// Right-click in the Project window -> Create -> Procedural -> Biome
[CreateAssetMenu(fileName = "NewBiome", menuName = "Procedural/Biome")]
public class BiomeSO : ScriptableObject
{
    [Header("Terrain Shape Settings")]
    [Tooltip("The average height of the terrain, acting as the 'sea level' for this biome.")]
    public float baseHeight = 32f;

    [Tooltip("The maximum height variation. Higher values create taller mountains and deeper valleys.")]
    public float terrainAmplitude = 50f;

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
    [Tooltip("Overall strength of the 3D noise used for caves and overhangs. 0 = no 3D noise, 1 = full effect.")]
    [Range(0, 1)]
    public float threeDNoiseInfluence = 0.5f;

    public NoiseSettings threeDNoise;

    [Tooltip("Controls the 3D noise influence based on world height (Y-axis, normalized 0-1). Allows making the ground solid at the bottom and removing noise high in the sky.")]
    public AnimationCurve verticalGradientCurve;


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

// A reusable struct for noise settings to keep the Inspector clean.
[System.Serializable]
public struct NoiseSettings
{
    [Range(0,1)]
    public float scale;
    public int octaves;
    public float lacunarity;
    public float persistence;
    public float frequency;
}

// A class for defining rules for spawning objects like trees, rocks, etc.
[System.Serializable]
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