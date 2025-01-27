using UnityEngine;

public class PlayerStats : MonoBehaviour
{    
    [SerializeField] private PlayerStatsConfiguration playerStatsSO;
    public float strength {get; private set;}
    public float speed {get; private set;}
    public float sprintFactor {get; private set;}
    public float crouchFactor {get; private set;}
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
        sprintFactor = playerStatsSO.SprintFactor;
        crouchFactor = playerStatsSO.CrouchFactor;
        endurance = playerStatsSO.Endurance;
        health = playerStatsSO.Health;
        summoningCapacity = playerStatsSO.SummoningCapacity;
        bindingAffinity = playerStatsSO.BindingAffinity;
    }
}
