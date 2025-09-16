using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class PlayerStats : NetworkStats
{
    public PlayerStatsConfiguration playerStatsSO;
    private NetworkVariable<float> healthStat = new NetworkVariable<float>();
    private NetworkVariable<float> maxHealthStat = new NetworkVariable<float>();
    private NetworkVariable<float> strengthStat = new NetworkVariable<float>();
    private NetworkVariable<float> maxStrengthStat = new NetworkVariable<float>();
    private NetworkVariable<float> speedStat = new NetworkVariable<float>();
    private NetworkVariable<float> maxSpeedStat = new NetworkVariable<float>();
    private NetworkVariable<float> enduranceStat = new NetworkVariable<float>();
    private NetworkVariable<float> maxEnduranceStat = new NetworkVariable<float>();
    private NetworkVariable<float> summoningCapacityStat = new NetworkVariable<float>();
    private NetworkVariable<float> maxSummoningCapacityStat = new NetworkVariable<float>();
    private NetworkVariable<float> bindingAffinityStat = new NetworkVariable<float>();
    private NetworkVariable<float> maxBindingAffinityStat = new NetworkVariable<float>();
    private NetworkVariable<float> corruptionStat = new NetworkVariable<float>();
    private NetworkVariable<float> maxCorruptionStat = new NetworkVariable<float>();

    public NetworkStat StrengthStat;
    public NetworkStat SpeedStat;
    public NetworkStat EnduranceStat;
    public NetworkStat SummoningCapacityStat;
    public NetworkStat BindingAffinityStat;
    public NetworkStat CorruptionStat;

    private void Awake()
    {
        HealthStat = new NetworkStat(healthStat, maxHealthStat);
        StrengthStat = new NetworkStat(strengthStat, maxStrengthStat);
        SpeedStat = new NetworkStat(speedStat, maxSpeedStat);
        EnduranceStat = new NetworkStat(enduranceStat, maxEnduranceStat);
        SummoningCapacityStat = new NetworkStat(summoningCapacityStat, maxSummoningCapacityStat);
        BindingAffinityStat = new NetworkStat(bindingAffinityStat, maxBindingAffinityStat);
        CorruptionStat = new NetworkStat(corruptionStat, maxCorruptionStat);
    }

    private void Start()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            LoadStatsForSinglePlayer();
        }
    }

    public override void OnNetworkSpawn()
    {
        // ENTRY POINT 2: MULTIPLAYER
        base.OnNetworkSpawn();
        if (!IsOwner) return; // Only the owner should request/load their stats.

        if (IsServer) // This also covers the Host case
        {
            ApplyLoadedData(NetworkManager.Singleton.LocalClientId, 
                          SaveLoadSystem.LoadPlayerData(NetworkHandler.Instance.DeterminePlayerId(NetworkManager.Singleton.LocalClientId)));
        }
        else // This is for clients
        {
            RequestStatsServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    private void LoadStatsForSinglePlayer()
    {
        Debug.Log("Loading stats for single-player mode...");
        
        // For single-player, we need a default profile ID to load.
        string singlePlayerProfileId = "singleplayer_profile"; // You can change this
        SaveLoadSystem.PlayerSaveData playerSaveData = SaveLoadSystem.LoadPlayerData(singlePlayerProfileId);

        if (!playerSaveData.Equals(default(SaveLoadSystem.PlayerSaveData)))
        {
            Debug.Log("Single-player stats loaded from save file.");
            // Apply saved data directly
            ApplyStatsFromSave(playerSaveData);
            GetComponent<PlayerMovement>().PlayerPosition.Value = playerSaveData.PlayerPosition;
        }
        else
        {
            Debug.Log("Single-player stats loaded from configuration data (ScriptableObject).");
            // Apply default SO data
            ApplyStatsFromConfiguration(playerStatsSO);
            GetComponent<PlayerMovement>().PlayerPosition.Value = transform.position; // Use current position
        }
    }

    // Helper method to reduce code duplication
    private void ApplyStatsFromSave(SaveLoadSystem.PlayerSaveData playerSaveData)
    {
        HealthStat.Stat.Value = playerSaveData.Health;
        HealthStat.MaxStat.Value = playerSaveData.MaxHealth;
        StrengthStat.Stat.Value = playerSaveData.Strength;
        StrengthStat.MaxStat.Value = playerSaveData.MaxStrength;
        SpeedStat.Stat.Value = playerSaveData.Speed;
        SpeedStat.MaxStat.Value = playerSaveData.MaxSpeed;
        EnduranceStat.Stat.Value = playerSaveData.Endurance;
        EnduranceStat.MaxStat.Value = playerSaveData.MaxEndurance;
        SummoningCapacityStat.Stat.Value = playerSaveData.SummoningCapacity;
        SummoningCapacityStat.MaxStat.Value = playerSaveData.MaxSummoningCapacity;
        BindingAffinityStat.Stat.Value = playerSaveData.BindingAffinity;
        BindingAffinityStat.MaxStat.Value = playerSaveData.MaxBindingAffinity;
        CorruptionStat.Stat.Value = playerSaveData.Corruption;
        CorruptionStat.MaxStat.Value = playerSaveData.MaxCorruption;
    }

    private void ApplyStatsFromConfiguration(PlayerStatsConfiguration playerStatsSO) {
        HealthStat.Stat.Value = playerStatsSO.Health;
        HealthStat.MaxStat.Value = playerStatsSO.Health;
        StrengthStat.Stat.Value = playerStatsSO.Strength;
        StrengthStat.MaxStat.Value = playerStatsSO.Strength;
        SpeedStat.Stat.Value = playerStatsSO.Speed;
        SpeedStat.MaxStat.Value = playerStatsSO.Speed;
        EnduranceStat.Stat.Value = playerStatsSO.Endurance;
        EnduranceStat.MaxStat.Value = playerStatsSO.Endurance;
        SummoningCapacityStat.Stat.Value = playerStatsSO.SummoningCapacity;
        SummoningCapacityStat.MaxStat.Value = playerStatsSO.SummoningCapacity;
        BindingAffinityStat.Stat.Value = playerStatsSO.BindingAffinity;
        BindingAffinityStat.MaxStat.Value = playerStatsSO.BindingAffinity;
        CorruptionStat.Stat.Value = playerStatsSO.Corruption;
        CorruptionStat.MaxStat.Value = playerStatsSO.Corruption;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestStatsServerRpc(ulong clientId)
    {
        SaveLoadSystem.PlayerSaveData playerData = SaveLoadSystem.LoadPlayerData(NetworkHandler.Instance.DeterminePlayerId(clientId));
        ApplyLoadedData(clientId, playerData);
    }

    private void ApplyLoadedData(ulong clientId, SaveLoadSystem.PlayerSaveData playerSaveData)
    {
        if (!IsServer) { return; }
        
        Vector3 position;

        if (!playerSaveData.Equals(default(SaveLoadSystem.PlayerSaveData)))
        {
            Debug.Log("Applying stats from save!");
            position = playerSaveData.PlayerPosition;
            ApplyStatsFromSave(playerSaveData);
        }
        else
        {
            Debug.Log("Applying stats from configuration!");
            position = default;
            ApplyStatsFromConfiguration(playerStatsSO);
        }

        OwnerAuthoritativeData playerData = new OwnerAuthoritativeData { Position = position };
        ClientRpcParams rpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } } };
        SendOwnerAuthoritativeDataClientRpc(playerData, rpcParams);
    }

    [ClientRpc]
    private void SendOwnerAuthoritativeDataClientRpc(OwnerAuthoritativeData data, ClientRpcParams rpcParams)
    {
        StartCoroutine(TrySetPlayerPosition(data, rpcParams));
    }

    private IEnumerator TrySetPlayerPosition(OwnerAuthoritativeData data, ClientRpcParams rpcParams)
    {
        // Wait until the player object is available
        NetworkObject playerObject = null;
        while (playerObject == null)
        {
             if (NetworkManager.Singleton.LocalClient != null)
             {
                 playerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
             }
             yield return null; // Wait one frame
        }

        if (playerObject != null)
        {
            playerObject.GetComponent<PlayerMovement>().PlayerPosition.Value = data.Position;
        }
        else
        {
            Debug.LogError($"Failed to find PlayerObject for client {NetworkManager.Singleton.LocalClientId} after retry!");
        }
    }

    public struct OwnerAuthoritativeData : INetworkSerializable
    {
        public Vector3 Position;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Position);
        }
    }
}