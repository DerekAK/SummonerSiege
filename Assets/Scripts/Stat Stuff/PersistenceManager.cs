using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using System;

public class PersistenceManager : NetworkBehaviour
{
    private List<IPersistable> persistableComponents;
    [SerializeField] private string uniqueId;
    public ePersistenceType PersistenceType;
    public string GetUniqueId() => uniqueId;
    private Dictionary<string, object> localPManData;
    public bool ReceivedData = false;


    public override void OnNetworkDespawn()
    {
        if (PersistenceType == ePersistenceType.Player)
        {
            PlayerSaveManager.Instance.SavePlayer(OwnerClientId);
        }
    }

    public enum ePersistenceType
    {
        World,  // Saved to world_save.json (e.g., enemies, chests)
        Player  // Saved to player_X.json (e.g., player inventory, stats)
    }

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

    public void ApplyAllData(Dictionary<string, object> pManData)
    {
        // pManData should never be null, otherwise the getworlddatafunction could fail
        foreach (IPersistable component in persistableComponents)
        {
            // Use the component's type name as the key for its data
            string componentName = component.GetType().Name;
            if (pManData.TryGetValue(componentName, out object componentData))
            {
                //Debug.Log($"Applying WORLD data for {component.GetType().Name}");
                component.ApplyData(componentData as Dictionary<string, object>);
            }
            else
            {
                //Debug.Log($"Applying EMPTY data for {component.GetType().Name}");
                component.ApplyData(new Dictionary<string, object>());
            }
        }

    }

}