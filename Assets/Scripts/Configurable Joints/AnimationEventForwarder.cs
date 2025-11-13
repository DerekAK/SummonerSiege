using UnityEngine;

public class AnimationEventForwarder : MonoBehaviour
{
    private PlayerCombat playerCombat;
    private PlayerMovement playerMovement;
    
    private void Awake()
    {
        playerCombat = GetComponentInParent<PlayerCombat>();
        playerMovement = GetComponentInParent<PlayerMovement>();
        
        if (playerCombat == null)
            Debug.LogError("AnimationEventForwarder couldn't find PlayerCombat in parent!");
        if (playerMovement == null)
            Debug.LogError("AnimationEventForwarder couldn't find PlayerMovement in parent!");
    }
    
    // Combat events (make sure these are NOT private in the original scripts)
    public void AnimationEvent_EnableHitBoxes()
    {
        playerCombat?.AnimationEvent_EnableHitBoxes();
    }
    
    public void AnimationEvent_DisableHitBoxes()
    {
        playerCombat?.AnimationEvent_DisableHitBoxes();
    }
    
    public void AnimationEvent_AttackFinished()
    {
        playerCombat?.AnimationEvent_AttackFinished();
    }
    
    public void AnimationEvent_ComboTransfer()
    {
        playerCombat?.AnimationEvent_ComboTransfer();
    }
    
    public void AnimationEvent_Trigger(int numEvent)
    {
        playerCombat?.AnimationEvent_Trigger(numEvent);
    }

    // Movement events
    public void AnimationEvent_EndRoll()
    {
        playerMovement?.AnimationEvent_EndRoll();
    }
    
    public void AnimationEnent_HoldTransfer()
    {
        playerCombat.AnimationEvent_HoldTransfer();
    }
}