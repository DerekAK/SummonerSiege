using System.Collections;
using UnityEngine;

public class StoneBoulder : BaseAttackScript
{
    [SerializeField] private Transform pfStoneBoulder;
    private Rigidbody rbStone;
    private float rbStoneMass;
    private float rbThrowForceMultiplier;
    private Transform currBoulder;
    //first attacks are only called with current player target, but for one attack that triggers multiple animation events that depend on current player target like this one, need this 
    private Coroutine rotateCoroutine;
    private Transform _rightHand;
    private EnemySpecificInfo _enemyInfo;
    private EnemyAttackManager _enemyAttackManager;
    private bool finishedRotate;

    public override void ExecuteAttack(object sender, EnemyAI4.AttackEvent e){ 
        base.ExecuteAttack(sender, e);
        _enemyScript.AnimationAttackEvent -= ExecuteAttack;
        _enemyScript.AnimationAttackEvent += PickUpBoulder;
        _enemyAttackManager = _enemyGameObject.GetComponent<EnemyAttackManager>();
        _enemyInfo = _enemyGameObject.GetComponent<EnemySpecificInfo>();
        _rightHand = _enemyInfo.GetRightHandTransform();
        StartCoroutine(RotateTowardsPlayerUntilEnd(e.TargetTransform));
    }
    private IEnumerator RotateTowardsPlayerUntilEnd(Transform targetTransform){
        finishedRotate = false;
        while(!finishedRotate){
            _enemyGameObject.transform.LookAt(new Vector3(targetTransform.position.x, _enemyGameObject.transform.position.y, targetTransform.position.z));
            yield return null;
        }
    }
    
    private void OnDisable(){
        if(currBoulder){Destroy(currBoulder.gameObject);}
    }
    private void PickUpBoulder(object sender, EnemyAI4.AttackEvent e){
        _enemyScript.AnimationAttackEvent -= PickUpBoulder;
        _enemyScript.AnimationAttackEvent += ReleaseBoulder;
        if(currBoulder){Destroy(currBoulder.gameObject);}
        currBoulder = Instantiate(pfStoneBoulder, _rightHand.position, Quaternion.identity);
        rbStone = currBoulder.GetComponent<Rigidbody>();
        
        UtilityFunctions.SetParentOfTransform(currBoulder, _rightHand, GetFirstWeaponPositionOffset(), GetFirstWeaponRotationOffset());
        rbStone.isKinematic = true;
    }

    private void ReleaseBoulder(object sender, EnemyAI4.AttackEvent e){ 
        rbStone.isKinematic = false;
        _enemyScript.AnimationAttackEvent -= ReleaseBoulder;
        currBoulder.SetParent(null);
        Vector3 playerPosition = e.TargetTransform.position;
        finishedRotate = true;
        UtilityFunctions.ThrowWithRigidbody(currBoulder.gameObject, _rightHand.position, playerPosition, 200f, 10f);
    }
}
