using Unity.Netcode;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Goes on the Player Prefab. Handles the save/load "handshake" with the server.
/// Requires a PersistenceManager on the same object.
/// </summary>
[RequireComponent(typeof(PersistenceManager))]
public class PlayerSaveDataRelay : NetworkBehaviour
{
    private PersistenceManager _pManager;

    private void Awake()
    {
        _pManager = GetComponent<PersistenceManager>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (_pManager.PersistenceType != PersistenceManager.ePersistenceType.Player)
        {
            Debug.LogWarning($"PlayerSaveDataRelay is on an object '{name}' not marked as PersistenceType.Player!");
        }

        StartCoroutine(WaitForPlayerSaveManagerSpawn());
    }

    private IEnumerator WaitForPlayerSaveManagerSpawn()
    {
        while (!PlayerSaveManager.Instance.IsNetworkReady)
        {
            yield return null;
        }

        if (IsServer)
        {
            PlayerSaveManager.Instance.RegisterPlayer(OwnerClientId, _pManager);
        }

        if (IsOwner)
        {
            // The client requests its data from the server
            RequestPlayerDataServerRpc();
        }
    }

    /// <summary>
    /// A client sends this to the server to ask for its saved data.
    /// </summary>
    [ServerRpc]
    private void RequestPlayerDataServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        PlayerSaveManager.Instance.LoadAndSendDataToClient(rpcParams.Receive.SenderClientId);
    }

    /// <summary>
    /// The server sends this to a specific client, containing their data.
    /// </summary>
    [ClientRpc]
    public void ReceivePlayerDataClientRpc(string jsonPlayerData, ClientRpcParams clientRpcParams = default)
    {
        // Deserialize the data
        Dictionary<string, object> playerData = JsonConvert.DeserializeObject<Dictionary<string, object>>(
            jsonPlayerData,
            SaveLoadSystem.GetSerializerSettings()
        );

        // Apply it
        _pManager.ApplyAllData(playerData);
    }

}
