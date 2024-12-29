using UnityEngine;
using System.Collections;
public class Sword2HandedDefault : BaseAttackScript{   
    private bool endRotate;
    
    public override void ExecuteAttack(object sender, EnemyAI4.AttackEvent e){ 
        endRotate = false;
        _enemyScript.AnimationAttackEvent -= ExecuteAttack;
        _enemyScript.AnimationAttackEvent += EndRotate;
        StartCoroutine(RotateTowardsPlayer(e.TargetTransform));
    }
    private IEnumerator RotateTowardsPlayer(Transform targetTransform){
        while (!endRotate){
            _enemyGameObject.transform.LookAt(new Vector3(targetTransform.position.x, transform.position.y, targetTransform.position.z));
            yield return null;
        }
    }
    protected void EndRotate(object sender, EnemyAI4.AttackEvent e){ 
        _enemyScript.AnimationAttackEvent -= EndRotate;
        endRotate = true;
    }
}
