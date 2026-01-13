using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerCombat : CombatManager
{

    [Header("Core Settings")]
    private PlayerMovement _playerMovement;
    private PhysicsManager _physicsManager;
    [SerializeField] private GameObject animatedGO;

    // Animator Hashes
    private int attackParamA = Animator.StringToHash("AttackTriggerA");
    private int attackParamB = Animator.StringToHash("AttackTriggerB");
    private int isHoldingParam = Animator.StringToHash("IsHolding"); // New parameter to control looping

    // Animation placeholder strings
    private const string animAttackStringPlaceholderA = "Attack A Placeholder";
    private const string animAttackStringPlaceholderB = "Attack B Placeholder";

    // Input & State Management (Your original boolean-based system)
    private float inputHoldTime = 0f;
    [SerializeField] private float longAttackThreshold = 0.2f;
    private Coroutine inputHoldCoroutine;
    private bool checkingForInputRelease = false;
    private bool isReadyToReceiveInput = false;
    
    // Combo Management (Your original system)
    private ComboSystem.Combo currentCombo;
    private Coroutine comboCoroutine;
    private Coroutine comboPressTypeCoroutine; // New coroutine to track press type during combos
    private bool comboInputBuffered = false; // True if the correct next combo input was made
    private bool inComboWindow = false;      // True if we are listening for the next combo input
    private bool inHoldCombo = false;
    
    private int currAnimAttackState = 0;

    [Tooltip("Time required before and after triggering the next attack in combo sequence")]
    [SerializeField] private float comboTimeBeforeThreshold = 0.3f;
    [SerializeField] private float comboTimeEndThreshold = 0.8f;

    [Header("Attack Data")]
    [SerializeField] private List<ComboSystem.Combo> defaultCombos = new List<ComboSystem.Combo>();
    private List<ComboSystem.Combo> possibleCombos = new List<ComboSystem.Combo>();


    [SerializeField] private string lockOnTargetTag;
    private List<Transform> lockOnTargets = new List<Transform>();
    public List<Transform> LockOnTargets => lockOnTargets;


    private void Awake(){
        _anim = animatedGO.GetComponent<Animator>();     

        if (_anim.runtimeAnimatorController != null)
        {
            _animOverrideController = new AnimatorOverrideController(_anim.runtimeAnimatorController);
            _anim.runtimeAnimatorController = _animOverrideController;
        }
        else
        {
            Debug.LogError($"Animator on {gameObject.name} does not have a Runtime Animator Controller!");
        }

        _playerMovement = GetComponent<PlayerMovement>();
        _physicsManager = GetComponent<PhysicsManager>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner) return;
        
        GameInput.Instance.OnAttackButtonStarted += AttackButtonStart;
        GameInput.Instance.OnAttackButtonCanceled += AttackButtonCanceled;

        foreach (ComboSystem.Combo combo in defaultCombos)
        {
            possibleCombos.Add(combo);
            combo.Initialize();
        }

        isReadyToReceiveInput = true;

        // temporary fix to make the spawned in player the viewer
        EndlessTerrain endlessTerrain = FindFirstObjectByType<EndlessTerrain>();
        if (endlessTerrain) endlessTerrain.SetViewerTransform(transform);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnAttackButtonStarted -= AttackButtonStart;
            GameInput.Instance.OnAttackButtonCanceled -= AttackButtonCanceled;
        }
    }

    private void PerformAttack()
    {
        // Check if player is in a valid state to perform the chosen attack
        if (!((ChosenAttack.AirAttack && _playerMovement.InAir) || (!ChosenAttack.AirAttack && !_playerMovement.InAir)) || _playerMovement.IsRolling)
        {
            return; // Invalid state, do nothing.
        }

        if (!loadedClips.ContainsKey(ChosenAttack.UniqueID))
        {
            Debug.LogWarning($"Attack {ChosenAttack.UniqueID} not loaded yet!");
            AttackFinish();
            return;
        }

        if (_physicsManager) _physicsManager.EnableAnimationMode();

        inAttack = true;
        
        ResetHitboxIndex();
        ChosenAttack.ExecuteAttack(this);
        
        if (currAnimAttackState == 0) _anim.SetTrigger(attackParamA);
        else _anim.SetTrigger(attackParamB);
        currAnimAttackState = (currAnimAttackState + 1) % 2;

        // Start tracking for the *next* combo input immediately, as you intended.
        if (comboCoroutine != null) StopCoroutine(comboCoroutine);
        comboCoroutine = StartCoroutine(TrackComboWindow());
    }

    private void AttackButtonStart(GameInput.AttackInput inputButton)
    {
        if (!isReadyToReceiveInput) return;
        // If we're waiting for a combo input, start tracking the press type.
        if (inComboWindow)
        {
            if (comboPressTypeCoroutine != null) StopCoroutine(comboPressTypeCoroutine);
            comboPressTypeCoroutine = StartCoroutine(TrackComboPressType(inputButton));
        }
        // If we are not busy, start a new attack sequence.
        else if (!inAttack)
        {
            if (inputHoldCoroutine != null) StopCoroutine(inputHoldCoroutine);
            inputHoldCoroutine = StartCoroutine(HandleInputHeldTime(inputButton));
        }
    }

    private void AttackButtonCanceled(GameInput.AttackInput inputButton)
    {
        // If we are canceling an input that started during the combo window...
        if (inComboWindow && comboPressTypeCoroutine != null)
        {
            StopCoroutine(comboPressTypeCoroutine);
            comboPressTypeCoroutine = null;

            // Since the button was released before the long press threshold, it's a QUICK press.
            // Check if this is the correct input for the combo.
            if (DecideChosenAttack(inputButton, ComboSystem.AttackPressType.Quick) != null)
            {
                comboInputBuffered = true;
            }
            return; // Exit to avoid running other logic.
        }
        
        if (inHoldCombo)
        {
            // The logic for releasing a hold, which triggers the NEXT attack in the combo.
            inHoldCombo = false;

            _anim.SetBool(isHoldingParam, false);
            
            // The "release" attack is the one AFTER the holdable one in the combo list.
            SetChosenAttack(currentCombo.nextComboStep.attack);
            
            if (ChosenAttack)
            {
                currentCombo.UpdateComboStep(); // Move combo state to the release attack
                PerformAttack();
            }
            else
            {
                // Should not happen if combo is set up correctly, but as a fallback, end the attack.
                AttackFinish();
            }
            return;
        }

        if (!checkingForInputRelease) return;
        
        checkingForInputRelease = false;
        if (inputHoldCoroutine != null) StopCoroutine(inputHoldCoroutine);

        if (inputHoldTime < longAttackThreshold)
        {
            // This is a Quick press.
            SetChosenAttack(DecideChosenAttack(inputButton, ComboSystem.AttackPressType.Quick));
            if (ChosenAttack)
            {
                PerformAttack();
            }
        }
    }

    private IEnumerator HandleInputHeldTime(GameInput.AttackInput inputButton)
    {
        checkingForInputRelease = true;
        inputHoldTime = 0f;

        // --- Check for HOLD press ---
        SetChosenAttack(DecideChosenAttack(inputButton, ComboSystem.AttackPressType.Hold));
        if (ChosenAttack)
        {
            inHoldCombo = true;
            _anim.SetBool(isHoldingParam, true);
            PerformAttack();
            yield break;
        }

        // --- Measure for LONG press ---
        while (GameInput.Instance.IsAttackButtonPressed(inputButton))
        {
            inputHoldTime += Time.deltaTime;
            if (inputHoldTime > longAttackThreshold)
            {
                checkingForInputRelease = false;
                SetChosenAttack(DecideChosenAttack(inputButton, ComboSystem.AttackPressType.Long));
                if (ChosenAttack)
                {
                    PerformAttack();
                }
                yield break;
            }
            yield return null;
        }
    }

    private PlayerAttackSO DecideChosenAttack(GameInput.AttackInput inputButton, ComboSystem.AttackPressType pressType)
    {
        if (inComboWindow)
        {
            // We are looking for the NEXT step in the current combo.
            ComboSystem.ComboStep requiredNextStep = currentCombo.nextComboStep;
            if (requiredNextStep.userInput == inputButton && requiredNextStep.userPressType == pressType)
            {
                return requiredNextStep.attack;
            }
            return null; // Wrong input for the combo.
        }
        
        // If not in a combo window, we are looking for the FIRST step of ANY combo.
        if (!inAttack)
        {
            foreach (var combo in possibleCombos)
            {
                if (combo.comboSteps[0].userInput == inputButton && combo.comboSteps[0].userPressType == pressType)
                {
                    currentCombo = combo;
                    currentCombo.ResetComboStep(); // Ensure it starts from the beginning.
                    return combo.comboSteps[0].attack;
                }
            }
        }
        return null; // No matching attack found.
    }

    private IEnumerator TrackComboWindow()
    {
        // This coroutine opens the window to buffer the next input.
        comboInputBuffered = false;

        // 1. Wait until the "combo window" officially opens.
        yield return new WaitForSeconds(comboTimeBeforeThreshold);
        
        inComboWindow = true;

        // 2. Wait until the combo window closes.
        float windowDuration = comboTimeEndThreshold - comboTimeBeforeThreshold;
        if (windowDuration < 0) windowDuration = 0;
        yield return new WaitForSeconds(windowDuration);

        // 3. If we are here, the window has closed.
        inComboWindow = false;
        
        // Clean up the combo press coroutine if it's still running (e.g., player held button too long)
        if (comboPressTypeCoroutine != null)
        {
            StopCoroutine(comboPressTypeCoroutine);
            comboPressTypeCoroutine = null;
        }
    }
    
    // New coroutine to handle combo input timing
    private IEnumerator TrackComboPressType(GameInput.AttackInput inputButton)
    {
        yield return new WaitForSeconds(longAttackThreshold);

        // If we reach this point, the button has been held long enough for a LONG press.
        // Check if the window is still open and if a LONG press is the correct combo input.
        if (inComboWindow && DecideChosenAttack(inputButton, ComboSystem.AttackPressType.Long) != null)
        {
            comboInputBuffered = true;
        }
        comboPressTypeCoroutine = null;
    }

    private void AttackFinish()
    {
        // This is the definitive end of an attack sequence.
        inAttack = false;
        inComboWindow = false;
        inHoldCombo = false;
        comboInputBuffered = false;
        _anim.SetBool(isHoldingParam, false);
        if (currentCombo != null)
        {
            currentCombo.ResetComboStep();
        }

        if (_physicsManager) _physicsManager.EnablePhysicsMode();
    }

    // --- Animation Events ---
    
    public override void AnimationEvent_AttackFinished()
    {
        Debug.Log("Attack finished!");

        if (inHoldCombo)
        {
            return;
        }
        // This is called at the end of an animation if no combo was performed.
        // It cleans up the state. This fixes your original bug.
        AttackFinish();
    }
    
    public override void AnimationEvent_ComboTransfer()
    {
        if (inComboWindow && comboInputBuffered)
        {
            // Input was correct and buffered in time.
            inComboWindow = false;
            comboInputBuffered = false;
            
            // Move to the next step and perform the attack.
            currentCombo.UpdateComboStep();
            SetChosenAttack(currentCombo.currComboStep.attack);
            PerformAttack();
        }
    }
    
    public void AnimationEvent_HoldTransfer()
    {
        // This event is called by the looping "hold" animation.
        // It's a placeholder in case you need logic during the hold.
        // Currently, all hold logic is on release (AttackButtonCanceled).
    }


    protected override async Task LoadDefaultAttackAnimations()
    {
        var tasks = new List<Task>();
        var uniqueIDs = new HashSet<int>(); // Auto-handles duplicate attacks in combos

        foreach (ComboSystem.Combo combo in defaultCombos)
        {
            foreach (ComboSystem.ComboStep comboStep in combo.comboSteps)
            {
                if(comboStep.attack != null)
                    uniqueIDs.Add(comboStep.attack.UniqueID);
            }
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
        _animOverrideController[animAttackStringPlaceholderA] = clip;
        _animOverrideController[animAttackStringPlaceholderB] = clip;
    }


    private void OnTriggerEnter(Collider other){
        if(other.CompareTag(lockOnTargetTag)){lockOnTargets.Add(other.transform);}
    }
    
    private void OnTriggerExit(Collider other){
        if(other.CompareTag(lockOnTargetTag)){lockOnTargets.Remove(other.transform);}
    }
}

