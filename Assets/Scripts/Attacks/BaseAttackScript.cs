using UnityEngine;
public abstract class BaseAttackScript : MonoBehaviour
{
    protected EnemyAI3 _enemyScript;
    
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
    public int GetAttackType(){return attackType;}

    [SerializeField] protected float attackWeight;
    public float GetAttackWeight(){return attackWeight;}
    public void SetAttackWeight(float newWeight){attackWeight = newWeight;}

    [SerializeField] protected AnimationClip clip;
    public AnimationClip getAnimationClip(){return clip;}
    protected void OverrideClip(){clipToOverride = "Attack" +  attackType.ToString() + " Placeholder";}
    protected string clipToOverride;
    public abstract void ExecuteAttack(object sender, EnemyAI3.AttackEvent e);
    protected Animator _anim;
    protected AnimatorOverrideController _animOverrider;
    
    public void HandleAnimation(){
        Debug.Log(transform.root.gameObject.name);
        _anim = transform.root.gameObject.GetComponent<Animator>();
        Debug.Log("ANIMATOR:", _anim);
        _animOverrider = (AnimatorOverrideController)_anim.runtimeAnimatorController;
        _animOverrider[clipToOverride] = clip;
        _enemyScript.AnimationAttackEvent += ExecuteAttack;
    }    
}
