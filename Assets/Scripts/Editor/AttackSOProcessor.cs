using UnityEditor;
using UnityEngine;

// This script automatically runs whenever assets are imported or changed.
public class AttackSOProcessor : AssetPostprocessor
{
    // This function is called by Unity after assets are processed
    static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        bool assetsChanged = false;

        foreach (string path in importedAssets)
        {
            // Check if the imported asset is a BaseAttackSO
            BaseAttackSO attackSO = AssetDatabase.LoadAssetAtPath<BaseAttackSO>(path);

            // If it is, and its ID is 0 (unassigned)
            if (attackSO != null && attackSO.UniqueID == 0)
            {
                // Get the asset's permanent GUID
                string guid = AssetDatabase.AssetPathToGUID(path);
                
                // Create a unique hash from that GUID
                int newID = guid.GetHashCode();
                
                // Ensure the ID is never 0, as we use 0 to mean "unassigned"
                if (newID == 0)
                {
                    newID = (guid + "salt").GetHashCode();
                }

                // Set the ID and mark the asset as "dirty" (changed)
                attackSO.UniqueID = newID;
                EditorUtility.SetDirty(attackSO);
                assetsChanged = true;
                
                Debug.Log($"Assigned new UniqueID {newID} to {attackSO.name}");
            }
        }
        
        // If we changed any assets, save them to disk
        if (assetsChanged)
        {
            AssetDatabase.SaveAssets();
        }
    }
}