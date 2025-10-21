using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System;

public class ServerSaveManager : NetworkBehaviour
{
    public static ServerSaveManager Instance { get; private set; }

    [Tooltip("How often the server will automatically save all data, in seconds.")]
    [SerializeField] private float autoSaveInterval = 300f; // 5 minutes

    // This dictionary holds the entire persistent state of the world in memory.
    private Dictionary<string, object> worldSaveData;
    public static event Action OnWorldDataLoaded;

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
            enabled = false;
            Destroy(this);
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
            string uniqueId = pManager.GetComponent<PersistenceManager>().GetUniqueId();

            // If we have saved data for this object's ID, apply it.
            if (worldSaveData.TryGetValue(uniqueId, out object savedDataForPManager))
            {
                pManager.ProvideAllData(savedDataForPManager as Dictionary<string, object>);
            }
            else // worldSaveData doesn't have this value, most likely because it was empty
            {
                pManager.ProvideAllData(new Dictionary<string, object>()); // this will be an empty dictionary if there was no save file. 
            }
        }
        OnWorldDataLoaded?.Invoke();
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
            string uniqueId = pManager.GetComponent<PersistenceManager>().GetUniqueId();
            // Gather all data from the object and update our in-memory save data.
            worldSaveData[uniqueId] = pManager.SaveAllData();
        }
        
        // Write the entire updated dictionary to the file.
        SaveLoadSystem.SaveWorldData(worldSaveData);
    }
    
    // Unsubscribe on destroy to prevent errors.
    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            // The null check is important in case NetworkManager is already destroyed.
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStopped -= (isHost) => SaveWorld();
            }
        }
    }
}