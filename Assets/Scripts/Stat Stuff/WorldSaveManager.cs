using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class WorldSaveManager : NetworkBehaviour
{
    public static WorldSaveManager Instance { get; private set; }

    [Tooltip("How often the server will automatically save all data, in seconds.")]
    [SerializeField] private float autoSaveInterval = 300f; // 5 minutes

    // This dictionary holds the entire persistent state of the world in memory.
    private Dictionary<string, object> worldSaveData;
    public Dictionary<string, object> GetWorldSaveData() => worldSaveData;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // This script is server-only.
        if (!IsServer)
        {
            return;
        }

        LoadWorld();
        
        // Subscribe to server shutdown to ensure a final save.
        NetworkManager.Singleton.OnServerStopped += (isServer) => SaveWorld();

        // Start the periodic autosave routine.
        InvokeRepeating(nameof(SaveWorld), autoSaveInterval, autoSaveInterval);

    }

    /// <summary>
    /// Loads the entire world state from a file and applies it to all persistent objects.
    /// </summary>
    public void LoadWorld()
    {
        worldSaveData = SaveLoadSystem.LoadWorldData();

        if (worldSaveData == null)
        {
            worldSaveData = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Gathers data from all persistent objects and saves it to a file.
    /// </summary>
    private void SaveWorld()
    {        
        // Find every object in the world that needs saving.
        PersistenceManager[] persistenceManagers = FindObjectsByType<PersistenceManager>(FindObjectsSortMode.None);

        foreach (var pManager in persistenceManagers)
        {
            if (pManager.PersistenceType == PersistenceManager.ePersistenceType.World)
            {
                string uniqueId = pManager.GetUniqueId();
                worldSaveData[uniqueId] = pManager.SaveAllData();
            }
        }
        
        // Write the entire updated dictionary to the file.
        SaveLoadSystem.SaveWorldData(worldSaveData);
    }
}