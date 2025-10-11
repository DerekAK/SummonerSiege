using UnityEngine;
using System.IO;
using System.Collections.Generic;
// Import the new library
using Newtonsoft.Json;

public static class SaveLoadSystem
{
    // Define the folder and a single file for the entire world's data.
    private static readonly string saveFolder = Application.persistentDataPath + "/Saves/";
    private static readonly string worldSaveFile = "world_save.json";

    // These settings help Newtonsoft handle Unity types like Vector3 correctly.
    private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto,
        Formatting = Formatting.Indented, // Makes the JSON file human-readable
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
    };

    /// <summary>
    /// Saves the entire world's data to a single JSON file.
    /// </summary>
    /// <param name="worldData">The dictionary containing all persistent object data.</param>
    public static void SaveWorldData(Dictionary<string, object> worldData)
    {
        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
        }

        string filePath = Path.Combine(saveFolder, worldSaveFile);
        Debug.Log($"Saving world data to: {filePath}");

        // Use Newtonsoft to serialize the entire dictionary to a JSON string.
        string json = JsonConvert.SerializeObject(worldData, serializerSettings);
        
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Loads the entire world's data from a single JSON file.
    /// </summary>
    /// <returns>A dictionary containing all persistent object data.</returns>
    public static Dictionary<string, object> LoadWorldData()
    {
        string filePath = Path.Combine(saveFolder, worldSaveFile);
        Debug.Log(filePath);

        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);

            // Use Newtonsoft to deserialize the JSON string back into a dictionary.
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(json, serializerSettings);
        }
        
        // If no save file exists, return a new empty dictionary.
        return null;
    }
}