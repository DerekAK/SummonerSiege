using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "WeaponCategory", menuName = "Scriptable Objects/Weapons/Category")]
public class WeaponCategorySO : ScriptableObject
{
    public string categoryName;
    
    [Header("Basic Attack Animations - 1H")]
    public AssetReference anim_1H_Up;
    public AssetReference anim_1H_Down;
    public AssetReference anim_1H_Left;
    public AssetReference anim_1H_Right;
    
    [Header("Basic Attack Animations - 2H")]
    public AssetReference anim_2H_Up;
    public AssetReference anim_2H_Down;
    public AssetReference anim_2H_Left;
    public AssetReference anim_2H_Right;
    
}