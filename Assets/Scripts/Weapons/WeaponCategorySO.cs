using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "WeaponCategory", menuName = "Scriptable Objects/Weapons/Category")]
public class WeaponCategorySO : ScriptableObject
{    
    [Header("Basic Attack Animations - 1H")]
    public AssetReference anim_1H_E;
    public AssetReference anim_1H_NE;
    public AssetReference anim_1H_NW;
    public AssetReference anim_1H_SE;
    public AssetReference anim_1H_SW;
    public AssetReference anim_1H_W;
    
    [Header("Basic Attack Animations - 2H")]
    public AssetReference anim_2H_E;
    public AssetReference anim_2H_NE;
    public AssetReference anim_2H_NW;
    public AssetReference anim_2H_SE;
    public AssetReference anim_2H_SW;
    public AssetReference anim_2H_W;

    [Header("Parry Animations")]
    public AssetReference anim_Parry_E;
    public AssetReference anim_Parry_NE;
    public AssetReference anim_Parry_NW;
    public AssetReference anim_Parry_SE;
    public AssetReference anim_Parry_SW;
    public AssetReference anim_Parry_W;
    
}