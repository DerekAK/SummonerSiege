// Base class for all conditions
using UnityEngine;

public abstract class Consideration : ScriptableObject
{
    // Returns a score from 0 (don't do it) to 1 (perfectly suited).
    public abstract float Evaluate(BehaviorManager ai);
}
