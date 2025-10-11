using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using System;

public class PersistenceManager : NetworkBehaviour
{
    private List<IPersistable> persistableComponents;
    [SerializeField]
    private string uniqueId;
    public string GetUniqueId() => uniqueId;

    private Dictionary<string, object> localPManData;

    public event Action OnStatsProvided;

    void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueId))
        {
            uniqueId = Guid.NewGuid().ToString();
        }
    }

    void Awake()
    {
        persistableComponents = new List<IPersistable>(GetComponents<IPersistable>());
    }

    public Dictionary<string, object> SaveAllData()
    {
        var allObjectData = new Dictionary<string, object>();
        foreach (IPersistable component in persistableComponents)
        {
            // Use the component's type name as the key for its data
            allObjectData[component.GetType().Name] = component.SaveData();
        }
        return allObjectData;
    }

    public void ProvideAllData(Dictionary<string, object> pManData)
    {
        localPManData = pManData;
    }

    public Dictionary<string, object> GetWorldDataFor(IPersistable component)
    {
        string componentName = component.GetType().Name;
        if (localPManData.TryGetValue(componentName, out object componentData))
        {
            Debug.Log($"Applying WORLD data for {component.GetType().Name}");
            return componentData as Dictionary<string, object>;
        }
        else
        {
            Debug.Log($"Applying EMPTY data for {component.GetType().Name}");
            return new Dictionary<string, object>();
        }
    }
}