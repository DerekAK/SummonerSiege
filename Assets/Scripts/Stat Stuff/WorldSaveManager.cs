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

        PersistenceManager[] persistenceManagers = FindObjectsByType<PersistenceManager>(FindObjectsSortMode.None);
        worldSaveData = SaveLoadSystem.LoadWorldData();

        if (worldSaveData == null)
        {
            worldSaveData = new Dictionary<string, object>();
        }

        foreach (PersistenceManager pManager in persistenceManagers)
        {
            if (pManager.PersistenceType == PersistenceManager.ePersistenceType.World)
            {
                string uniqueId = pManager.GetComponent<PersistenceManager>().GetUniqueId();

                // If we have saved data for this object's ID, apply it.
                if (worldSaveData.TryGetValue(uniqueId, out object savedDataForPManager))
                {
                    pManager.ApplyAllData(savedDataForPManager as Dictionary<string, object>);
                }
                else // worldSaveData doesn't have this value, most likely because it was empty
                {
                    pManager.ApplyAllData(new Dictionary<string, object>()); // this will be an empty dictionary if there was no save file. 
                }
            }
        }
    }

    /// <summary>
    /// Gathers data from all persistent objects and saves it to a file.
    /// </summary>
    public void SaveWorld()
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