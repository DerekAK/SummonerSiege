// In a new file: AttackAction.cs
using UnityEngine;
using System.Collections.Generic;

public abstract class EnemyAttackSO : BaseAttackSO
{
    [Header("Attack Properties")]
    public float MinRange;
    public float MaxRange;
    public float Damage;

    [Header("Scoring Logic")]
    [Tooltip("The considerations specific to choosing THIS attack.")]
    public List<Consideration> considerations;

    // This method scores ONLY this specific attack.
    public float ScoreAction(BehaviorManager ai)
    {
        float totalScore = 1f;
        foreach (var consideration in considerations)
        {
            float score = consideration.Evaluate(ai);
            if (score == 0) return 0; // Can't perform this attack, score is 0.
            totalScore *= score;
        }
        return totalScore;
    }
}