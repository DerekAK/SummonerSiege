using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerCombat : CombatManager
{
    [SerializeField] private GameObject pfTmpSword;
    private Animator _anim;
    private PlayerState _playerState;
    private PlayerClipsHandler _playerClipsHandler;

    // Animator Hashes
    private int attackParamA = Animator.StringToHash("AttackTriggerA");
    private int attackParamB = Animator.StringToHash("AttackTriggerB");
    private int isHoldingParam = Animator.StringToHash("IsHolding"); // New parameter to control looping

    // Input & State Management (Your original boolean-based system)
    private float inputHoldTime = 0f;
    [SerializeField] private float longAttackThreshold = 0.2f;
    private Coroutine inputHoldCoroutine;
    private Coroutine attackCoroutine;

    private bool isAttacking = false;
    private bool checkingForInputRelease = false;
    
    // Combo Management (Your original system)
    private ComboSystem.Combo currentCombo;
    private Coroutine comboCoroutine;
    private Coroutine comboPressTypeCoroutine; // New coroutine to track press type during combos
    private bool comboInputBuffered = false; // True if the correct next combo input was made
    private bool inComboWindow = false;      // True if we are listening for the next combo input
    private bool inHoldCombo = false;
    
    private int currAnimAttackState = 0;
    [HideInInspector] public List<BaseWeapon> EquippedWeapons = new List<BaseWeapon>();

    [Tooltip("Time required before and after triggering the next attack in combo sequence")]
    [SerializeField] private float comboTimeBeforeThreshold = 0.3f;
    [SerializeField] private float comboTimeEndThreshold = 0.8f;

    [Header("Attack Data")]
    public PlayerAttackSO[] attackSOList;
    [HideInInspector] public NetworkVariable<int> nvAttackSOIndex = new NetworkVariable<int>();
    [SerializeField] private List<ComboSystem.Combo> defaultCombos = new List<ComboSystem.Combo>();
    private List<ComboSystem.Combo> possibleCombos = new List<ComboSystem.Combo>();

    private bool isInitialized = false;

    private void Awake(){
        _anim = GetComponent<Animator>();
        _playerState = GetComponent<PlayerState>();
        _playerClipsHandler = GetComponent<PlayerClipsHandler>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        
        GameInput.Instance.OnAttackButtonStarted += AttackButtonStart;
        GameInput.Instance.OnAttackButtonCanceled += AttackButtonCanceled;

        foreach (ComboSystem.Combo combo in defaultCombos){
            possibleCombos.Add(combo);
            combo.Initialize();
        }
        isInitialized = true;
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
        if (!((ChosenAttack.AirAttack && _playerState.InAir) || (!ChosenAttack.AirAttack && !_playerState.InAir)) || _playerState.Rolling)
        {
            return; // Invalid state, do nothing.
        }

        isAttacking = true;
        _playerState.currentAttack = ChosenAttack;
        _playerState.ChangeAttackStatus(true);
        
        ResetHitboxIndex();
        ChosenAttack.ExecuteAttack(this);

        int attackIndex = GetIndexByAttackSO((PlayerAttackSO)ChosenAttack);
        ChangeAttackSOIndexServerRpc(attackIndex);
        _playerClipsHandler.ChangeOverriderClipsServerRpc(attackIndex);
        
        if (currAnimAttackState == 0) _anim.SetTrigger(attackParamA);
        else _anim.SetTrigger(attackParamB);
        currAnimAttackState = (currAnimAttackState + 1) % 2;

        // Start tracking for the *next* combo input immediately, as you intended.
        if (comboCoroutine != null) StopCoroutine(comboCoroutine);
        comboCoroutine = StartCoroutine(TrackComboWindow());
    }

    private void AttackButtonStart(GameInput.AttackInput inputButton)
    {
        // If we're waiting for a combo input, start tracking the press type.
        if (inComboWindow)
        {
            if (comboPressTypeCoroutine != null) StopCoroutine(comboPressTypeCoroutine);
            comboPressTypeCoroutine = StartCoroutine(TrackComboPressType(inputButton));
        }
        // If we are not busy, start a new attack sequence.
        else if (!isAttacking)
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
            ChosenAttack = currentCombo.nextComboStep.attack;
            
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
            ChosenAttack = DecideChosenAttack(inputButton, ComboSystem.AttackPressType.Quick);
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
        ChosenAttack = DecideChosenAttack(inputButton, ComboSystem.AttackPressType.Hold);
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
                ChosenAttack = DecideChosenAttack(inputButton, ComboSystem.AttackPressType.Long);
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
        if (!isAttacking)
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
        isAttacking = false;
        _playerState.ChangeAttackStatus(false);
        inComboWindow = false;
        inHoldCombo = false;
        comboInputBuffered = false;
        _anim.SetBool(isHoldingParam, false);
        if (currentCombo != null)
        {
            currentCombo.ResetComboStep();
        }
    }

    // --- Animation Events ---
    
    public override void AnimationEvent_AttackFinished()
    {

        if (inHoldCombo)
        {
            return;
        }
        // This is called at the end of an animation if no combo was performed.
        // It cleans up the state. This fixes your original bug.
        AttackFinish();
    }
    
    private void AnimationEvent_ComboTransfer()
    {
        Debug.Log($"Calling comboTransfer for {this.name}");
        if (inComboWindow && comboInputBuffered)
        {
            // Input was correct and buffered in time.
            inComboWindow = false;
            comboInputBuffered = false;
            
            // Move to the next step and perform the attack.
            currentCombo.UpdateComboStep();
            ChosenAttack = currentCombo.currComboStep.attack;
            PerformAttack();
        }
    }
    
    private void AnimationEvent_HoldTransfer()
    {
        // This event is called by the looping "hold" animation.
        // It's a placeholder in case you need logic during the hold.
        // Currently, all hold logic is on release (AttackButtonCanceled).
    }

    [ServerRpc]
    private void ChangeAttackSOIndexServerRpc(int newIndex){
        nvAttackSOIndex.Value = newIndex;
    }

    public int GetIndexByAttackSO(PlayerAttackSO attackSO){
        return Array.IndexOf(attackSOList, attackSO);
    }
    
}

