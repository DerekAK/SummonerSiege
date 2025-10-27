using Unity.Netcode;
using UnityEngine;


/**
Responsible for handling all animation transitions of the player
*/
public class PlayerClipsHandler : NetworkBehaviour{

    private Animator _anim;
    private PlayerCombat _playerCombat;
    private AnimatorOverrideController _templateOverrider;
    private AnimatorOverrideController _copyOverrider;

    [Header("Movement Animations")]
    [Header("Movement Animations")]
    [SerializeField] private AnimationClip unarmedIdleClip;
    [SerializeField] private AnimationClip unarmedStrafeIdleClip;
    [SerializeField] private AnimationClip unarmedWalkForwardClip;
    [SerializeField] private AnimationClip unarmedWalkForwardLeftClip;
    [SerializeField] private AnimationClip unarmedWalkForwardRightClip;
    [SerializeField] private AnimationClip unarmedWalkBackwardClip;
    [SerializeField] private AnimationClip unarmedWalkBackwardLeftClip;
    [SerializeField] private AnimationClip unarmedWalkBackwardRightClip;
    [SerializeField] private AnimationClip unarmedRunForwardClip;
    [SerializeField] private AnimationClip unarmedStrafeLeftClip;
    [SerializeField] private AnimationClip unarmedStrafeRightClip;
    [SerializeField] private AnimationClip unarmedRollForwardClip;
    [SerializeField] private AnimationClip unarmedRollBackwardClip;
    [SerializeField] private AnimationClip unarmedRollLeftlip;
    [SerializeField] private AnimationClip unarmedRollRightClip;
    [SerializeField] private AnimationClip unarmedCrouchClip;

    private void Awake(){
        _anim = GetComponent<Animator>();
        _playerCombat = GetComponent<PlayerCombat>();
        _templateOverrider = (AnimatorOverrideController)_anim.runtimeAnimatorController;
        _copyOverrider = new AnimatorOverrideController(_templateOverrider);
        _anim.runtimeAnimatorController = _copyOverrider;
    }

    private void Start(){
        // Using Start for this is fine as it's not dependent on network state
        // and needs to run on all clients regardless of ownership.
        HandleMovementClips();
    }

    private void HandleMovementClips(){
        _copyOverrider["Idle Placeholder"] = unarmedIdleClip;
        _copyOverrider["Strafe Idle Placeholder"] = unarmedStrafeIdleClip;
        _copyOverrider["Run Forward Placeholder"] = unarmedRunForwardClip;
        _copyOverrider["Walk Forward Placeholder"] = unarmedWalkForwardClip;
        _copyOverrider["Strafe Forward Left Placeholder"] = unarmedWalkForwardLeftClip;
        _copyOverrider["Strafe Forward Right Placeholder"] = unarmedWalkForwardRightClip;
        _copyOverrider["Strafe Backward Placeholder"] = unarmedWalkBackwardClip;
        _copyOverrider["Strafe Backward Left Placeholder"] = unarmedWalkBackwardLeftClip;
        _copyOverrider["Strafe Backward Right Placeholder"] = unarmedWalkBackwardRightClip;
        _copyOverrider["Strafe Left Placeholder"] = unarmedStrafeLeftClip;
        _copyOverrider["Strafe Right Placeholder"] = unarmedStrafeRightClip;
        _copyOverrider["Roll Forward Placeholder"] = unarmedRollForwardClip;
        _copyOverrider["Roll Backward Placeholder"] = unarmedRollBackwardClip;
        _copyOverrider["Roll Left Placeholder"] = unarmedRollLeftlip;
        _copyOverrider["Roll Right Placeholder"] = unarmedRollRightClip;
        _copyOverrider["Crouch Frame Placeholder"] = unarmedCrouchClip;
    }
}