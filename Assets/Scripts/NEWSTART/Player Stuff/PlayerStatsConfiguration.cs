using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "Scriptable Objects/PlayerStatsConfiguration")]
public class PlayerStatsConfiguration : ScriptableObject
{
    public float Strength{get; private set;} = 10f; //base damage
    public float Speed{get; private set;} = 5f; //base walking speed
    public float Endurance{get; private set;} = 10f; //base endurance
    public float Health{get; private set;} = 100f; //base health
    

    public float SummoningCapacity{get; private set;} = 1f; //total summoned weight of creatures you can hold
    public float BindingAffinity{get; private set;} = 20f; //chance to bind a creature

}
