using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
/*
IMPORTANT TO UNDERSTAND: any attack that might possibly lead into a combo must be in a combo. in other words,
there can't be two different viable attacks on a player that both take in the same input, because otherwise 
the computer wouldn't know which one to use (no shit). so basically, need to first figure out which inputs we're 
listening for, and then only have one viable attack from that starting input at any time. in other words, at any
given point, no matter the state, there is only one viable next move for any single input.
*/

public class PlayerCombat : NetworkBehaviour
{
    private Animator _anim;
    private PlayerState _playerState;
    private PlayerClipsHandler _playerClipsHandler;
    private int attackParamA = Animator.StringToHash("AttackTriggerA");
    private int attackParamB = Animator.StringToHash("AttackTriggerB");
    private float inputHoldTime = 0f;
    private float weightedHoldThreshold = 0.2f;
    private Coroutine inputHoldCoroutine;
    public bool isAttacking = false;
    private bool checkingForInputRelease = false;
    private ComboSystem.Combo currentCombo;
    private AttackSO currentAttackSO;
    private Coroutine comboCoroutine;
    private Coroutine attackCoroutine;
    private bool comboTransition = false; 
    private bool inCombo = false;
    int currAnimAttackState = 0;
    
    [Tooltip("Time required before and after triggering the next attack in combo sequence")]
    [SerializeField] private float comboTimeBeforeThreshold = 0.3f;
    [SerializeField] private float comboTimeEndThreshold = 0.8f;

    [Header("Attack Data")]
    [SerializeField] private List<ComboSystem.Combo> possibleCombos = new List<ComboSystem.Combo>();
    
    public List<AnimationClip> attackClipsList = new List<AnimationClip>();

    private void Awake(){
        _anim = GetComponent<Animator>();
        _playerState = GetComponent<PlayerState>();
        _playerClipsHandler = GetComponent<PlayerClipsHandler>();
    }

    private void Start(){
        GameInput.Instance.OnAttackButtonStarted += AttackButtonStart;
        GameInput.Instance.OnAttackButtonCanceled += AttackButtonCanceled;
        
        foreach(ComboSystem.Combo combo in possibleCombos){
            combo.Initialize();
            foreach(var comboStep in combo.comboSteps){
                attackClipsList.Add(comboStep.attack.attackClip);
            }
        }
    }

    private void OnDisable(){
        GameInput.Instance.OnAttackButtonStarted -= AttackButtonStart;
        GameInput.Instance.OnAttackButtonCanceled -= AttackButtonCanceled;
    }

    private void AttackButtonStart(GameInput.AttackInput inputButton){
        if(!IsOwner){
            return;
        }
        if(!isAttacking || inCombo){
            checkingForInputRelease = true;
            inputHoldCoroutine = StartCoroutine(HandleInputHeldTime(inputButton));
        }
    }

    private void AttackButtonCanceled(GameInput.AttackInput inputButton){
        if(checkingForInputRelease){
            checkingForInputRelease = false;
            StopCoroutine(inputHoldCoroutine);
            if(inputHoldTime < weightedHoldThreshold){
                // Perform a quick attack based on input button type
                currentAttackSO = DecideCurrentAttackSO(inputButton, ComboSystem.AttackPressType.Quick);
                if(currentAttackSO){
                    if(attackCoroutine != null){StopCoroutine(attackCoroutine);}
                    attackCoroutine = StartCoroutine(HandlePerformAttack());
                }
            }
        }
    }

    private IEnumerator HandleInputHeldTime(GameInput.AttackInput inputButton){
        inputHoldTime = 0;
        while(GameInput.Instance.IsAttackButtonPressed(inputButton)){
            inputHoldTime += Time.deltaTime;
            if(inputHoldTime > weightedHoldThreshold){
                currentAttackSO = DecideCurrentAttackSO(inputButton, ComboSystem.AttackPressType.Hold);
                if(currentAttackSO){
                    if(attackCoroutine != null){StopCoroutine(attackCoroutine);}
                    attackCoroutine = StartCoroutine(HandlePerformAttack());
                    checkingForInputRelease = false;
                }
                yield break;
            }
            yield return null;
        }
    }

    private AttackSO DecideCurrentAttackSO(GameInput.AttackInput inputButton, ComboSystem.AttackPressType pressType){
        if(inCombo && currentCombo.currComboStep.userPressType == pressType && currentCombo.currComboStep.userInput == inputButton){
            //Debug.Log("Registered combo input!");
            return currentCombo.currComboStep.attack;
        }
        else if(inCombo){
            //Debug.Log("In combo but wrong input");
            return null;
        }
        else{
            if(!isAttacking){
                //return the attack in which the first attack of that combo matches these inputs
                foreach(ComboSystem.Combo combo in possibleCombos){
                    ComboSystem.ComboStep firstComboStep = combo.comboSteps[0];
                    if(firstComboStep.userInput == inputButton && firstComboStep.userPressType == pressType){
                        //Debug.Log("Found the correct normal attack!");
                        currentCombo = combo;
                        return firstComboStep.attack;
                    }
                }
                //Debug.Log("No Normal attack matching these inputs!");
                return null;
            }
            else{
                //Debug.Log("In attack but past combat window");
                return null;
            }
        }
    }

    private IEnumerator HandlePerformAttack(){
        if(((currentAttackSO.airAttack && _playerState.InAir) || (!currentAttackSO.airAttack && !_playerState.InAir)) && !_playerState.Rolling){
            if(inCombo){ 
                while(!comboTransition){
                    yield return null;
                }
                PerformAttack();
                comboTransition = false;
            }
            else{
                PerformAttack();
                comboTransition = false;
            }
        }
    }

    private void PerformAttack(){
        isAttacking = true;
        _playerState.currentAttack = currentAttackSO;
        _playerState.ChangeAttackStatus(true);
        _playerClipsHandler.HandleAttackClip(GetIndexByAttackSO(currentAttackSO));
        if(currAnimAttackState == 0){_anim.SetTrigger(attackParamA);}
        else { _anim.SetTrigger(attackParamB); }
        currAnimAttackState = (currAnimAttackState + 1) % 2;

        currentCombo.UpdateComboStep();
        inCombo = currentCombo.currIndex > 0;
        if(inCombo){
            if(comboCoroutine != null){StopCoroutine(comboCoroutine);}
            comboCoroutine = StartCoroutine(TrackComboWindow());
        }
    }

    private IEnumerator TrackComboWindow(){
        float elapsedTime = 0f;
        inCombo = false;
        while (elapsedTime < comboTimeBeforeThreshold){
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        inCombo = true;
        while(elapsedTime < comboTimeEndThreshold){
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        inCombo = false;
        currentCombo.ResetComboStep();
    }

    // called by animation event
    private void AttackFinish(){
        isAttacking = false;
        _playerState.ChangeAttackStatus(false);
    }

    // called by animation event
    private void ComboTransfer(){
        comboTransition = true;
    }

    private int GetIndexByAttackSO(AttackSO attackSO){
        int index = attackClipsList.IndexOf(attackSO.attackClip);
        return index;
    }
}