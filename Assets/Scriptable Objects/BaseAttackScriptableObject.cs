using UnityEngine;

[CreateAssetMenu(fileName = "BaseAttackScriptableObject", menuName = "Scriptable Objects/BaseAttackScriptableObject")]
public class BaseAttackScriptableObject : ScriptableObject
{
    [SerializeField] private bool requiresWeapon;
    public bool DoesRequireWeapon(){return requiresWeapon;}
    
    [Tooltip("1-melee, 2-medium-range, 3-long-range")]
    [SerializeField] protected int attackType;
    public int GetAttackType(){return attackType;}
}
