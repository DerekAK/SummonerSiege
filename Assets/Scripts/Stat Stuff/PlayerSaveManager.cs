using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// A server-only manager to handle loading and saving data for individual players.
/// </summary>
public class PlayerSaveManager : NetworkBehaviour
{
    public static PlayerSaveManager Instance { get; private set; }

    [SerializeField] private float autoSaveInterval = 300f; // 5 minutes

    // Server-side cache of all connected players' PersistenceManagers
    private Dictionary<ulong, PersistenceManager> connectedPlayers = new Dictionary<ulong, PersistenceManager>();
    public bool IsNetworkReady { get; private set; } = false;

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
        IsNetworkReady = true; // needs to be before isServer check because all clients need to read this value
        
        if (!IsServer)
        {
            return;
        }

        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;

        // Start auto-save for players
        InvokeRepeating(nameof(SaveAllPlayers), autoSaveInterval, autoSaveInterval);

    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }


    /// <summary>
    /// Called by the server when a client disconnects. Saves their data.
    /// </summary>
    public void OnClientDisconnect(ulong clientId)
    {
        // Clean up the cache
        if (connectedPlayers.ContainsKey(clientId))
        {
            connectedPlayers.Remove(clientId);
        }
    }

    /// <summary>
    /// Server-side function to register a player's PersistenceManager when they spawn.
    /// </summary>
    public void RegisterPlayer(ulong clientId, PersistenceManager pManager)
    {
        connectedPlayers[clientId] = pManager;
    }

    /// <summary>
    /// Called by a ServerRpc from the client. Finds, loads, and sends data back to that client.
    /// </summary>
    public void LoadAndSendDataToClient(ulong clientId)
    {
        if (!IsServer) return;

        string playerId = GetPlayerId(clientId);
        Dictionary<string, object> playerData = SaveLoadSystem.LoadPlayerData(playerId);

        if (playerData == null) playerData = new Dictionary<string, object>();

        // Serialize the data to a string to send over the network
        string jsonPlayerData = JsonConvert.SerializeObject(playerData, SaveLoadSystem.GetSerializerSettings());

        // Get the specific player's relay to send the RPC
        if (connectedPlayers.TryGetValue(clientId, out PersistenceManager pManager))
        {

            // Get the relay component on the same object
            PlayerSaveDataRelay relay = pManager.GetComponent<PlayerSaveDataRelay>();
            if (relay != null)
            {
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { clientId }
                    }
                };

                relay.ReceivePlayerDataClientRpc(jsonPlayerData, clientRpcParams);
            }
        }
    }

   
    // this is called from the persistence manager itself to ensure its object isn't destroyed by the time onclientdisconnected is called
    public void SavePlayer(ulong clientId)
    {
        if (!IsServer || !connectedPlayers.ContainsKey(clientId)) return;

        string playerId = GetPlayerId(clientId);
        PersistenceManager pManager = connectedPlayers[clientId];
        
        if (pManager != null) // Check if player object still exists
        {
            Dictionary<string, object> playerData = pManager.SaveAllData();
            SaveLoadSystem.SavePlayerData(playerId, playerData);
        }
    }

    /// <summary>
    /// Iterates through all connected players and saves their data.
    /// </summary>
    public void SaveAllPlayers()
    {
        if (!IsServer) return;
        foreach (ulong clientId in connectedPlayers.Keys)
        {
            SavePlayer(clientId);
        }
    }

    /// <summary>
    /// This is where you would get the player's Steam ID.
    /// For now, we'll use your "host" and "client" concept.
    /// The host is always ClientId 0.
    /// </summary>
    private string GetPlayerId(ulong clientId)
    {
        if (clientId == NetworkManager.ServerClientId)
        {
            return "host";
        }
        else
        {
            // This will be "client_1", "client_2" etc.
            // For your 1-client test, this will be "client_1"
            return $"client_{clientId}"; 
        }
    }

}
