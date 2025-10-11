using System;
using System.Collections.Generic;

/// <summary>
/// Defines a contract for any component that needs to save and load data.
/// </summary>
public interface IPersistable
{
    /// <summary>
    /// Gathers the component's current state into a generic dictionary.
    /// </summary>
    /// <returns>A dictionary containing the data to be saved.</returns>
    public Dictionary<string, object> SaveData();

    /// <summary>
    /// Applies a saved state to the component from a generic dictionary, called from within the ipersistable class itself
    /// </summary>
    /// <param name="data">The dictionary of data to apply.</param>
    public void ApplyData(Dictionary<string, object> data);    
    public event Action OnStatsConfigured;
}