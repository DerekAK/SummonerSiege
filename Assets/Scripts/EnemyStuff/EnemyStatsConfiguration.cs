using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStatsConfiguration", menuName = "Scriptable Objects/EnemyStatsConfiguration")]
public class EnemyStatsConfiguration : ScriptableObject
{
    public float Strength{get; private set;} = 10f;
    public float Speed{get; private set;} = 5f; 
    public float Health{get; private set;} = 100f; 
    public float Corruption{get; private set;} = 100f;
}
