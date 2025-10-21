// The new, more abstract version of AttackAction
using System.Collections.Generic;
using UnityEngine;

public abstract class Intention : ScriptableObject
{
    public List<Consideration> considerations;

    // The scoring logic remains the same.
    public virtual float ScoreIntention(BehaviorManager ai)
    {
        float totalScore = 1f;
        foreach (Consideration consideration in considerations)
        {
            float score = consideration.Evaluate(ai);
            if (score == 0) return 0; // Optimization: if any consideration is 0, the whole behavior is invalid
            totalScore *= score;
        }
        return totalScore;
    }

    // This is the key change: This method tells the FSM what to do.
    public abstract void Execute(BehaviorManager ai);
}