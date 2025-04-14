using Unity.Netcode;
using UnityEngine;

public class EnemyStats: NetworkStats
{    
    [SerializeField] private EnemyStatsConfiguration enemyStatsSO;
    
    private NetworkVariable<float> healthStat = new NetworkVariable<float>();
    private NetworkVariable<float> maxHealthStat = new NetworkVariable<float>();
    private NetworkVariable<float> speedStat = new NetworkVariable<float>();
    private NetworkVariable<float> maxSpeedStat = new NetworkVariable<float>();
    private NetworkVariable<float> strengthStat = new NetworkVariable<float>();
    private NetworkVariable<float> maxStrengthStat = new NetworkVariable<float>();
    private NetworkVariable<float> corruptionStat = new NetworkVariable<float>();
    private NetworkVariable<float> maxCorruptionStat = new NetworkVariable<float>();


    public NetworkStat StrengthStat;
    public NetworkStat SpeedStat;
    public NetworkStat CorruptionStat;
    private void Awake(){
        
        HealthStat = new NetworkStat(healthStat, maxHealthStat);
        StrengthStat = new NetworkStat(strengthStat, maxStrengthStat);
        SpeedStat = new NetworkStat(speedStat, maxSpeedStat);
        CorruptionStat = new NetworkStat(corruptionStat, maxCorruptionStat);

        ConfigureEnemyStats();
    }

    private void ConfigureEnemyStats(){
        HealthStat.Stat.Value = enemyStatsSO.Health;
        StrengthStat.Stat.Value = enemyStatsSO.Strength;
        SpeedStat.Stat.Value = enemyStatsSO.Speed;
        CorruptionStat.Stat.Value = enemyStatsSO.Corruption;

        HealthStat.MaxStat.Value = enemyStatsSO.Health;
        StrengthStat.MaxStat.Value = enemyStatsSO.Strength;
        SpeedStat.MaxStat.Value = enemyStatsSO.Speed;
        CorruptionStat.MaxStat.Value = enemyStatsSO.Corruption;
    }

}