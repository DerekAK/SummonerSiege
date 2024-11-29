using UnityEngine;
public abstract class BaseAttackScript : MonoBehaviour
{
    protected EnemyAI3 _enemyScript;
    
    //attack specific information
    
    [Tooltip("0-unarmed, 1-one-handed, 2-two-handed, 3-double-wielding")]
    [SerializeField] private int weaponType; 
    public int GetWeaponType(){return weaponType;}

    
    [Tooltip("1-melee, 2-medium-range, 3-long-range")]
    [SerializeField] protected int attackType;
    public int GetAttackType(){return attackType;}


    [SerializeField] protected float attackWeight;
    public float GetAttackWeight(){return attackWeight;}

    [SerializeField] protected AnimationClip clip;
    protected void OverrideClip(){clipToOverride = "Attack" +  attackType.ToString() + " Placeholder";}
    protected string clipToOverride;
    public abstract void ExecuteAttack(object sender, EnemyAI3.AttackEvent e);
    protected Animator _anim;
    protected AnimatorOverrideController _animOverrider;
    
    public void HandleAnimation(){
        _anim = GetComponent<Animator>();
        _animOverrider = (AnimatorOverrideController)_anim.runtimeAnimatorController;
        _animOverrider[clipToOverride] = clip;
    }
}
