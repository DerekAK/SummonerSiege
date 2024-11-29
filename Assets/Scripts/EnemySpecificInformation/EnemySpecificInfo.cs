using UnityEngine;

public class EnemySpecificInfo : MonoBehaviour
{
   [SerializeField] protected Transform _rightHandTransform;
   public Transform GetRightHandTransform(){return _rightHandTransform;}
   [SerializeField] protected Transform _swordAttachPointTransform;
   public Transform GetSwordAttachPointTransform(){return _swordAttachPointTransform;}

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
   [SerializeField] protected float animChangeProbability;
   public float GetAnimChangeProbability(){
      return animChangeProbability;
   }
}
