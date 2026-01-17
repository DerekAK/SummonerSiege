using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class EnemyCombat: CombatManager
{
    private static int animAttack = Animator.StringToHash("Attack");
    private const string animAttackStringPlaceholder = "Attack Placeholder";
    [SerializeField] private List<EnemyAttackSO> availableAttacks;
    public List<EnemyAttackSO> AvailableAttacks => availableAttacks;

    private bool stopRotate = false;
    public bool StopRotate => stopRotate;

    private BehaviorManager _behaviorManager;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
        if (_anim.runtimeAnimatorController != null)
        {
            _animOverrideController = new AnimatorOverrideController(_anim.runtimeAnimatorController);
            _anim.runtimeAnimatorController = _animOverrideController;
        }
        else
        {
            Debug.LogError($"Animator on {gameObject.name} does not have a Runtime Animator Controller!");
        }

        _behaviorManager = GetComponent<BehaviorManager>();
    }

    public void StartChosenAttack()
    {
        if (!ChosenAttack)
        {
            Debug.LogError("Chosen Attack NULL!");
            return;
        }
        if (!loadedClips.ContainsKey(ChosenAttack.UniqueID))
        {
            Debug.LogWarning($"Attack {ChosenAttack.UniqueID} not loaded yet!");
            inAttack = false;
            SetChosenAttack(null);
            _behaviorManager.DecideNextIntention();
            return;
        }

        inAttack = true;
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
        stopRotate = false;
        SetChosenAttack(null);
        
        _behaviorManager.DecideNextIntention();
        
    }

    private void AnimationEvent_StopRotation()
    {
        stopRotate = true;
    }

    private void AnimationEvent_ResumeRotation()
    {
        stopRotate = false;
    }

    protected override async Task LoadDefaultAttackAnimations()
    {
        var tasks = new List<Task>();
        var uniqueIDs = new HashSet<int>(); // Auto-handles duplicates

        foreach (BaseAttackSO attackSO in availableAttacks)
        {
            if(attackSO != null) uniqueIDs.Add(attackSO.UniqueID);
        }
        
        foreach (int id in uniqueIDs)
        {
            tasks.Add(LoadClipFromReference(id)); // Add the awaitable Task
        }

        // Wait for all clips to finish loading in parallel
        await Task.WhenAll(tasks);
    }

    protected override void ApplyClipToAnimator(AnimationClip clip)
    {
        if (clip == null)
        {
            Debug.LogError("Animation clip was not loaded in time for the enemy to use it for their attack!");
            return;
        }
        _animOverrideController[animAttackStringPlaceholder] = clip;
    }
}