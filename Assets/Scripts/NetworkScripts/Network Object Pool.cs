using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

/// <summary>
/// Centralized pool manager for Unity Netcode NetworkObjects using UnityEngine.Pool
/// Manages multiple network object types from a single component
/// </summary>
public class NetworkObjectPool : MonoBehaviour
{
    [System.Serializable]
    public class PoolSettings
    {
        public NetworkObject prefab;
        public int defaultCapacity = 10;
        public int maxSize = 100;
    }

    [SerializeField] private List<PoolSettings> poolSettings = new List<PoolSettings>();
    [SerializeField] private bool collectionCheck = true;

    private Dictionary<NetworkObject, ObjectPool<NetworkObject>> pools = new Dictionary<NetworkObject, ObjectPool<NetworkObject>>();
    private Dictionary<NetworkObject, NetworkObject> objectToPrefabMap = new Dictionary<NetworkObject, NetworkObject>();
    private Dictionary<NetworkObject, int> activeCounts = new Dictionary<NetworkObject, int>();
    private Dictionary<NetworkObject, int> inactiveCounts = new Dictionary<NetworkObject, int>();
    private bool isInitialized = false;

    public static NetworkObjectPool Singleton;

    private void Awake()
    {
        if (Singleton != null && Singleton != this)
        {
            Destroy(gameObject);
            return;
        }
        Singleton = this;
    }

    private void Start()
    {
        // Subscribe to NetworkManager events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        }
        else
        {
            Debug.LogWarning("NetworkPoolManager: NetworkManager.Singleton is null. Pools will not be initialized automatically.");
        }
    }

    private void OnServerStarted()
    {
        // Initialize pools after the server has fully started and completed scene sweep
        if (!isInitialized)
        {
            InitializePools();
            isInitialized = true;
            // Debug.Log("NetworkPoolManager: Initialized pools after server started.");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
        
        ClearAllPools();
    }

    private void InitializePools()
    {
        foreach (var settings in poolSettings)
        {
            if (settings.prefab == null)
            {
                Debug.LogWarning("NetworkPoolManager: Null prefab found in pool settings. Skipping.");
                continue;
            }

            CreatePool(settings.prefab, settings.defaultCapacity, settings.maxSize, prewarm: true);
        }
    }

    /// <summary>
    /// Create a new pool for a NetworkObject prefab at runtime
    /// </summary>
    /// <param name="prefab">The NetworkObject prefab to pool</param>
    /// <param name="defaultCapacity">Initial capacity (also used for prewarming)</param>
    /// <param name="maxSize">Maximum pool size</param>
    /// <param name="prewarm">If true, creates defaultCapacity objects immediately</param>
    public void CreatePool(NetworkObject prefab, int defaultCapacity = 10, int maxSize = 100, bool prewarm = true)
    {
        if (prefab == null)
        {
            Debug.LogError("NetworkPoolManager: Cannot create pool for null prefab.");
            return;
        }

        if (pools.ContainsKey(prefab))
        {
            Debug.LogWarning($"NetworkPoolManager: Pool for {prefab.name} already exists.");
            return;
        }

        // Auto-initialize if creating pools at runtime
        if (!isInitialized)
        {
            isInitialized = true;
        }

        var pool = new ObjectPool<NetworkObject>(
            createFunc: () => CreatePooledNetworkObject(prefab),
            actionOnGet: (obj) => OnGetFromPool(obj, prefab),
            actionOnRelease: (obj) => OnReleaseToPool(obj, prefab),
            actionOnDestroy: (obj) => OnDestroyPoolObject(obj, prefab),
            collectionCheck: collectionCheck,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );

        pools.Add(prefab, pool);
        activeCounts[prefab] = 0;
        inactiveCounts[prefab] = 0;

        // Prewarm the pool by getting and releasing objects
        if (prewarm && defaultCapacity > 0)
        {
            PrewarmPool(prefab, defaultCapacity);
        }

        // Debug.Log($"NetworkPoolManager: Created pool for {prefab.name} (Capacity: {defaultCapacity}, Max: {maxSize}, Prewarmed: {prewarm})");
    }

    private NetworkObject CreatePooledNetworkObject(NetworkObject prefab)
    {
        // Don't parent to the pool manager - leave unparented to avoid network parenting conflicts
        NetworkObject obj = Instantiate(prefab);
        
        // CRITICAL: Deactivate immediately to prevent NetworkManager from trying to spawn it
        obj.gameObject.SetActive(false);
        
        objectToPrefabMap[obj] = prefab;
        
        inactiveCounts[prefab]++;
        
        return obj;
    }

    private void OnGetFromPool(NetworkObject obj, NetworkObject prefab)
    {
        obj.gameObject.SetActive(true);
        activeCounts[prefab]++;
        inactiveCounts[prefab]--;
    }

    private void OnReleaseToPool(NetworkObject obj, NetworkObject prefab)
    {
        activeCounts.Remove(obj);
        
        // Only despawn if it's currently spawned (must happen before reparenting)
        if (obj.IsSpawned)
        {
            obj.Despawn(false);
        }

        obj.gameObject.SetActive(false);
        
        // Don't reparent - just reset transform to keep pooled objects organized
        // Reparenting NetworkObjects causes issues with network parenting system
        obj.transform.position = Vector3.zero;
        obj.transform.rotation = Quaternion.identity;
        
        activeCounts[prefab]--;
        inactiveCounts[prefab]++;
    }

    private void OnDestroyPoolObject(NetworkObject obj, NetworkObject prefab)
    {
        if (objectToPrefabMap.ContainsKey(obj))
        {
            objectToPrefabMap.Remove(obj);
        }
        
        if (obj != null && obj.gameObject != null)
        {
            inactiveCounts[prefab]--;
            Destroy(obj.gameObject);
        }
    }

    /// <summary>
    /// Prewarm a pool by creating the specified number of objects
    /// Note: NetworkObjects are NOT spawned during prewarming
    /// </summary>
    private void PrewarmPool(NetworkObject prefab, int count)
    {
        if (!pools.ContainsKey(prefab))
        {
            Debug.LogError($"NetworkPoolManager: Cannot prewarm pool for {prefab.name}. Pool does not exist.");
            return;
        }

        List<NetworkObject> tempList = new List<NetworkObject>(count);
        
        // Get objects from pool (this creates them but doesn't spawn them)
        for (int i = 0; i < count; i++)
        {
            tempList.Add(pools[prefab].Get());
        }
        
        // Release them all back to the pool
        foreach (var obj in tempList)
        {
            pools[prefab].Release(obj);
        }

        // Debug.Log($"NetworkPoolManager: Prewarmed {count} objects for {prefab.name}");
    }

    /// <summary>
    /// Get and spawn a NetworkObject from the pool by prefab reference
    /// Server only
    /// </summary>
    public NetworkObject GetObject(NetworkObject prefab, Vector3 position, Quaternion rotation, bool destroyWithScene = false)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogError("NetworkPoolManager: GetObject can only be called on the server!");
            return null;
        }

        if (prefab == null)
        {
            Debug.LogError("NetworkPoolManager: Cannot get object for null prefab.");
            return null;
        }

        if (!pools.ContainsKey(prefab))
        {
            Debug.LogWarning($"NetworkPoolManager: No pool exists for {prefab.name}. Creating one now.");
            CreatePool(prefab);
        }

        // Get from pool - this activates the GameObject via OnGetFromPool
        NetworkObject obj = pools[prefab].Get();
        
        // Set position and rotation after activation
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        
        // Now spawn the network object (GameObject is already active)
        obj.Spawn(destroyWithScene);
        
        return obj;
    }

    /// <summary>
    /// Get and spawn a NetworkObject from the pool with a specific parent
    /// Server only
    /// </summary>
    public NetworkObject GetObject(NetworkObject prefab, Vector3 position, Quaternion rotation, Transform parent, bool destroyWithScene = false)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogError("NetworkPoolManager: GetObject can only be called on the server!");
            return null;
        }

        if (prefab == null)
        {
            Debug.LogError("NetworkPoolManager: Cannot get object for null prefab.");
            return null;
        }

        if (!pools.ContainsKey(prefab))
        {
            Debug.LogWarning($"NetworkPoolManager: No pool exists for {prefab.name}. Creating one now.");
            CreatePool(prefab);
        }

        // Get from pool - this activates the GameObject via OnGetFromPool
        NetworkObject obj = pools[prefab].Get();
        
        // Set position and rotation (but NOT parent yet)
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        
        // Spawn the network object FIRST (GameObject is already active)
        obj.Spawn(destroyWithScene);
        
        // THEN set parent (NetworkObjects can only be reparented after spawning)
        if (parent != null)
        {
            obj.transform.SetParent(parent, true);
        }
        
        return obj;
    }

    /// <summary>
    /// Return a NetworkObject to its pool
    /// Server only
    /// </summary>
    public void ReturnObject(NetworkObject obj, NetworkObject prefab)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogError("NetworkPoolManager: ReturnObject can only be called on the server!");
            return;
        }

        if (obj == null)
        {
            Debug.LogWarning("NetworkPoolManager: Attempted to return null object.");
            return;
        }

        if (prefab == null)
        {
            Debug.LogError("NetworkPoolManager: Cannot return object without prefab reference.");
            return;
        }

        if (!pools.ContainsKey(prefab))
        {
            Debug.LogError($"NetworkPoolManager: No pool exists for {prefab.name}. Cannot return object.");
            
            // If it's spawned, despawn it
            if (obj.IsSpawned)
            {
                obj.Despawn();
            }
            return;
        }

        pools[prefab].Release(obj);
    }

    /// <summary>
    /// Return a NetworkObject to its pool (automatically determines which pool from the instance)
    /// Server only
    /// </summary>
    public void ReturnObject(NetworkObject obj)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogError("NetworkPoolManager: ReturnObject can only be called on the server!");
            return;
        }

        if (obj == null)
        {
            Debug.LogWarning("NetworkPoolManager: Attempted to return null object.");
            return;
        }

        if (objectToPrefabMap.TryGetValue(obj, out NetworkObject prefab))
        {
            ReturnObject(obj, prefab);
        }
        else
        {
            Debug.LogError($"NetworkPoolManager: NetworkObject {obj.name} is not tracked by any pool. Cannot return.");
            
            // If it's spawned, despawn it
            if (obj.IsSpawned)
            {
                obj.Despawn();
            }
        }
    }

    /// <summary>
    /// Clear a specific pool
    /// </summary>
    public void ClearPool(NetworkObject prefab)
    {
        if (prefab == null || !pools.ContainsKey(prefab))
        {
            Debug.LogWarning($"NetworkPoolManager: Cannot clear pool for {(prefab != null ? prefab.name : "null")}. Pool does not exist.");
            return;
        }

        pools[prefab].Clear();
        activeCounts[prefab] = 0;
        inactiveCounts[prefab] = 0;
    }

    /// <summary>
    /// Clear all pools
    /// </summary>
    public void ClearAllPools()
    {
        foreach (var pool in pools.Values)
        {
            pool.Clear();
        }
        
        activeCounts.Clear();
        inactiveCounts.Clear();
        objectToPrefabMap.Clear();
    }

    /// <summary>
    /// Get statistics for a specific pool
    /// </summary>
    public void GetPoolStats(NetworkObject prefab, out int active, out int inactive, out int total)
    {
        if (prefab != null && pools.ContainsKey(prefab))
        {
            active = activeCounts[prefab];
            inactive = inactiveCounts[prefab];
            total = active + inactive;
        }
        else
        {
            active = inactive = total = 0;
        }
    }

    /// <summary>
    /// Check if a pool exists for a prefab
    /// </summary>
    public bool HasPool(NetworkObject prefab)
    {
        return prefab != null && pools.ContainsKey(prefab);
    }

    /// <summary>
    /// Manually prewarm a pool with additional objects
    /// Note: NetworkObjects are NOT spawned during prewarming
    /// </summary>
    public void Prewarm(NetworkObject prefab, int count)
    {
        if (prefab == null)
        {
            Debug.LogError("NetworkPoolManager: Cannot prewarm null prefab.");
            return;
        }

        if (!pools.ContainsKey(prefab))
        {
            Debug.LogWarning($"NetworkPoolManager: No pool exists for {prefab.name}. Creating one first.");
            CreatePool(prefab, count, count * 2, prewarm: true);
            return;
        }

        PrewarmPool(prefab, count);
    }
}