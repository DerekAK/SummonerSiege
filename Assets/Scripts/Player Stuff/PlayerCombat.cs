using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour
{
    private Animator _anim;
    private PlayerState _playerState;
    private PlayerClipsHandler _playerClipsHandler;
    private int attackParamA = Animator.StringToHash("AttackTriggerA");
    private int attackParamB = Animator.StringToHash("AttackTriggerB");
    private float inputHoldTime = 0f;
    private float longAttackThreshold = 0.2f;
    private Coroutine inputHoldCoroutine;
    public bool isAttacking = false;
    private bool checkingForInputRelease = false;
    private ComboSystem.Combo currentCombo;
    private BaseAttackSO currentAttackSO;
    private Coroutine comboCoroutine;
    private Coroutine attackCoroutine;
    private bool comboTransition = false;
    private bool inCombo = false;
    private bool inHoldCombo = false;
    int currAnimAttackState = 0;
    public static string HitboxTag = "Hitbox";
    public static string WeaponTag = "Weapon";

    [Tooltip("Time required before and after triggering the next attack in combo sequence")]
    [SerializeField] private float comboTimeBeforeThreshold = 0.3f;
    [SerializeField] private float comboTimeEndThreshold = 0.8f;

    [Header("Attack Data")]
    public BaseAttackSO[] attackSOList;
    public int CurrHitboxIndex;
    public NetworkVariable<int> attackSOIndexNetworkVariable = new NetworkVariable<int>();

    [SerializeField] private List<ComboSystem.Combo> defaultCombos = new List<ComboSystem.Combo>();
    [SerializeField] private List<ComboSystem.Combo> possibleCombos = new List<ComboSystem.Combo>();
    [SerializeField] private List<GameObject> ActiveHitboxes = new List<GameObject>();
    public List<Transform> EquippedWeapons = new List<Transform>();

    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _playerState = GetComponent<PlayerState>();
        _playerClipsHandler = GetComponent<PlayerClipsHandler>();
    }

    private void Start()
    {
        GameInput.Instance.OnAttackButtonStarted += AttackButtonStart;
        GameInput.Instance.OnAttackButtonCanceled += AttackButtonCanceled;

        foreach (ComboSystem.Combo combo in defaultCombos)
        {
            possibleCombos.Add(combo);
            combo.Initialize();
        }
    }

    private void OnDisable()
    {
        GameInput.Instance.OnAttackButtonStarted -= AttackButtonStart;
        GameInput.Instance.OnAttackButtonCanceled -= AttackButtonCanceled;
    }

    private void AttackButtonStart(GameInput.AttackInput inputButton)
    {
        if (!IsLocalPlayer) return;
        if (inHoldCombo) return;

        if (!isAttacking || inCombo)
        {
            checkingForInputRelease = true;
            inputHoldCoroutine = StartCoroutine(HandleInputHeldTime(inputButton));
        }
    }

    private void AttackButtonCanceled(GameInput.AttackInput inputButton)
    {
        if (!checkingForInputRelease) return;
        checkingForInputRelease = false;
        if (inputHoldCoroutine != null) StopCoroutine(inputHoldCoroutine);

        if (inHoldCombo)
        {
            // Advance to release attack if holdable
            if (currentAttackSO?.Holdable == true)
            {
                currentAttackSO = DecideCurrentAttackSO(GameInput.AttackInput.None, ComboSystem.AttackPressType.Hold);
                if (currentAttackSO)
                {
                    if (attackCoroutine != null) StopCoroutine(attackCoroutine);
                    attackCoroutine = StartCoroutine(HandlePerformAttack());
                }
            }
            inHoldCombo = false;
            currentCombo?.ResetComboStep(); // Reset after release
            return;
        }

        if (inputHoldTime < longAttackThreshold)
        {
            currentAttackSO = DecideCurrentAttackSO(inputButton, ComboSystem.AttackPressType.Quick);
            if (currentAttackSO)
            {
                if (attackCoroutine != null) StopCoroutine(attackCoroutine);
                attackCoroutine = StartCoroutine(HandlePerformAttack());
            }
        }
    }

    private IEnumerator HandleInputHeldTime(GameInput.AttackInput inputButton)
    {
        currentAttackSO = DecideCurrentAttackSO(inputButton, ComboSystem.AttackPressType.Hold);
        if (currentAttackSO)
        {
            inHoldCombo = true;
            if (attackCoroutine != null) StopCoroutine(attackCoroutine);
            attackCoroutine = StartCoroutine(HandlePerformAttack());
            yield break;
        }

        inputHoldTime = 0;

        while (GameInput.Instance.IsAttackButtonPressed(inputButton))
        {
            inputHoldTime += Time.deltaTime;
            if (inputHoldTime > longAttackThreshold)
            {
                currentAttackSO = DecideCurrentAttackSO(inputButton, ComboSystem.AttackPressType.Long);
                if (currentAttackSO)
                {
                    if (attackCoroutine != null) StopCoroutine(attackCoroutine);
                    attackCoroutine = StartCoroutine(HandlePerformAttack());
                    checkingForInputRelease = false;
                }
                yield break;
            }
            yield return null;
        }
    }

    private BaseAttackSO DecideCurrentAttackSO(GameInput.AttackInput inputButton, ComboSystem.AttackPressType pressType)
    {
        if (inCombo && currentCombo.currComboStep.userPressType == pressType && currentCombo.currComboStep.userInput == inputButton)
        {
            Debug.Log("Registered combo input!");
            return currentCombo.currComboStep.attack;
        }
        else if (inCombo)
        {
            Debug.Log("In combo but wrong input");
            return null;
        }
        else
        {
            if (!isAttacking)
            {
                foreach (ComboSystem.Combo combo in possibleCombos)
                {
                    ComboSystem.ComboStep firstComboStep = combo.comboSteps[0];
                    if (firstComboStep.userInput == inputButton && firstComboStep.userPressType == pressType)
                    {
                        Debug.Log("Found the correct normal attack!");
                        currentCombo = combo;
                        return firstComboStep.attack;
                    }
                }
                Debug.Log("No Normal attack matching these inputs!");
                return null;
            }
            else
            {
                Debug.Log("In attack but past combat window");
                return null;
            }
        }
    }

    private IEnumerator HandlePerformAttack()
    {
        Debug.Log("Starting HandlePerformAttack!");
        if (((currentAttackSO.AirAttack && _playerState.InAir) || (!currentAttackSO.AirAttack && !_playerState.InAir)) && !_playerState.Rolling)
        {
            if (inCombo)
            {
                while (!comboTransition)
                {
                    yield return null;
                }
            }
            PerformAttack();
            comboTransition = false;
            currentCombo.UpdateComboStep();
            inCombo = currentCombo.currIndex > 0;
            if (inCombo)
            {
                if (comboCoroutine != null) StopCoroutine(comboCoroutine);
                comboCoroutine = StartCoroutine(TrackComboWindow());
            }
        }
    }

    private void PerformAttack()
    {
        if (!IsLocalPlayer) return;
        isAttacking = true;
        _playerState.currentAttack = currentAttackSO;
        _playerState.ChangeAttackStatus(true);
        ChangeAttackSOIndexServerRpc(GetIndexByAttackSO(currentAttackSO));
        _playerClipsHandler.HandleAttackClip(GetIndexByAttackSO(currentAttackSO));

        if (currAnimAttackState == 0) _anim.SetTrigger(attackParamA);
        else _anim.SetTrigger(attackParamB);
        currAnimAttackState = (currAnimAttackState + 1) % 2;
    }

    [ServerRpc]
    private void ChangeAttackSOIndexServerRpc(int newIndex)
    {
        attackSOIndexNetworkVariable.Value = newIndex;
    }

    private IEnumerator TrackComboWindow()
    {
        if (inHoldCombo) yield break;
        float elapsedTime = 0f;
        inCombo = false;
        while (elapsedTime < comboTimeBeforeThreshold)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        inCombo = true;
        while (elapsedTime < comboTimeEndThreshold)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        inCombo = false;
        currentCombo.ResetComboStep();
    }

    private void AttackFinish()
    {
        isAttacking = false;
        _playerState.ChangeAttackStatus(false);
        inCombo = false;
    }

    private void ComboTransfer()
    {
        comboTransition = true;
    }

    private void HoldAttackTransfer()
    {
        if (!inHoldCombo) return;
        else if (currentAttackSO.Holdable)
        {
            PerformAttack();
        }
        else
        {
            currentAttackSO = DecideCurrentAttackSO(GameInput.AttackInput.None, ComboSystem.AttackPressType.Hold);
            if (currentAttackSO)
            {
                if (attackCoroutine != null) StopCoroutine(attackCoroutine);
                attackCoroutine = StartCoroutine(HandlePerformAttack());
            }
        }
    }

    private void EnableHitBoxes()
    {
        if (!IsServer) return;
        GetAttackSOByIndex(attackSOIndexNetworkVariable.Value).Enable(this, _anim);
    }

    private void DisableHitBoxes()
    {
        if (!IsServer) return;
        GetAttackSOByIndex(attackSOIndexNetworkVariable.Value).Disable(this, _anim);
    }

    public int GetIndexByAttackSO(BaseAttackSO attackSO)
    {
        return Array.IndexOf(attackSOList, attackSO);
    }

    public BaseAttackSO GetAttackSOByIndex(int attackSOIndex)
    {
        return attackSOList[attackSOIndex];
    }
}