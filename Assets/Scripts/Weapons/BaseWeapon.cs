using UnityEngine;

public class BaseWeapon : MonoBehaviour
{
    
    public enum eWeaponType
    {
        SingleWielding,
        DoubleWielding
    }

    public eWeaponType WeaponType;

}