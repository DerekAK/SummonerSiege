using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Biomes", menuName = "Scriptable Objects/BiomeSO")]
public class BiomeSO2 : ScriptableObject
{

    [Serializable]
    public class ColorInterval
    {
        public Color IntervalColor;
        [Range(0, 1)] public float StartNoise;
        [Range(0, 1)] public float EndNoise;
    }
    public string BiomeName;
    public List<ColorInterval> ColorIntervals = new();
    public AnimationCurve NoiseSplineCurve;
}

