using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Defines a contract for any component that needs to save and load data.
/// </summary>
public interface IPersistable
{
    public bool IsNetworkReady();
    public Dictionary<string, object> SaveData();
    public void ApplyData(Dictionary<string, object> data);
    public event Action OnStatsConfigured;
}