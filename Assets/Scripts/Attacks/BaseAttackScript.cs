using UnityEngine;
public abstract class BaseAttackScript : MonoBehaviour
{
    protected EnemyAI4 _enemyScript;
    protected GameObject _enemyGameObject;
    
    //attack specific information
    [Tooltip("Can be empty for non-weapon attacks! Also only fill up the field that is necessary for that attack. To figure out offset, attach the weapon(s) to the respective hand transform and rotate/position weapon until works with animation, and record offset and position here")]
    [SerializeField] private Vector3 weaponPositionOffsetFirstWeapon;
    public Vector3 GetFirstWeaponPositionOffset(){return weaponPositionOffsetFirstWeapon;}
    [SerializeField] private Vector3 weaponRotationOffsetFirstWeapon;
    public Vector3 GetFirstWeaponRotationOffset(){return weaponRotationOffsetFirstWeapon;}
    [SerializeField] private Vector3 weaponPositionOffsetSecondWeapon;
    public Vector3 GetSecondWeaponPositionOffset(){return weaponPositionOffsetSecondWeapon;}
    [SerializeField] private Vector3 weaponRotationOffsetSecondWeapon;
    public Vector3 GetSecondWeaponRotationOffset(){return weaponRotationOffsetSecondWeapon;}

    [SerializeField] private bool requiresWeapon; 
    public bool DoesRequireWeapon(){return requiresWeapon;}

    
    [Tooltip("1-melee, 2-medium-range, 3-long-range")]
    [SerializeField] protected int attackType;
    public int GetAttackType(){
        return attackType;
    }

    [SerializeField] protected float attackWeight;
    public float GetAttackWeight(){
        return attackWeight;
    }
    public void SetAttackWeight(float newWeight){
        attackWeight = newWeight;
    }
    
    [SerializeField] protected AnimationClip clip;
    public AnimationClip getAnimationClip(){
        return clip;
    }
    
    public abstract void ExecuteAttack(object sender, EnemyAI4.AttackEvent e);
    
    public void SetGameObjectReference(GameObject gameObject){
        _enemyScript = gameObject.GetComponent<EnemyAI4>();
        _enemyGameObject = gameObject;
    }
    
    public void SubscribeAttackToEvent(){
        _enemyScript.AnimationAttackEvent += ExecuteAttack;
    }
}