using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "Scriptable Objects/PlayerStatsConfiguration")]
public class PlayerStatsConfiguration : ScriptableObject
{
    public float Strength = 10f; //base damage
    public float Speed = 5f; //base walking speed
    public float SprintFactor = 5f;
    public float CrouchFactor = 0.5f;
    public float Endurance = 10f; //base endurance
    public float Health = 100f; //base health
    

    public float SummoningCapacity = 1f; //total summoned weight of creatures you can hold
    public float BindingAffinity = 20f; //chance to bind a creature

}
