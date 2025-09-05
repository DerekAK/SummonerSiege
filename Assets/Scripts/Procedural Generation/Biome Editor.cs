using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BiomeSO2))]
public class BiomeSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        BiomeSO2 biome = (BiomeSO2)target;

        // Draw default inspector for fields other than ColorIntervals
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Color Intervals", EditorStyles.boldLabel);

        // Loop through the intervals
        for (int i = 0; i < biome.ColorIntervals.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Interval {i + 1}");

            var interval = biome.ColorIntervals[i];
            interval.IntervalColor = EditorGUILayout.ColorField("Color", interval.IntervalColor);
            interval.StartNoise = EditorGUILayout.FloatField("Start Noise", interval.StartNoise);
            interval.EndNoise = EditorGUILayout.FloatField("End Noise", interval.EndNoise);
            biome.ColorIntervals[i] = interval; // Assign back after modification

            // Auto-adjust adjacent start/end values if you want that "fancy link" behavior
            if (i < biome.ColorIntervals.Count - 1)
            {
                biome.ColorIntervals[i + 1] = new BiomeSO2.ColorInterval
                {
                    IntervalColor = biome.ColorIntervals[i + 1].IntervalColor,
                    StartNoise = interval.EndNoise,
                    EndNoise = biome.ColorIntervals[i + 1].EndNoise
                };
            }

            if (GUILayout.Button("Remove Interval"))
            {
                biome.ColorIntervals.RemoveAt(i);
                break; // Exit loop to avoid index issues
            }

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Add Color Interval"))
        {
            biome.ColorIntervals.Add(new BiomeSO2.ColorInterval { IntervalColor = Color.white, StartNoise = 0f, EndNoise = 1f });
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(biome);
        }
    }
}
