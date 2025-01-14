using Unity.VisualScripting;
using UnityEngine;

public class PlayerState : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private Animator _anim; 
    private PlayerMovement _playerMovement;
    public enum NonMovementState{
        Crouch,
        Roll,
        Block,
        Parry,
        QuickMelee,
        ChargeMelee,
        ReleaseChargeMelee,
        QuickLong,
        ChargeLong,
        ReleaseChargeLong,
        

    }
    public NonMovementState currentPlayerState{get; private set;}
    private bool isAttacking()=>currentPlayerState == NonMovementState.Attacking;
    private bool is

    private void Awake(){
        _anim = GetComponent<Animator>();
        _playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update(){
        if(GameInput.Instance.LeftClickPressed() && !isAttacking()){

        }
        
    }


    private void HandleAnimatorState(){
        int movementLayerIndex = 0;
        int meleeLayerIndex = 1;
        int longRangeLayerIndex = 2;

        _anim.SetInteger
        _anim.SetLayerWeight
        _anim.SetFloat

        if(isAttacking){_anim.SetLayerWeight(meleeLayerIndex, 1);}


        //override all the time 

        //override depends: melee attacks

        
        

    }



}
