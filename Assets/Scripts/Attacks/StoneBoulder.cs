using UnityEngine;

public class StoneBoulder : BaseAttackScript
{
    
    [SerializeField] private Transform pfStoneBoulder;
    [SerializeField] private AnimationClip clip;
    
    public override void ExecuteAttack(Transform playerTransform, Transform projectileInstantiation){
        Debug.Log("STONE BOULDER THROW");
        Transform boulder = Instantiate(pfStoneBoulder, projectileInstantiation.position, Quaternion.identity);
        boulder.SetParent(projectileInstantiation);
    }

    public override void SetAnimationClip(AnimatorOverrideController overrideController)
    {
        
        overrideController[ph3] = clip;
    }
}
