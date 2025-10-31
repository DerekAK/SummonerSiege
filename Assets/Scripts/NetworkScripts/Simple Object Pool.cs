using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

/// <summary>
/// Centralized pool manager for non-networked GameObjects using UnityEngine.Pool
/// Manages multiple object types from a single component
/// </summary>
public class SimpleObjectPool : MonoBehaviour
{
    [System.Serializable]
    public class PoolSettings
    {
        public GameObject prefab;
        public int defaultCapacity = 10;
        public int maxSize = 100;
    }

    [SerializeField] private List<PoolSettings> poolSettings = new List<PoolSettings>();
    [SerializeField] private bool collectionCheck = true;

    private Dictionary<GameObject, ObjectPool<GameObject>> pools = new Dictionary<GameObject, ObjectPool<GameObject>>();
    private Dictionary<GameObject, GameObject> objectToPrefabMap = new Dictionary<GameObject, GameObject>();
    private Dictionary<GameObject, int> activeCounts = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, int> inactiveCounts = new Dictionary<GameObject, int>();

    public static SimpleObjectPool Singleton;

    private void Awake()
    {
        if (Singleton != null && Singleton != this)
        {
            Destroy(gameObject);
            return;
        }
        Singleton = this;

        InitializePools();
    }

    private void InitializePools()
    {
        foreach (var settings in poolSettings)
        {
            if (settings.prefab == null)
            {
                Debug.LogWarning("PoolManager: Null prefab found in pool settings. Skipping.");
                continue;
            }

            CreatePool(settings.prefab, settings.defaultCapacity, settings.maxSize);
        }
    }

    /// <summary>
    /// Create a new pool for a prefab at runtime
    /// </summary>
    /// <param name="prefab">The prefab to pool</param>
    /// <param name="defaultCapacity">Initial capacity (also used for prewarming)</param>
    /// <param name="maxSize">Maximum pool size</param>
    public void CreatePool(GameObject prefab, int defaultCapacity = 10, int maxSize = 100)
    {
        if (prefab == null)
        {
            Debug.LogError("PoolManager: Cannot create pool for null prefab.");
            return;
        }

        if (pools.ContainsKey(prefab))
        {
            Debug.LogWarning($"PoolManager: Pool for {prefab.name} already exists.");
            return;
        }

        var pool = new ObjectPool<GameObject>(
            createFunc: () => CreatePooledObject(prefab),
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
        if (defaultCapacity > 0)
        {
            List<GameObject> prewarmed = new List<GameObject>(defaultCapacity);
        
            // Get objects from pool (this creates them)
            for (int i = 0; i < defaultCapacity; i++)
            {
                prewarmed.Add(pools[prefab].Get());
            }
            
            // Release them all back to the pool
            foreach (var obj in prewarmed)
            {
                pools[prefab].Release(obj);
            }
        }

        Debug.Log($"PoolManager: Created pool for {prefab.name} (Capacity: {defaultCapacity}, Max: {maxSize}!");
    }

    private GameObject CreatePooledObject(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        objectToPrefabMap[obj] = prefab;
        
        inactiveCounts[prefab]++;
        
        return obj;
    }

    private void OnGetFromPool(GameObject obj, GameObject prefab)
    {
        obj.SetActive(true);
        activeCounts[prefab]++;
        inactiveCounts[prefab]--;
    }

    private void OnReleaseToPool(GameObject obj, GameObject prefab)
    {
        obj.SetActive(false);
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        
        activeCounts[prefab]--;
        inactiveCounts[prefab]++;
    }

    private void OnDestroyPoolObject(GameObject obj, GameObject prefab)
    {
        if (objectToPrefabMap.ContainsKey(obj))
        {
            objectToPrefabMap.Remove(obj);
        }
        
        inactiveCounts[prefab]--;
        Destroy(obj);
    }


    /// <summary>
    /// Get an object from the pool by prefab reference
    /// </summary>
    public GameObject GetObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogError("PoolManager: Cannot get object for null prefab.");
            return null;
        }

        if (!pools.ContainsKey(prefab))
        {
            Debug.LogWarning($"PoolManager: No pool exists for {prefab.name}. Creating one now.");
            CreatePool(prefab);
        }

        GameObject obj = pools[prefab].Get();
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        
        return obj;
    }

    /// <summary>
    /// Return an object to its pool
    /// </summary>
    public void ReturnObject(GameObject obj, GameObject prefab)
    {
        if (obj == null)
        {
            Debug.LogWarning("PoolManager: Attempted to return null object.");
            return;
        }

        if (prefab == null)
        {
            Debug.LogError("PoolManager: Cannot return object without prefab reference.");
            return;
        }

        if (!pools.ContainsKey(prefab))
        {
            Debug.LogError($"PoolManager: No pool exists for {prefab.name}. Cannot return object.");
            Destroy(obj);
            return;
        }

        pools[prefab].Release(obj);
    }

    /// <summary>
    /// Return an object to its pool (automatically determines which pool from the instance)
    /// </summary>
    public void ReturnObject(GameObject obj)
    {
        if (obj == null)
        {
            Debug.LogWarning("PoolManager: Attempted to return null object.");
            return;
        }

        if (objectToPrefabMap.TryGetValue(obj, out GameObject prefab))
        {
            ReturnObject(obj, prefab);
        }
        else
        {
            Debug.LogError($"PoolManager: Object {obj.name} is not tracked by any pool. Cannot return.");
        }
    }

    /// <summary>
    /// Clear a specific pool
    /// </summary>
    public void ClearPool(GameObject prefab)
    {
        if (prefab == null || !pools.ContainsKey(prefab))
        {
            Debug.LogWarning($"PoolManager: Cannot clear pool for {(prefab != null ? prefab.name : "null")}. Pool does not exist.");
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
    public void GetPoolStats(GameObject prefab, out int active, out int inactive, out int total)
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
    public bool HasPool(GameObject prefab)
    {
        return prefab != null && pools.ContainsKey(prefab);
    }


    private void OnDestroy()
    {
        ClearAllPools();
    }
}