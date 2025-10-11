using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityStatsConfiguration", menuName = "Scriptable Objects/Entity Stats Configuration")]
public class EntityStatsConfigurationSO : ScriptableObject
{
    public List<StatDefinition> baseStats;
}

[System.Serializable]
public class StatDefinition
{
    public StatType type;
    public float baseValue;
}