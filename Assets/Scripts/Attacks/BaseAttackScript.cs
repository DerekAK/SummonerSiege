using UnityEngine;


//to add a new attack in the game, must set clipToOveride to ph1, ph2, or ph3 in awake()
public abstract class BaseAttackScript : MonoBehaviour
{

    //attack specific information
    [SerializeField] protected int attackType;
    public int GetAttackType(){
        return attackType;
    }
    [SerializeField] protected float attackWeight;
    public float GetAttackWeight(){
        return attackWeight;
    }
    [SerializeField] protected AnimationClip clip;
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
