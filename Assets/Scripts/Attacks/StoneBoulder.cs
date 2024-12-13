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

    private void Awake(){
        _enemyScript = GetComponent<EnemyAI3>();
        _enemyAttackManager = GetComponent<EnemyAttackManager>();
        OverrideClip();
        _enemyInfo = GetComponent<EnemySpecificInfo>();
    }
    private void Start(){
        _rightHand = _enemyInfo.GetRightHandTransform();
    }
    public override void ExecuteAttack(object sender, EnemyAI3.AttackEvent e){ //can be null here 
        _enemyScript.AnimationAttackEvent -= ExecuteAttack;
        _enemyScript.AnimationAttackEvent += PickUpBoulder;
    }
    private void PickUpBoulder(object sender, EnemyAI3.AttackEvent e){
        _enemyScript.AnimationAttackEvent -= PickUpBoulder;
        _enemyScript.AnimationAttackEvent += ReleaseBoulder;
        if(currBoulder){Destroy(currBoulder.gameObject);}
        currBoulder = Instantiate(pfStoneBoulder, _rightHand.position, Quaternion.identity);
        _enemyAttackManager.SetParentOfTransform(currBoulder, _rightHand, GetFirstWeaponPositionOffset(), GetFirstWeaponRotationOffset());
        float enemyLocalScale = transform.root.localScale.x;
        currBoulder.localScale *= enemyLocalScale;
        rbStone = currBoulder.GetComponent<Rigidbody>();
        rbStoneMass = rbStone.mass;
        rbThrowForceMultiplier = rbStoneMass * 120;
        rbStone.isKinematic = true;
        currBoulder.GetComponent<SphereCollider>().enabled = false;
    }

    private void ReleaseBoulder(object sender, EnemyAI3.AttackEvent e){ //subscriber to the Attack3 event in enemyai3script
        _enemyScript.AnimationAttackEvent -= ReleaseBoulder;
        currBoulder.GetComponent<SphereCollider>().enabled = true;
        rbStone.isKinematic = false;
        currBoulder.SetParent(null);
        Vector3 playerPosition;
        playerPosition = e.TargetTransform.position;
        float distanceToPlayer = Vector3.Distance(playerPosition, transform.position);
        Vector3 directionToPlayer = (playerPosition+(Vector3.up*(distanceToPlayer/10f)) - _rightHand.position).normalized;
        rbStone.AddForce(directionToPlayer * rbThrowForceMultiplier, ForceMode.Impulse);
    }
}
