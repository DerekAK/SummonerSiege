using UnityEngine;
using UnityEditor;

[CustomEditor (typeof(MapGenerator))]
public class EditorScript : Editor
{   
    public override void OnInspectorGUI()
    {
        MapGenerator mapGen = (MapGenerator)target;

        if (DrawDefaultInspector() && mapGen.AutoUpdate)
        {
            mapGen.GenerateMap();
        }

        if (GUILayout.Button("Generate"))
        {
            mapGen.GenerateMap();
        }

    }
}