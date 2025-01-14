using UnityEditor.Rendering;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{    
    private PlayerStatsConfiguration playerStatsSO;
    public float strength {get; private set;}
    public float speed {get; private set;}
    public float endurance {get; private set;}
    public float health {get; private set;}
    public float summoningCapacity {get; private set;}
    public float bindingAffinity {get; private set;}

    
    private void Awake(){
        ConfigurePlayerStats();
    }

    private void ConfigurePlayerStats(){
        strength = playerStatsSO.Strength;
        speed = playerStatsSO.Speed;
        endurance = playerStatsSO.Endurance;
        health = playerStatsSO.Health;
        summoningCapacity = playerStatsSO.SummoningCapacity;
        bindingAffinity = playerStatsSO.BindingAffinity;
    }
}
