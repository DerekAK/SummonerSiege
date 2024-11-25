using System.Collections;
using UnityEngine;

public class MageShockWave : BaseAttackScript
{

    private bool hasCrashedDown;
    private EnemyAI3 _enemyScript;
    private Transform _rightHand;

    private void Awake(){
        _enemyScript = GetComponent<EnemyAI3>();
        clipToOverride = "Attack" +  attackType.ToString() + " Placeholder";
    }
    public override void ExecuteAttack(object sender, EnemyAI3.AttackEvent e){
        _enemyScript.AnimationAttackEvent -= ExecuteAttack;
        _enemyScript.AnimationAttackEvent += EndRotation;
        Debug.Log("Do Something!");

        EnemySpecificInfo enemyInfo = GetComponent<EnemySpecificInfo>();
        _rightHand = enemyInfo.GetRightHandTransform();
        //Transform playerTransform = e.TargetTransform;
        //playerTransform.gameObject.GetComponent<CharacterController>().enabled = false;
        //playerTransform.SetParent(_rightHand);
        hasCrashedDown = false;
        StartCoroutine(RotateTowardsPlayerUntilCrash(e.TargetTransform));
    }

    private IEnumerator RotateTowardsPlayerUntilCrash(Transform targetTransform)
    {
        while (!hasCrashedDown)
        {
            transform.LookAt(new Vector3(targetTransform.position.x, transform.position.y, targetTransform.position.z));
            yield return null;
        }
    }

    private void EndRotation(object sender, EnemyAI3.AttackEvent e){ 
        _enemyScript.AnimationAttackEvent -= EndRotation;
        hasCrashedDown = true;
    }
}
