// TerrainDebuggerEditor.cs
using UnityEngine;
using UnityEditor; // We need this for editor scripting


// MAKE SURE TO HAVE TERRAINDEBUGGER GAMEOBJECT CLICKED IN SCENE VIEW WHEN DOING THIS!

// This tells Unity that this script is a custom editor for our TerrainDebugger
[CustomEditor(typeof(TerrainDebugger))]
public class TerrainDebuggerEditor : Editor
{
    void OnSceneGUI()
    {
        // 'target' is the TerrainDebugger component instance this editor is inspecting
        TerrainDebugger debugger = (TerrainDebugger)target;

        // Get the current event (mouse clicks, key presses, etc.) in the Scene View
        Event e = Event.current;

        // If a mouse button was pressed and it was the left button (button 0)
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            // Create a ray from the Scene View camera to the mouse position
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

            // Perform the raycast
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // We hit something! Call the public method on our debugger script
                debugger.FindChunkFromWorldPos(hit.point);

                // Tell the Scene View to repaint so we see the Gizmo update immediately
                SceneView.RepaintAll();
            }

            // This consumes the event so other things don't accidentally use the click
            e.Use();
        }
    }
}