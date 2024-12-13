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

   [SerializeField] private float chainProbability;
   public float GetChainProbability(){return chainProbability;}
   [SerializeField] private float attackWaitTime;
   public float GetWaitTimeAfterAttack(){return attackWaitTime;}
   [SerializeField] private float chaseGiveUpTime;
   public float GetChaseGiveUpTime(){return chaseGiveUpTime;}
   [SerializeField] private float animChangeProbability;
   public float GetAnimChangeProbability(){return animChangeProbability;}
   [SerializeField] private float weaponSpawnProbability;
   public float GetWeaponSpawnProbability(){return weaponSpawnProbability;}
}
