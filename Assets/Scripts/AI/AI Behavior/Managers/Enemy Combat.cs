// In CombatManager.cs
using System.Collections.Generic;
using UnityEngine;

public class EnemyCombat: CombatManager
{
    private Animator _anim;
    private AnimatorOverrideController _animOverrideController;
    private static int animAttack = Animator.StringToHash("Attack");
    private const string animAttackStringPlaceholder = "Attack Placeholder";
    [SerializeField] private List<EnemyAttackSO> availableAttacks;
    public List<EnemyAttackSO> AvailableAttacks => availableAttacks;

    private bool inAttack = false;
    public bool InAttack => inAttack;

    private bool stopRotate = false;
    public bool StopRotate => stopRotate;

    private BehaviorManager _behaviorManager;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _behaviorManager = GetComponent<BehaviorManager>();
        
        // Initialize the override controller
        _animOverrideController = new AnimatorOverrideController(_anim.runtimeAnimatorController);
    
        // 2. Assign this new, unique controller back to the animator
        _anim.runtimeAnimatorController = _animOverrideController;
    }
    
    public void StartChosenAttack()
    {
        if (!ChosenAttack)
        {
            Debug.LogError("Chosen Attack NULL!");
            return;
        }
        inAttack = true;
        _animOverrideController[animAttackStringPlaceholder] = ChosenAttack.AttackClip;
        _anim.SetTrigger(animAttack);

        ChosenAttack.ExecuteAttack(this);
    }

    public override void AnimationEvent_Trigger(int numEvent)
    {
        if (ChosenAttack != null && inAttack)
        {
            ChosenAttack.OnAnimationEvent(numEvent, this);
        }
    }

    /// <summary>
    /// This function is called by an Animation Event at the end of the attack.
    /// </summary>
    public override void AnimationEvent_AttackFinished()
    {
        inAttack = false;
        ChosenAttack = null;
        _behaviorManager.DecideNextIntention();
    }

    private void StopRotation()
    {
        stopRotate = true;
    }

    private void ResumeRotation()
    {
        stopRotate = false;
    }
}