using UnityEngine;

/*
    Description: Will set speed to 0 before attacking
*/

[CreateAssetMenu(fileName = "SimpleAttackState", menuName = "Scriptable Objects/AI Behavior/States/Attack/SimpleAttackState")]
public class SimpleAttackState : BaseAttackState
{

    public override void EnterState(BehaviorManager behaviorManager)
    {
        behaviorManager.HandleSpeedChangeWithValue(0);
        base.EnterState(behaviorManager);
    }

}