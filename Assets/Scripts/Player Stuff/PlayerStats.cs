using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class PlayerStats : NetworkStats{
    public PlayerStatsConfiguration playerStatsSO;

    // Declare NetworkVariables directly in PlayerStats
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

    // NetworkStat instances using the above NetworkVariables
    public NetworkStat StrengthStat;
    public NetworkStat SpeedStat;
    public NetworkStat EnduranceStat;
    public NetworkStat SummoningCapacityStat;
    public NetworkStat BindingAffinityStat;
    public NetworkStat CorruptionStat;

    private void Awake(){
        HealthStat = new NetworkStat(healthStat, maxHealthStat);
        StrengthStat = new NetworkStat(strengthStat, maxStrengthStat);
        SpeedStat = new NetworkStat(speedStat, maxSpeedStat);
        EnduranceStat = new NetworkStat(enduranceStat, maxEnduranceStat);
        SummoningCapacityStat = new NetworkStat(summoningCapacityStat, maxSummoningCapacityStat);
        BindingAffinityStat = new NetworkStat(bindingAffinityStat, maxBindingAffinityStat);
        CorruptionStat = new NetworkStat(corruptionStat, maxCorruptionStat);
    }

    public override void OnNetworkSpawn(){
        base.OnNetworkSpawn();

        if (IsServer && IsLocalPlayer){
            ApplyLoadedData(NetworkManager.Singleton.LocalClientId, 
                          SaveLoadSystem.LoadPlayerData(NetworkHandler.Instance.DeterminePlayerId(NetworkManager.Singleton.LocalClientId)));
        }
        else if (IsLocalPlayer){
            RequestStatsServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestStatsServerRpc(ulong clientId)
    {
        SaveLoadSystem.PlayerSaveData playerData = SaveLoadSystem.LoadPlayerData(NetworkHandler.Instance.DeterminePlayerId(clientId));
        ApplyLoadedData(clientId, playerData);
    }

    private void ApplyLoadedData(ulong clientId, SaveLoadSystem.PlayerSaveData playerSaveData){
        if (!IsServer) { return; }

        Debug.Log($"Applying data for client {clientId}. IsSpawned: {NetworkObject.IsSpawned}");
        Vector3 position;

        if (!playerSaveData.Equals(default(SaveLoadSystem.PlayerSaveData))){
            Debug.Log($"Client {clientId} received stats from save file");
            position = playerSaveData.PlayerPosition;

            HealthStat.Stat.Value = playerSaveData.Health;
            StrengthStat.Stat.Value = playerSaveData.Strength;
            SpeedStat.Stat.Value = playerSaveData.Speed;
            EnduranceStat.Stat.Value = playerSaveData.Endurance;
            SummoningCapacityStat.Stat.Value = playerSaveData.SummoningCapacity;
            BindingAffinityStat.Stat.Value = playerSaveData.BindingAffinity;
            CorruptionStat.Stat.Value = playerSaveData.Corruption;

            HealthStat.MaxStat.Value = playerSaveData.MaxHealth;
            StrengthStat.MaxStat.Value = playerSaveData.MaxStrength;
            SpeedStat.MaxStat.Value = playerSaveData.MaxSpeed;
            EnduranceStat.MaxStat.Value = playerSaveData.MaxEndurance;
            SummoningCapacityStat.MaxStat.Value = playerSaveData.MaxSummoningCapacity;
            BindingAffinityStat.MaxStat.Value = playerSaveData.MaxBindingAffinity;
            CorruptionStat.MaxStat.Value = playerSaveData.MaxCorruption;
        }
        else{
            Debug.Log($"Client {clientId} received stats from configuration data");
            position = default;

            HealthStat.Stat.Value = playerStatsSO.Health;
            StrengthStat.Stat.Value = playerStatsSO.Strength;
            SpeedStat.Stat.Value = playerStatsSO.Speed;
            EnduranceStat.Stat.Value = playerStatsSO.Endurance;
            SummoningCapacityStat.Stat.Value = playerStatsSO.SummoningCapacity;
            BindingAffinityStat.Stat.Value = playerStatsSO.BindingAffinity;
            CorruptionStat.Stat.Value = playerStatsSO.Corruption;

            HealthStat.MaxStat.Value = playerStatsSO.Health;
            StrengthStat.MaxStat.Value = playerStatsSO.Strength;
            SpeedStat.MaxStat.Value = playerStatsSO.Speed;
            EnduranceStat.MaxStat.Value = playerStatsSO.Endurance;
            SummoningCapacityStat.MaxStat.Value = playerStatsSO.SummoningCapacity;
            BindingAffinityStat.MaxStat.Value = playerStatsSO.BindingAffinity;
            CorruptionStat.MaxStat.Value = playerStatsSO.Corruption;
        }

        OwnerAuthoritativeData playerData = new OwnerAuthoritativeData { Position = position };
        ClientRpcParams rpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } } };
        SendOwnerAuthoritativeDataClientRpc(playerData, rpcParams);
    }

    [ClientRpc]
    private void SendOwnerAuthoritativeDataClientRpc(OwnerAuthoritativeData data, ClientRpcParams rpcParams){
        Debug.Log($"ClientRpc called for client {NetworkManager.Singleton.LocalClientId}. LocalClient null: {NetworkManager.Singleton.LocalClient == null}");

        // Wait until the player object is available
        NetworkObject playerObject = null;
        if (NetworkManager.Singleton.LocalClient != null){
            playerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
        }

        if (playerObject != null){
            Debug.Log($"Player object found for client {NetworkManager.Singleton.LocalClientId}. Setting position to {data.Position}");
            playerObject.GetComponent<PlayerMovement>().PlayerPosition.Value = data.Position;
        }
        else{
            Debug.LogWarning($"PlayerObject is null for client {NetworkManager.Singleton.LocalClientId}. Retrying in next frame...");
            // Retry in the next frame if the player object isn’t ready yet
            StartCoroutine(TrySetPlayerPosition(data, rpcParams));
        }
    }

    private IEnumerator TrySetPlayerPosition(OwnerAuthoritativeData data, ClientRpcParams rpcParams){
        yield return null; // Wait one frame

        NetworkObject playerObject = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (playerObject != null){
            Debug.Log($"Retry succeeded for client {NetworkManager.Singleton.LocalClientId}. Setting position to {data.Position}");
            playerObject.GetComponent<PlayerMovement>().PlayerPosition.Value = data.Position;
        }
        else{
            Debug.LogError($"Failed to find PlayerObject for client {NetworkManager.Singleton.LocalClientId} after retry!");
        }
    }

    public struct OwnerAuthoritativeData : INetworkSerializable{
        public Vector3 Position;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter{
            serializer.SerializeValue(ref Position);
        }
    }
}