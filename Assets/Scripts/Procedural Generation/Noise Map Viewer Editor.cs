using UnityEngine;
using UnityEditor; // Required for editor scripting

// This attribute tells Unity that this script is a custom editor for our NoiseMapViewer
[CustomEditor(typeof(NoiseMapViewer))]
public class NoiseMapViewerEditor : Editor
{
    // This method is called to draw the custom inspector
    public override void OnInspectorGUI()
    {
        // 'target' is the NoiseMapViewer component this editor is inspecting
        NoiseMapViewer viewer = (NoiseMapViewer)target;

        // Draw the default inspector fields (for the EndlessTerrain reference)
        DrawDefaultInspector();

        // Add some space for visual clarity
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Noise Controls", EditorStyles.boldLabel);

        // Update the offsets from the sliders and check if they changed
        bool valueChanged = false;
        float newX = EditorGUILayout.Slider("X Offset", viewer.xOffset, -10000, 10000);
        float newZ = EditorGUILayout.Slider("Z Offset", viewer.zOffset, -10000, 10000);

        if (newX != viewer.xOffset || newZ != viewer.zOffset)
        {
            viewer.xOffset = newX;
            viewer.zOffset = newZ;
            valueChanged = true;
        }

        // Add the "Generate" button
        if (GUILayout.Button("Generate"))
        {
            viewer.GenerateMap();
        }
        
        // Optional: A checkbox for auto-updating when the slider moves
        EditorGUILayout.Space();
        if (EditorGUILayout.Toggle("Auto-Update", false) && valueChanged)
        {
            viewer.GenerateMap();
        }
    }
}
