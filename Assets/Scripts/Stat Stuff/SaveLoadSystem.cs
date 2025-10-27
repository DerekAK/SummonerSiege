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
        EnsureSaveDirectoryExists();

        string filePath = Path.Combine(saveFolder, worldSaveFile);
        // Debug.Log($"Saving world data to: {filePath}");

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
        //Debug.Log(filePath);

        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);

            // Use Newtonsoft to deserialize the JSON string back into a dictionary.
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(json, serializerSettings);
        }

        // If no save file exists, return a new empty dictionary.
        return null;
    }
    
    private static string GetPlayerSaveFile(string playerId)
    {
        // This will result in "player_host.json", "player_client.json", etc.
        return $"player_{playerId}.json";
    }

    /// <summary>
    /// Saves a specific player's data to their own file.
    /// </summary>
    public static void SavePlayerData(string playerId, Dictionary<string, object> playerData)
    {
        EnsureSaveDirectoryExists();
        string fileName = GetPlayerSaveFile(playerId);
        string filePath = Path.Combine(saveFolder, fileName);
        //Debug.Log($"Saving PLAYER data for '{playerId}' to: {filePath}");

        string json = JsonConvert.SerializeObject(playerData, serializerSettings);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Loads a specific player's data from their file.
    /// </summary>
    public static Dictionary<string, object> LoadPlayerData(string playerId)
    {
        string fileName = GetPlayerSaveFile(playerId);
        string filePath = Path.Combine(saveFolder, fileName);

        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(json, serializerSettings);
        }
        
        // If no save file exists for this player, return null.
        return null;
    }

    private static void EnsureSaveDirectoryExists()
    {
        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
        }
    }

    /// <summary>
    /// Helper to get the serializer settings for sending data over the network.
    /// </summary>
    public static JsonSerializerSettings GetSerializerSettings()
    {
        return serializerSettings;
    }
}