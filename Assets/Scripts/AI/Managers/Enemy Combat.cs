using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyCombat: CombatManager
{
    // Animator variables
    private static int specialAnimAttackTrigger = Animator.StringToHash("SpecialAttack");
    private static int basic1HAnimAttackTrigger = Animator.StringToHash("BasicAttack1H");
    private static int basic2HAnimAttackTrigger = Animator.StringToHash("BasicAttack2H");
    private static int parry1HTrigger = Animator.StringToHash("Parry1H");
    private static int parry2HTrigger = Animator.StringToHash("Parry2H");
    private static int basic1HXInput = Animator.StringToHash("BasicAttack1HX");
    private static int basic1HYInput = Animator.StringToHash("BasicAttack1HY");
    private static int basic2HXInput = Animator.StringToHash("BasicAttack2HX");
    private static int basic2HYInput = Animator.StringToHash("BasicAttack2HY");
    private static int parry1HXInput = Animator.StringToHash("Parry1HX");
    private static int parry1HYInput = Animator.StringToHash("Parry1HY");
    private static int parry2HXInput = Animator.StringToHash("Parry2HX");
    private static int parry2HYInput = Animator.StringToHash("Parry2HY");
    private const string animSpecialAttackStringPlaceholder = "Attack Placeholder";

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
        if(!ChosenAttack) Debug.Log("ATTACK IS NULL!!");
        if (!IsAttackLoaded(ChosenAttack) || !ChosenAttack)
        {
            Debug.LogWarning($"Attack {ChosenAttack} is null or its id: {ChosenAttack.UniqueID} not loaded yet!");
            inAttack = false;
            SetChosenAttack(null);
            _behaviorManager.DecideNextIntention();
            return;
        }
        inAttack = true;

        ChosenAttack.ExecuteAttack(this);

        // this is where we set the animator to the correct state
        if (ChosenAttack is BasicEnemyAttackSO)
        {
            bool twoHanded = DecideTwoHanded();
            if (twoHanded) _anim.SetTrigger(basic2HAnimAttackTrigger);
            else _anim.SetTrigger(basic1HAnimAttackTrigger);
        }
        else if (ChosenAttack is SpecialEnemyAttackSO) _anim.SetTrigger(specialAnimAttackTrigger);
        else Debug.LogError("Enemy chose a non enemy attack somehow!");
    }

    private bool DecideTwoHanded()
    {
        return false;
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
            if(attackSO != null && attackSO is SpecialEnemyAttackSO) uniqueIDs.Add(attackSO.UniqueID);
        }
        
        foreach (int id in uniqueIDs)
        {
            tasks.Add(LoadAndApplyAttack(id)); // Add the awaitable Task
        }

        // Wait for all clips to finish loading in parallel
        await Task.WhenAll(tasks);
    }

    protected override void ApplySpecialAttackClipToAnimator(AnimationClip clip)
    {
        if (clip == null)
        {
            Debug.LogError("Animation clip was not loaded in time for the enemy to use it for their attack!");
            return;
        }
        _animOverrideController[animSpecialAttackStringPlaceholder] = clip;
    }

    public void SetBasicAttackArc(Vector2 arc)
    {
        bool twoHanded = DecideTwoHanded();
        if (twoHanded){
            _anim.SetFloat(basic2HXInput, arc.x); 
            _anim.SetFloat(basic2HYInput, arc.y); 
        }
        else
        {
            _anim.SetFloat(basic1HXInput, arc.x); 
            _anim.SetFloat(basic1HYInput, arc.y); 
        }

    }

}