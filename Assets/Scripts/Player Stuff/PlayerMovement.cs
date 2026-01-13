using Unity.Netcode;

public abstract class PlayerMovement: NetworkBehaviour
{
    protected bool inAir;
    public bool InAir => inAir;

    protected bool isRolling;
    public bool IsRolling => isRolling;


    public abstract void AnimationEvent_EndRoll();
}
