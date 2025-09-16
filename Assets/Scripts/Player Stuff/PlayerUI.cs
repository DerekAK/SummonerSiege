using System.Collections;
using Unity.Netcode;
using UnityEngine;

class PlayerUI : NetworkBehaviour
{
    [SerializeField] private GameObject screenUI;
    [SerializeField] private GameObject worldUI;

    private void Start()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            Initialize();
        }
    }
    
    public override void OnNetworkSpawn()
    {
        Initialize();
    }

    private void Initialize()
    {
        bool isNetworked = NetworkManager.Singleton != null && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer);

        if (isNetworked)
        {
            // Multiplayer logic
            if (IsOwner)
            {
                if(worldUI != null) Destroy(worldUI);
            }
            else
            {
                if(screenUI != null) Destroy(screenUI);
            }
        }
        else
        {
            // Single-player logic: We are always the "local player".
            // We don't need a world-space UI for others to see.
            if(worldUI != null) Destroy(worldUI);
        }
    }
}