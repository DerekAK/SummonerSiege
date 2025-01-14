using UnityEngine;


/**
Responsible for handling all animation transitions of the player
*/
public class PlayerClipsHandler : MonoBehaviour{
    
    private Animator _anim;
    private AnimatorOverrideController _templateOverrider;

    [SerializeField] private AnimationClip unarmedIdleClip;
    [SerializeField] private AnimationClip unarmedWalkForwardClip;
    [SerializeField] private AnimationClip unarmedWalkForwardLeftClip;
    [SerializeField] private AnimationClip unarmedWalkForwardRightClip;
    [SerializeField] private AnimationClip unarmedWalkBackwardClip;
    [SerializeField] private AnimationClip unarmedWalkBackwardLeftClip;
    [SerializeField] private AnimationClip unarmedWalkBackwardRightClip;
    [SerializeField] private AnimationClip unarmedRunForwardClip;
    [SerializeField] private AnimationClip unarmedRunForwardLeftClip;
    [SerializeField] private AnimationClip unarmedRunForwardRightClip;
    [SerializeField] private AnimationClip unarmedRunBackwardClip;
    [SerializeField] private AnimationClip unarmedRunBackwardLeftClip;
    [SerializeField] private AnimationClip unarmedWRunBackwardRightClip;
    [SerializeField] private AnimationClip unarmedStrafeLeftClip;
    [SerializeField] private AnimationClip unarmedStrafeRightClip;

    private void Awake(){
        
    }

}
