// The new, more abstract version of AttackAction
using System.Collections.Generic;
using UnityEngine;

public abstract class Intention : ScriptableObject
{
    public List<Consideration> considerations;

    // The scoring logic remains the same.
    public virtual float ScoreIntention(BehaviorManager ai)
    {
        float totalScore = 0.5f; // 0.5 is neutral
        foreach (Consideration consideration in considerations)
        {
            float score = consideration.Evaluate(ai);
            if (score == 0) return 0; // Optimization: if any consideration is 0, the whole behavior is invalid
            totalScore *= score;
        }
        return totalScore;
    }


    // this function is for specific logic that should prevent this intention from being executed
    // that cannot or would not make sense for it to go into a consideration
    // a common example in my intentions is if the current state is already the state associated
    // with this intention
    public abstract bool CanExecute(BehaviorManager ai);
    public abstract void Execute(BehaviorManager ai);
}