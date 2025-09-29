using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

// This struct can be moved to its own file if you prefer
[Serializable]
public struct PoolConfigObject
{
    public GameObject Prefab;
    public int PrewarmCount;
}

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Singleton { get; private set; }

    // This list is now on the generic manager
    public List<PoolConfigObject> PooledPrefabsList;

    private Dictionary<GameObject, ObjectPool<GameObject>> m_PooledObjects = new Dictionary<GameObject, ObjectPool<GameObject>>();

    private void Awake()
    {
        if (Singleton != null && Singleton != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Singleton = this;
        }
        DontDestroyOnLoad(gameObject);

        // Initialize all pools at the start
        InitializePools();
    }

    private void InitializePools()
    {
        foreach (var configObject in PooledPrefabsList)
        {
            // The registration no longer needs network-specific code
            RegisterPrefab(configObject.Prefab, configObject.PrewarmCount);
        }
    }

    /// <summary>
    /// Registers a prefab with the pool, creating a new object pool for it.
    /// </summary>
    private void RegisterPrefab(GameObject prefab, int prewarmCount)
    {
        // Generic create function for GameObjects
        GameObject CreateFunc()
        {
            return Instantiate(prefab);
        }

        // Generic actions for GameObjects
        void ActionOnGet(GameObject obj)
        {
            obj.SetActive(true);
        }

        void ActionOnRelease(GameObject obj)
        {
            obj.SetActive(false);
        }

        void ActionOnDestroy(GameObject obj)
        {
            Destroy(obj);
        }

        // Create a new pool for this prefab
        m_PooledObjects[prefab] = new UnityEngine.Pool.ObjectPool<GameObject>(CreateFunc, ActionOnGet, ActionOnRelease, ActionOnDestroy, defaultCapacity: prewarmCount);

        // Pre-warm the pool by getting and immediately releasing the objects
        var prewarmList = new List<GameObject>();
        for (var i = 0; i < prewarmCount; i++)
        {
            prewarmList.Add(m_PooledObjects[prefab].Get());
        }
        foreach (var obj in prewarmList)
        {
            m_PooledObjects[prefab].Release(obj);
        }
    }

    /// <summary>
    /// Gets a GameObject instance from the pool.
    /// </summary>
    public GameObject GetObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!m_PooledObjects.ContainsKey(prefab))
        {
            Debug.LogError($"Pool for prefab '{prefab.name}' does not exist. Please add it to the PooledPrefabsList.");
            return null;
        }

        GameObject obj = m_PooledObjects[prefab].Get();
        obj.transform.SetPositionAndRotation(position, rotation);
        return obj;
    }

    /// <summary>
    /// Returns a GameObject instance to its pool.
    /// </summary>
    public void ReturnObject(GameObject obj, GameObject prefab)
    {
        if (!m_PooledObjects.ContainsKey(prefab))
        {
            Debug.LogWarning($"Trying to return an object for prefab '{prefab.name}' but no pool exists for it. Destroying instead.");
            Destroy(obj);
            return;
        }
        
        m_PooledObjects[prefab].Release(obj);
    }
}