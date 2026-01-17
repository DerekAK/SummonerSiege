// In a new file: AttackIntention.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
        combatManager.SetChosenAttack(bestAttack);

        // 3. The overall score for "Attacking" is the score of our best attack option.
        return highestScore;
    }

    public override void Execute(BehaviorManager behaviorManager)
    {
        float distance = Vector3.Distance(behaviorManager.transform.position, behaviorManager.CurrentTarget.transform.position);
        //Debug.Log(distance);

        EnemyAttackSO chosenAttack = (EnemyAttackSO)behaviorManager.GetComponent<EnemyCombat>().ChosenAttack;

        if (chosenAttack)
        {
           if (distance >= chosenAttack.MinRange && distance <= chosenAttack.MaxRange && behaviorManager.GetComponent<BehaviorManager>().CanAttack())
            {
                behaviorManager.SwitchState(behaviorManager.AttackState);
            }
            // 2. If we are not in correct distance, execute the ChasingState to close the distance
            else if (distance > chosenAttack.MaxRange)
            {
                if (behaviorManager.CurrentState is BaseChasingState)
                {
                    return; // We're already doing the right thing. Do nothing.
                }
                behaviorManager.SwitchState(behaviorManager.ChasingState);
            }

            else
            {
                behaviorManager.DecideNextIntention();
                Debug.LogWarning("Enemy did not successfully attack on an attack intention!");
            }
        }
    }
}