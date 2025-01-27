using UnityEngine;

public class BaseWeaponScript : MonoBehaviour
{
    [Tooltip("1-one-handed, 2-double-handed, 3-double-wielding")]
    [SerializeField] private int weaponType; 
    public int GetWeaponType(){return weaponType;}
    [SerializeField] private int weaponQuality;
    public int GetWeaponQuality(){return weaponQuality;}  
}