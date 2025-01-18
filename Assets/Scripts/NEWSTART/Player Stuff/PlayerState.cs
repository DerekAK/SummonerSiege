using System.Collections.Generic;
using UnityEngine;

public class PlayerState : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private Animator _anim; 
    private PlayerMovement _playerMovement;
    [SerializeField] private string lockOnTargetTag;
    public List<Transform> lockOnTargets {get; private set;} = new List<Transform>();

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
        ReleaseChargeLong
    }
    public NonMovementState nonMovementState {get; private set;}

    private void OnTriggerEnter(Collider other){
        if(other.CompareTag(lockOnTargetTag)){lockOnTargets.Add(other.transform);}
    }
    
    private void OnTriggerExit(Collider other){
        if(other.CompareTag(lockOnTargetTag)){lockOnTargets.Remove(other.transform);}
    }

    public NonMovementState currentPlayerState{get; private set;}
//     private bool isAttacking()=>currentPlayerState == NonMovementState.Attacking;
//     private bool is

//     private void Awake(){
//         _anim = GetComponent<Animator>();
//         _playerMovement = GetComponent<PlayerMovement>();
//     }

//     private void Update(){
//         if(GameInput.Instance.LeftClickPressed() && !isAttacking()){

//         }
        
//     }


//     private void HandleAnimatorState(){
//         int movementLayerIndex = 0;
//         int meleeLayerIndex = 1;
//         int longRangeLayerIndex = 2;

//         _anim.SetInteger
//         _anim.SetLayerWeight
//         _anim.SetFloat

//         if(isAttacking){_anim.SetLayerWeight(meleeLayerIndex, 1);}


//         //override all the time 

//         //override depends: melee attacks

        
        

//     }



}
