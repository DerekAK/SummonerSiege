using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStatsConfiguration", menuName = "Scriptable Objects/PlayerStatsConfiguration")]
public class PlayerStatsConfiguration : ScriptableObject
{
    public float Strength{get; private set;} = 10f;
    public float Speed{get; private set;} = 5f; 
    public float Endurance{get; private set;} = 10f; 
    public float Health{get; private set;} = 100f; 
    

    public float SummoningCapacity{get; private set;} = 1f; //total summoned weight of creatures you can hold
    public float BindingAffinity{get; private set;} = 20f; //chance to bind a creature
    public float Corruption{get; private set;} = 100f;

}
