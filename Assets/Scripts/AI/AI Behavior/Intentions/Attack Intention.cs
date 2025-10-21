// In a new file: AttackIntention.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackIntention", menuName = "Scriptable Objects/AI Behavior/Intentions/Attack")]
public class AttackIntention : Intention
{
    // This intention doesn't need its own considerations.
    // Its score is derived from the best available attack.
    [Header("IGNORE CONSIDERATIONS AND THIS FIELD!")]
    [SerializeField] private string deezNuts;
    public override float ScoreIntention(BehaviorManager ai)
    {
        // 1. Find the best possible attack in the current situation.
        EnemyCombat combatManager = ai.GetComponent<EnemyCombat>();

        EnemyAttackSO bestAttack = null;
        float highestScore = 0f;

        foreach (var attack in combatManager.AvailableAttacks)
        {
            float score = attack.ScoreAction(ai);
            if (score > highestScore)
            {
                highestScore = score;
                bestAttack = attack;
            }
        }

        // 2. "Remember" which attack we chose so we can execute it later.
        combatManager.ChosenAttack = bestAttack;

        // 3. The overall score for "Attacking" is the score of our best attack option.
        return highestScore;
    }

    public override void Execute(BehaviorManager ai)
    {
        float distance = Vector3.Distance(ai.transform.position, ai.CurrentTarget.transform.position);
        //Debug.Log(distance);

        EnemyAttackSO chosenAttack = (EnemyAttackSO)ai.GetComponent<EnemyCombat>().ChosenAttack;
        // 1. If we are in range, execute the AttackState
        if (distance >= chosenAttack.MinRange && distance <= chosenAttack.MaxRange)
        {
            ai.SwitchState(ai.AttackState);
        }
        // 2. If we are too far, execute the ChasingState to close the distance
        else if (distance > chosenAttack.MaxRange)
        {
            if (ai.CurrentState is BaseChasingState)
            {
                return; // We're already doing the right thing. Do nothing.
            }
            ai.SwitchState(ai.ChasingState);
        }

        else // make this some reposition state
        {
            ai.DecideNextIntention();
        }
    }
}