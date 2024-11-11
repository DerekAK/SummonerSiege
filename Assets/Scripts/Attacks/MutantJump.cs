using UnityEngine;

public class MutantJump : BaseAttackScript
{
    [SerializeField] private AnimationClip clip;
    
    public override void ExecuteAttack(Transform playerTransform, Transform projectileInstantiation){
        Debug.Log("MUTANT JUMP");
    }

    public override void SetAnimationClip(AnimatorOverrideController overrideController)
    {
        Debug.Log("GOT HERE");
        overrideController[ph2] = clip;
    }
   
}
