using System.Collections;
using UnityEngine;
/*
IMPORTANT TO UNDERSTAND: any attack that might possibly lead into a combo must be in a combo. in other words,
there can't be two different viable attacks on a player that both take in the same input, because otherwise 
the computer wouldn't know which one to use (no shit). so basically, need to first figure out which inputs we're 
listening for, and then only have one viable attack from that starting input at any time. in other words, at any
given point, no matter the state, there is only one viable next move for any single input.
*/
public class PlayerCombat : MonoBehaviour
{
    private Animator _anim;
    private PlayerState _playerState;
    private PlayerClipsHandler _playerClipsHandler;
    private int attackParamA = Animator.StringToHash("AttackTriggerA");
    private int attackParamB = Animator.StringToHash("AttackTriggerB");
    private float leftClickHoldTime = 0f;
    private float weightedHoldThreshold = 0.2f;
    private Coroutine leftClickHoldCoroutine;
    public bool isAttacking = false;
    private bool checkingForLeftClickRelease = false;
    private ComboSystem.Combo currentCombo;
    private AttackSO currentAttackSO;
    private Coroutine comboCoroutine;
    private Coroutine attackCoroutine;
    private bool comboTransition = false; //boolean for tracking when to transfer over to next combo
    private bool inCombo = false;
    int currAnimAttackState = 0; //0 or 1 for combo states in animator

    [Header("Attack Data")]
    // everything is a combo, worry about how to add combos later, but the idea is that 
    // weapons will come with combos so you can just access it from weapon or other means
    [SerializeField] private ComboSystem.Combo unarmedQuickAttackCombo;
    [SerializeField] private ComboSystem.Combo unarmedHeavyAttackCombo;


    [Tooltip("Time required before and after triggering the next attack in combo sequence")]
    [SerializeField] private float comboTimeBeforeThreshold = 0.3f;
    [SerializeField] private float comboTimeEndThreshold = 0.8f;

    private void Awake(){
        _anim = GetComponent<Animator>();
        _playerState = GetComponent<PlayerState>();
        _playerClipsHandler = GetComponent<PlayerClipsHandler>();
    }

    private void Start(){
        GameInput.Instance.OnLeftClickStarted += LeftClickStart;
        GameInput.Instance.OnLeftClickCanceled += LeftClickCanceled;
        unarmedQuickAttackCombo.Initialize();
        unarmedHeavyAttackCombo.Initialize();
    }

    private void Update(){
        Debug.Log(inCombo);
    }

    private void OnDisable(){
        GameInput.Instance.OnLeftClickStarted -= LeftClickStart;
        GameInput.Instance.OnLeftClickCanceled -= LeftClickCanceled;
    }
  
    private void LeftClickStart(){
        //only want to start checking for an attack if he's either not attacking at all or if he's not in the combo window 
        if(!isAttacking || inCombo){
            checkingForLeftClickRelease = true;
            leftClickHoldCoroutine = StartCoroutine(LeftClickHeldTime());
        }
    }

    private void LeftClickCanceled(){
        if(checkingForLeftClickRelease){
            checkingForLeftClickRelease = false;
            StopCoroutine(leftClickHoldCoroutine);
            if(leftClickHoldTime < weightedHoldThreshold){
                //want to perform a quick attack. that quick attack could be a normal quick attack or a quick attack in a combo if currently in a combo
                currentAttackSO = DecideCurrentAttackSO(ComboSystem.AttackTrigger.LeftClickQuick);
                if(currentAttackSO){
                    if(attackCoroutine != null){StopCoroutine(attackCoroutine);}
                    attackCoroutine = StartCoroutine(HandlePerformAttack());
                }
            }
        }
    }

    private IEnumerator LeftClickHeldTime(){
        leftClickHoldTime = 0;
        while(GameInput.Instance.LeftClickPressed()){
            leftClickHoldTime += Time.deltaTime;
            if(leftClickHoldTime > weightedHoldThreshold){
                currentAttackSO = DecideCurrentAttackSO(ComboSystem.AttackTrigger.LeftClickHold);
                if(currentAttackSO){
                    if(attackCoroutine != null){StopCoroutine(attackCoroutine);}
                    attackCoroutine = StartCoroutine(HandlePerformAttack());
                    checkingForLeftClickRelease = false; //because after this heavy attack is performed, 
                }
                yield break;
            }
            yield return null;
        }
    }

    private AttackSO DecideCurrentAttackSO(ComboSystem.AttackTrigger trigger){
        //if in combo window and input correct combo input
        if(inCombo && currentCombo.currComboStep.trigger == trigger){
            Debug.Log("registered combo input!");
            return currentCombo.currComboStep.attack;
        }
        //if in combo and input wrong combo input
        else if(inCombo){
            Debug.Log("in combo but wrong input");
            return null;
        }
        
        else{
            //if not in combo window and not attacking, so just not in combat basically
            if(!isAttacking){
                Debug.Log("normal attack");
                switch(trigger){
                    case ComboSystem.AttackTrigger.LeftClickQuick:
                        currentCombo = unarmedQuickAttackCombo;
                        break;
                    case ComboSystem.AttackTrigger.LeftClickHold:
                        currentCombo = unarmedHeavyAttackCombo;
                        break;
                    default:
                        return unarmedQuickAttackCombo.comboSteps[0].attack;
                }
                return currentCombo.comboSteps[0].attack;
            }
            //if not in combo window but in an attack, meaning that you are past the combo window, so don't want to register any attacks
            else{ 
                Debug.Log("in attack but past combat window");
                return null;
            }
        }
    }

    private IEnumerator HandlePerformAttack(){
        if(((currentAttackSO.airAttack && _playerState.InAir) || (!currentAttackSO.airAttack && !_playerState.InAir)) && !_playerState.Rolling){
            if(inCombo){ //in combo
                while(!comboTransition){
                    yield return null;
                }
                PerformAttack();
                comboTransition = false;
            }
            else{
                PerformAttack();}
                comboTransition = false;
        }
    }
    private void PerformAttack(){
        isAttacking = true;
        _playerState.currentAttack = currentAttackSO; //this has to go before because changeattackstatus depends on this somehow
        _playerState.ChangeAttackStatus(true);
        _playerClipsHandler.HandleAttackClip(currentAttackSO);
        if(currAnimAttackState == 0){_anim.SetTrigger(attackParamA);}
        else{_anim.SetTrigger(attackParamB);}
        currAnimAttackState = (currAnimAttackState + 1) % 2;

        currentCombo.UpdateComboStep(); //will update the currcombostep for this specific combo
        //if it updated to something other than zero, we know that next attack is a combo
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

    //called by attack events
    private void AttackFinish(){
        isAttacking = false;
        _playerState.ChangeAttackStatus(false);
    }
    private void ComboTransfer(){comboTransition = true;}
}

//pseudocode
/*
left click start: decide if taking in any input at this time (if !attacking or inside of a combo window)
checking for left click release = true
start left click holding coroutine

left click release: if release and didn't reach threshold, perform light attack

left click holding: if reaches threshold, perform heavy attack

perform attack:
attacking = true until attack finishes

if the current attack is part of a combo (which is if the index of the attack just performed < the count of the combo-1), start the combo timer to check for the correct input for that combo

if the combo window runs out, we set combowindow to closed






*/
