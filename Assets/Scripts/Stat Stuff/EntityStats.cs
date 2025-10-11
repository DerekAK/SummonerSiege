using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;

public class EntityStats : NetworkBehaviour, IPersistable
{
    [SerializeField] private EntityStatsConfigurationSO statsConfigurationSO;

    // A dictionary to hold all stats for this entity, easily accessible by their type
    private NetworkList<NetStat> Stats = new NetworkList<NetStat>();
    public event Action<StatType, float> OnStatValueChanged;
    public event Action OnStatsConfigured;
    private PersistenceManager _persistenceManager;
    private bool isWorldDataReady;

    private void Awake()
    {
        _persistenceManager = GetComponent<PersistenceManager>();
    }
    
    private void OnEnable()
    {
        Debug.Log("Subscribing to onworlddataloaded event!");
        ServerSaveManager.OnWorldDataLoaded += OnWorldDataIsReady;
    }

    private void OnDisable()
    {
        ServerSaveManager.OnWorldDataLoaded -= OnWorldDataIsReady;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Stats.OnListChanged += HandleListChanged;
        TryInitializeStats();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Stats.OnListChanged -= HandleListChanged;
    }

    private void OnWorldDataIsReady()
    {
        isWorldDataReady = true;
        // After receiving the signal, we try to initialize.
        TryInitializeStats();
    }

    private void TryInitializeStats()
    {
        if (IsSpawned && IsServer && isWorldDataReady)
        {
            ApplyData(_persistenceManager.GetWorldDataFor(this));
        }
    }

    private void HandleListChanged(NetworkListEvent<NetStat> changeEvent)
    {
        if (changeEvent.Type == NetworkListEvent<NetStat>.EventType.Value)
        {
            NetStat changedStat = changeEvent.Value;
            // Fire our custom, specific event for other scripts to hear.
            OnStatValueChanged?.Invoke(changedStat.Type, changedStat.CurrentValue);
        }
    }

    public void ApplyData(Dictionary<string, object> data)
    {
        // Loop through the stats defined in our configuration
        foreach (StatDefinition statDef in statsConfigurationSO.baseStats)
        {
            // Check if the save data contains info for this stat
            if (data.TryGetValue($"{statDef.type}_Current", out object currentValue) &&
                data.TryGetValue($"{statDef.type}_Max", out object maxValue))
            {

                float current = System.Convert.ToSingle(currentValue);
                float max = System.Convert.ToSingle(maxValue);

                Stats.Add(new NetStat
                {
                    Type = statDef.type,
                    CurrentValue = current,
                    MaxValue = max
                });
            }
            else
            {
                // If no saved data, use the default from the configuration
                Stats.Add(new NetStat
                {
                    Type = statDef.type,
                    CurrentValue = statDef.baseValue,
                    MaxValue = statDef.baseValue
                });
            }
        }

        OnStatsConfigured?.Invoke();
    }

    public Dictionary<string, object> SaveData()
    {
        var data = new Dictionary<string, object>();
        // Loop through our stats and add them to the save data
        foreach (NetStat netStat in Stats)
        {
            // We save both current and max value for each stat
            data[$"{netStat.Type}_Current"] = netStat.CurrentValue;
            data[$"{netStat.Type}_Max"] = netStat.MaxValue;
        }
        return data;
    }

    public bool TryGetStat(StatType type, out NetStat foundStat)
    {
        // Loop through our list of stats
        foreach (NetStat stat in Stats)
        {
            if (stat.Type == type)
            {
                foundStat = stat; // This assigns a copy of the struct
                return true;
            }
        }

        // If we didn't find the stat, return false and a default struct
        foundStat = default;
        return false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ModifyStatServerRpc(StatType type, float amount)
    {
        // To modify a struct in a NetworkList, you must find it, copy it,
        // modify the copy, and then write the entire struct back.
        for (int i = 0; i < Stats.Count; i++)
        {
            if (Stats[i].Type == type)
            {
                NetStat modifiedStat = Stats[i];
                
                float newValue = Mathf.Clamp(modifiedStat.CurrentValue + amount, 0, modifiedStat.MaxValue);
                
                modifiedStat.CurrentValue = newValue;
                Stats[i] = modifiedStat; // Write the modified copy back
                return; // Exit after finding and modifying
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetStatServerRpc(StatType type, float value)
    {
        for (int i = 0; i < Stats.Count; i++)
        {
            if (Stats[i].Type == type)
            {
                NetStat modifiedStat = Stats[i];
                modifiedStat.CurrentValue = Mathf.Clamp(value, 0, modifiedStat.MaxValue);
                Stats[i] = modifiedStat;
                return;
            }
        }
    }
}

public struct NetStat : INetworkSerializable, System.IEquatable<NetStat>
{
    public StatType Type;
    public float CurrentValue;
    public float MaxValue;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Type);
        serializer.SerializeValue(ref CurrentValue);
        serializer.SerializeValue(ref MaxValue);
    }

    public bool Equals(NetStat other)
    {
        return Type == other.Type && CurrentValue == other.CurrentValue && MaxValue == other.MaxValue;
    }
}

public enum StatType
{
    Health,
    Strength,
    Speed,
    Endurance,
    SummoningCapacity,
    BindingAffinity,
    Corruption
}