using UnityEngine;

public class Kick : BaseAttackScript
{
    [SerializeField] private AnimationClip clip;
    
    public override void ExecuteAttack(Transform playerTransform, Transform projectileInstantiation){
        Debug.Log("Kick");
    }

    public override void SetAnimationClip(AnimatorOverrideController overrideController)
    {
        Debug.Log("GOT HERE");
        overrideController[ph1] = clip;
    }
   
}
