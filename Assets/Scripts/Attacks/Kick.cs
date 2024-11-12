using UnityEngine;

public class Kick : BaseAttackScript
{
    [SerializeField] private AnimationClip clip;
    private EnemyAI3 parentScript;
    private float attackRadius = 10f;
    private float forceMultiplier = 500f;
    private Transform _attackCenter;

    private void Start(){
        parentScript.Attack1Event += ExecuteAttack;
    }
    public override void SetAnimationClip(AnimatorOverrideController overrideController){overrideController[ph1] = clip;}
    public override void ProvideInstance(EnemyAI3 script){parentScript = script;}
    public override void ExecuteAttack(object sender, EnemyAI3.AttackEvent e){ 
        _attackCenter = e.AttackCenterForward; //for gizmos purpose
        Vector3 attackCenter = e.AttackCenterForward.position;
        
        Collider[] hitColliders = Physics.OverlapBox(attackCenter, Vector3.one * attackRadius, parentScript.transform.rotation, e.PlayerL);
        
        foreach (Collider hitCollider in hitColliders)
        {
            Rigidbody rb = hitCollider.gameObject.GetComponent<Rigidbody>();
            Vector3 direction = (attackCenter-parentScript.transform.position).normalized;
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
