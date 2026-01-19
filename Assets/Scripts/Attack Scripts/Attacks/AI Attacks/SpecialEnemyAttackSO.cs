using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "NewAttackAction", menuName = "Scriptable Objects/Attacks/Enemy/Special Attack")]
public class SpecialEnemyAttackSO: EnemyAttackSO
{
    
    public AssetReference AnimationClipRef;

}