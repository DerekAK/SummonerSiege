using UnityEngine;

public class Kick : BaseAttackScript
{
    [SerializeField] private AnimationClip clip;
    private EnemyAI3 _enemyScript;
    private Animator _anim;
    private AnimatorOverrideController _overrider;
    private float attackRadius = 10f;
    private float forceMultiplier = 50f;
    private Transform _attackCenter;

    private void Awake(){
        _enemyScript = GetComponent<EnemyAI3>();
        _anim = GetComponent<Animator>();
        _overrider = (AnimatorOverrideController)_anim.runtimeAnimatorController;
    }
    private void Start(){
        _enemyScript.Attack1Event += ExecuteAttack;
        _overrider[ph1] = clip;
    }
    public override void ExecuteAttack(object sender, EnemyAI3.AttackEvent e){ 
        _attackCenter = e.AttackCenterForward; //for gizmos purpose
        Vector3 attackCenter = e.AttackCenterForward.position;
        
        Collider[] hitColliders = Physics.OverlapBox(attackCenter, Vector3.one * attackRadius, transform.rotation, e.PlayerL);
        
        foreach (Collider hitCollider in hitColliders)
        {
            Rigidbody rb = hitCollider.gameObject.GetComponent<Rigidbody>();
            Vector3 direction = (attackCenter-transform.position).normalized;
            rb.AddForce(new Vector3(direction.x*forceMultiplier, forceMultiplier/3, direction.z*forceMultiplier), ForceMode.Impulse);
            Debug.DrawRay(transform.position, direction, Color.red, 3f);
        }
    }
    void OnDrawGizmos()
    {
        if (_attackCenter != null)
        {
            Gizmos.color = Color.red;
            // Draw a wireframe box to represent the OverlapBox area
            //this sets the origin of the next gizmos command to attackcenter.position, with the rotation of the attack center, and a scale of one (no scaling)
            Gizmos.matrix = Matrix4x4.TRS(_attackCenter.position, _attackCenter.rotation, Vector3.one); 
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one*attackRadius);  // Use the box size and center position
        }
    }
}
