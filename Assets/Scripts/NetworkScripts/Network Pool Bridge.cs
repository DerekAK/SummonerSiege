using Unity.Netcode;
using UnityEngine;

public class NetworkPoolBridge : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        // Hook into the generic ObjectPoolManager
        var poolManager = ObjectPoolManager.Singleton;
        if (poolManager == null)
        {
            Debug.LogError("NetworkPoolBridge requires an ObjectPoolManager in the scene.");
            return;
        }

        // For every prefab in the manager's list...
        foreach (var config in poolManager.PooledPrefabsList)
        {
            var prefab = config.Prefab;

            // ...if it's a networked object, register a spawn handler for it.
            if (prefab != null && prefab.GetComponent<NetworkObject>() != null)
            {
                NetworkManager.Singleton.PrefabHandler.AddHandler(prefab, new PooledPrefabInstanceHandler(prefab, poolManager));
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        // Clean up the handlers when the network session ends
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.PrefabHandler != null)
        {
            foreach (var config in ObjectPoolManager.Singleton.PooledPrefabsList)
            {
                if (config.Prefab != null && config.Prefab.GetComponent<NetworkObject>() != null)
                {
                    NetworkManager.Singleton.PrefabHandler.RemoveHandler(config.Prefab);
                }
            }
        }
    }
}
public class PooledPrefabInstanceHandler : INetworkPrefabInstanceHandler
{
    private GameObject m_Prefab;
    private ObjectPoolManager m_PoolManager;

    public PooledPrefabInstanceHandler(GameObject prefab, ObjectPoolManager poolManager)
    {
        m_Prefab = prefab;
        m_PoolManager = poolManager;
    }

    // Called when Netcode needs to create a new instance
    public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        // Get a generic GameObject from the pool
        GameObject obj = m_PoolManager.GetObject(m_Prefab, position, rotation);
        
        // Return its NetworkObject component
        return obj.GetComponent<NetworkObject>();
    }

    // Called when Netcode needs to destroy an instance
    public void Destroy(NetworkObject networkObject)
    {
        // Return the GameObject to the generic pool
        m_PoolManager.ReturnObject(networkObject.gameObject, m_Prefab);
    }
}