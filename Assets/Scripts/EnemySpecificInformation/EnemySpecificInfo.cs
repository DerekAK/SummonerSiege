using UnityEngine;

public class EnemySpecificInfo : MonoBehaviour
{
   [SerializeField] private Transform _rightHandTransform;
   public Transform GetRightHandTransform(){return _rightHandTransform;}
   [SerializeField] private Transform _leftHandTransform;
   public Transform GetLeftHandTransform(){return _leftHandTransform;}
   [SerializeField] private Transform _singleWeaponAttachPointTransform;
   public Transform GetSingleWeaponAttachPointTransform(){return _singleWeaponAttachPointTransform;}
   [SerializeField] private Transform _doubleWeaponAttachPointTransform1;
   public Transform GetDoubleWeaponAttachPointTransform1(){return _doubleWeaponAttachPointTransform1;}
   [SerializeField] private Transform _doubleWeaponAttachPointTransform2;
   public Transform GetDoubleWeaponAttachPointTransform2(){return _doubleWeaponAttachPointTransform2;}
   [SerializeField] private float weaponSpawnProbability;
   public float GetWeaponSpawnProbability(){return weaponSpawnProbability;}
   [SerializeField] private float chainProbability;
   public float GetChainProbability(){return chainProbability;}
   [SerializeField] private float attackWaitTime;
   public float GetWaitTimeAfterAttack(){return attackWaitTime;}
   [SerializeField] private float chaseGiveUpTime;
   public float GetChaseGiveUpTime(){return chaseGiveUpTime;}
   [SerializeField] private float animChangeProbability;
   public float GetAnimChangeProbability(){return animChangeProbability;}
   [SerializeField] private float detectionTime;
   public float GetDetectionTime(){return detectionTime;}



   [SerializeField] private float aggroDistance;
   public float GetAggroDistance(){return aggroDistance;}
   [SerializeField] private float minRoamingRange;
   public float GetMinRoamingRange(){return minRoamingRange;}
   [SerializeField] private float maxRoamingRange;
   public float GetMaxRoamingRange(){return maxRoamingRange;}
   [SerializeField] private float minIdleTime;
   public float GetMinIdleTime(){return minIdleTime;}
   [SerializeField] private float maxIdleTime;
   public float GetMaxIdleTime(){return maxIdleTime;}
   [SerializeField] private float roamingSpeed;
   public float GetRoamingSpeed(){return roamingSpeed;}
   [SerializeField] private float approachSpeed;
   public float GetApproachSpeed(){return approachSpeed;}
   [SerializeField] private float turnSpeed;
   public float GetTurnSpeed(){return turnSpeed;}
   [SerializeField] private float stareDownTime;
   public float GetStareDownTime(){return stareDownTime;}
   [SerializeField] private float circlingSpeed;
   public float GetCirclingSpeed(){return circlingSpeed;}
   [SerializeField] private float retreatDistance;
   public float GetRetreatDistance(){return retreatDistance;}
   [SerializeField] private float retreatSpeed;
   public float GetRetreatSpeed(){return retreatSpeed;}
   [SerializeField] private float repositionSpeed;
   public float GetRepositionSpeed(){return repositionSpeed;}

   [SerializeField] private float aggression;
   public float GetAggression(){return aggression;}
   [SerializeField] private float meleeAffinity;
   public float GetMeleeAffinity(){return meleeAffinity;}
   [SerializeField] private float mediumAffinity;
   public float GetMediumAffinity(){return mediumAffinity;}
   [SerializeField] private float rangedAffinity;
   public float GetRangedAffinity(){return rangedAffinity;}
}
