using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class NetworkHandler : NetworkBehaviour
{
    public static NetworkHandler Instance {get; private set;}
    private Dictionary<ulong, NetworkObject> connectedPlayers = new Dictionary<ulong, NetworkObject>();
    

    private void Awake(){
        // Singleton enforcement
        if (Instance != null && Instance != this){
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist across scenes
    }

    public override void OnNetworkSpawn(){
        base.OnNetworkSpawn();
        if(!IsServer){
            enabled = false; // Disable this script on clients
            return;
        }
    }

    public void StartHostHandler(){
        var netMan = NetworkManager.Singleton;

        //subscribe to all callbacks before starting the server
        netMan.OnClientConnectedCallback += OnClientConnected;
        netMan.OnClientDisconnectCallback += OnClientDisconnected;
        netMan.OnServerStopped += OnServerStopped;

        netMan.StartHost();
    }  
    
    private void OnClientConnected(ulong clientId){
        Debug.Log($"Client {clientId} connected");
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client)){
            NetworkObject playerObject = client.PlayerObject;
            connectedPlayers[clientId] = playerObject;
        }
    }

    private void OnClientDisconnected(ulong clientId){
        Debug.Log($"Client {clientId} disconnected");
        if (connectedPlayers.ContainsKey(clientId)){
            // Save player data before removal
            string playerId = DeterminePlayerId(clientId);
            NetworkObject playerObject = connectedPlayers[clientId];
            SaveLoadSystem.PlayerSaveData playerData = ExtractPlayerData(clientId, playerObject);
            SaveLoadSystem.SavePlayerData(playerId, playerData);
            connectedPlayers.Remove(clientId);
        }
    }

    private void OnServerStopped(bool isHost){
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        NetworkManager.Singleton.OnServerStopped -= OnServerStopped;
        foreach ((ulong clientId, NetworkObject playerObject) in connectedPlayers){
            string playerId = DeterminePlayerId(clientId);
            SaveLoadSystem.PlayerSaveData playerData = ExtractPlayerData(clientId, playerObject);
            SaveLoadSystem.SavePlayerData(playerId, playerData);
        }
        Debug.Log("Clearing all players from dictionaries");
        connectedPlayers.Clear();
    }

    private SaveLoadSystem.PlayerSaveData ExtractPlayerData(ulong clientId, NetworkObject playerObject){
        PlayerStats localPlayerStats = playerObject.GetComponent<PlayerStats>();

        SaveLoadSystem.PlayerSaveData playerSaveData = new SaveLoadSystem.PlayerSaveData{ 
            
            PlayerPosition = playerObject.GetComponent<PlayerMovement>().PlayerPosition.Value,

            // Network Stats
            Health = localPlayerStats.HealthStat.Stat.Value,
            Strength = localPlayerStats.StrengthStat.Stat.Value,
            Speed = localPlayerStats.SpeedStat.Stat.Value,
            Endurance = localPlayerStats.EnduranceStat.Stat.Value,
            SummoningCapacity = localPlayerStats.SummoningCapacityStat.Stat.Value,
            BindingAffinity = localPlayerStats.BindingAffinityStat.Stat.Value,
            Corruption = localPlayerStats.CorruptionStat.Stat.Value,

            MaxHealth = localPlayerStats.HealthStat.MaxStat.Value,
            MaxStrength = localPlayerStats.StrengthStat.MaxStat.Value,
            MaxSpeed = localPlayerStats.SpeedStat.MaxStat.Value,
            MaxEndurance = localPlayerStats.EnduranceStat.MaxStat.Value,
            MaxSummoningCapacity = localPlayerStats.SummoningCapacityStat.MaxStat.Value,
            MaxBindingAffinity = localPlayerStats.BindingAffinityStat.MaxStat.Value,
            MaxCorruption = localPlayerStats.CorruptionStat.MaxStat.Value,

        };
        return playerSaveData;
    }

    // temporarily just for two connections: host and client
    public string DeterminePlayerId(ulong clientID){
        if(clientID != NetworkManager.ServerClientId){
            return "client";
        }
        else{
            return "host";
        }
    }
}