using UnityEngine;

public class EnemySpecificInfo : MonoBehaviour
{
   protected Transform _rightHandTransform;
   public Transform GetRightHandTransform(){return _rightHandTransform;}

   [SerializeField] protected float chainProbability;
   public float GetChainProbability(){
      return chainProbability;
   }
   [SerializeField] protected float attackWaitTime;
   public float GetWaitTimeAfterAttack(){
      return attackWaitTime;
   }
   [SerializeField] protected float chaseGiveUpTime;
   public float GetChaseGiveUpTime(){
      return chaseGiveUpTime;
   }
}
