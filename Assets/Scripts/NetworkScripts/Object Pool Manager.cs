using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Singleton { get; private set; }

    // This list is now on the generic manager
    public List<PoolConfigObject> PooledPrefabsList;

    private Dictionary<GameObject, (ObjectPool<GameObject> pool, Transform parent)> m_PooledObjects = new();


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
    public void RegisterPrefab(GameObject prefab, int prewarmCount)
    {
        // Create scene hierarchy
        GameObject category = new GameObject(prefab.name);
        category.transform.SetParent(transform);

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
            #if UNITY_EDITOR
                DestroyImmediate(obj);
            #else
                Destroy(obj);
            #endif
        }


        // Create a new pool for this prefab
        ObjectPool<GameObject> newPool = new(CreateFunc, ActionOnGet, ActionOnRelease, ActionOnDestroy, defaultCapacity: prewarmCount);
        m_PooledObjects[prefab] = (newPool, category.transform);

        // Pre-warm the pool by getting and immediately releasing the objects
        var prewarmList = new List<GameObject>();
        for (var i = 0; i < prewarmCount; i++)
        {
            GameObject pooledObject = newPool.Get();
            prewarmList.Add(pooledObject);
            pooledObject.transform.SetParent(category.transform);
        }
        foreach (var obj in prewarmList)
        {
            newPool.Release(obj);
        }
    }

    /// <summary>
    /// Gets a GameObject instance from the pool.
    /// </summary>
    public GameObject GetObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!m_PooledObjects.TryGetValue(prefab, out var poolInfo))
        {
            Debug.LogError($"Pool for prefab '{prefab.name}' does not exist.");
            return null;
        }

        GameObject obj = poolInfo.pool.Get();
        obj.transform.SetPositionAndRotation(position, rotation);
        return obj;
    }

    /// <summary>
    /// Returns a GameObject instance to its pool.
    /// </summary>
    public void ReturnObject(GameObject obj, GameObject prefab)
    {
        if (!m_PooledObjects.TryGetValue(prefab, out var poolInfo))
        {
            Debug.LogWarning($"Trying to return object for prefab '{prefab.name}' but no pool exists. Destroying instead.");
            Destroy(obj);
            return;
        }

        obj.transform.SetParent(poolInfo.parent);
        
        poolInfo.pool.Release(obj);
    }
}
[Serializable]
public struct PoolConfigObject
{
    public GameObject Prefab;
    public int PrewarmCount;
}