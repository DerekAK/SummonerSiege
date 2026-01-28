// In a new file: AttackAction.cs
using UnityEngine;
using System.Collections.Generic;
using NUnit.Framework;

public abstract class EnemyAttackSO : BaseAttackSO
{
    [Header("Attack Properties")]
    public float MinRange;
    public float MaxRange;

    [Header("Scoring Logic")]
    [Tooltip("The considerations specific to choosing THIS attack.")]
    public List<Consideration> considerations;

    // This method scores ONLY this specific attack.
    public float ScoreAttack(BehaviorManager ai)
    {
        float totalScore = 0.5f;
        foreach (var consideration in considerations)
        {
            float score = consideration.Evaluate(ai);
            if (score == 0) return 0; // Can't perform this attack, score is 0.
            totalScore *= score;
        }
        return totalScore;
    }

    // specifically for this attack
    public override bool CanExecuteAttack(CombatManager combatManager)
    {
        // check shared parent cases first
        if (!base.CanExecuteAttack(combatManager)) {
            Debug.Log("Failed parent check!");
            return false;
        }
        
        // distance check
        BehaviorManager ai = combatManager.GetComponent<BehaviorManager>();
        float distance = Vector3.Distance(ai.transform.position, ai.CurrentTarget.transform.position);
        if (distance < MinRange || distance > MaxRange) {
            Debug.Log("Failed child check!");
            return false;
        }
        
        return true;
    }
}