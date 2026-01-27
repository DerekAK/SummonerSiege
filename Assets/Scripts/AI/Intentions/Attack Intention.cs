using Unity.Entities.UniversalDelegates;
using Unity.VisualScripting;
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

        foreach (EnemyAttackSO attack in combatManager.AvailableAttacks)
        {
            float score = attack.ScoreAttack(ai);
            if (score > highestScore && attack.CanExecuteAttack(ai.GetComponent<CombatManager>()))
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

    public override bool CanExecute(BehaviorManager ai)
    {
        if (ai.CurrentTarget == null) return false;

        // we have to check if inattack here rather than if the state is in attacking, because 
        // we can be in an attack state and still want to trigger a new attack. 
        // we can attack multiple times and still be in the attacking state the entire time,
        // but the InAttack bool will be false for a slight moment (within the same frame)
        if (ai.GetComponent<EnemyCombat>().InAttack) return false;

        return true;
    }

    public override void Execute(BehaviorManager behaviorManager)
    {
        behaviorManager.SwitchState(behaviorManager.AttackState);
    }
}