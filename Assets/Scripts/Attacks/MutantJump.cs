using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class MutantJump : BaseAttackScript
{
    [SerializeField] private AnimationClip clip; //3 animations
    private EnemyAI3 parentScript;
    private NavMeshAgent _agentEnemy;
    private Coroutine rotateJumpCoroutine;
    private Rigidbody _rbEnemy;
    private float jumpUpDuration = 1f;
    private float jumpDownDuration = 0.5f;
    private float attackRadius = 20;
    private float forceMultiplier = 1000f;
    private Transform attackCenter;
    
    private void Start(){
        parentScript.Attack2Event += ExecuteAttack;
        _agentEnemy = parentScript.GetComponent<NavMeshAgent>();
        _rbEnemy = parentScript.GetComponent<Rigidbody>();
        attackCenter = parentScript.transform;
    }
    public override void SetAnimationClip(AnimatorOverrideController overrideController){overrideController[ph2] = clip;}
    public override void ProvideInstance(EnemyAI3 script){parentScript = script;}
    public override void ExecuteAttack(object sender, EnemyAI3.AttackEvent e){ //in this case, its the start of the jump
        _rbEnemy.useGravity = false;
        parentScript.Attack2Event -= ExecuteAttack;
        parentScript.Attack2Event += CrashDown;
        rotateJumpCoroutine = StartCoroutine(RotateAndJumpUp(parentScript.transform, e.PlayerTransform));
    }
    private IEnumerator RotateAndJumpUp(Transform enemyTransform, Transform playerTransform) //this should run for the amount of time between the two attackanimations (in jumpduration)
    {
        _agentEnemy.enabled = false;
        Vector3 endDestination = playerTransform.position + Vector3.up * 30f;
        Vector3 origin = enemyTransform.position;
        Vector3 destination = origin + (endDestination - origin)* 0.8f;

        float elapsedTime = 0f;

        while (elapsedTime < jumpUpDuration)
        {
            // Interpolate horizontal position only
            float t = elapsedTime / jumpUpDuration;
            Vector3 newPosition = Vector3.Lerp(origin, destination, t);
            enemyTransform.position = newPosition;

            // Rotate towards player
            enemyTransform.LookAt(new Vector3(playerTransform.position.x, enemyTransform.position.y, playerTransform.position.z));

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private void CrashDown(object sender, EnemyAI3.AttackEvent e){ //in this case, its the end of the jump
        parentScript.Attack2Event -= CrashDown;
        parentScript.Attack2Event += TrackHits;
        StopCoroutine(rotateJumpCoroutine);
        StartCoroutine(JumpDown(parentScript.transform, e.PlayerTransform));
    }

    private IEnumerator JumpDown(Transform enemyTransform, Transform playerTransform){
        float elapsedTime = 0f;
        Vector3 start= enemyTransform.position;
        Vector3 end = playerTransform.position;
        while (elapsedTime < jumpDownDuration)
        {
            float t = elapsedTime / jumpDownDuration;
            Vector3 newPosition = Vector3.Lerp(start, end, t);
            enemyTransform.position = newPosition;
            enemyTransform.LookAt(new Vector3(playerTransform.position.x, enemyTransform.position.y, playerTransform.position.z));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        _rbEnemy.useGravity = true;
        _agentEnemy.enabled = true;
    }

    private void TrackHits(object sender, EnemyAI3.AttackEvent e){ 
        
        parentScript.Attack2Event -= TrackHits;
        parentScript.Attack2Event += ExecuteAttack;
        
        Collider[] hitColliders = Physics.OverlapBox(attackCenter.position, Vector3.one * attackRadius, attackCenter.rotation, e.PlayerL);
        
        foreach (Collider hitCollider in hitColliders)
        {
            Rigidbody rb = hitCollider.gameObject.GetComponent<Rigidbody>();
            Vector3 direction = (e.PlayerTransform.position-attackCenter.position).normalized;
            rb.AddForce(new Vector3(direction.x*forceMultiplier, forceMultiplier/3, direction.z*forceMultiplier), ForceMode.Impulse);
            Debug.DrawRay(transform.position, direction, Color.red, 3f);
        }
    }
    void OnDrawGizmos()
    {
        if (attackCenter != null)
        {
            Gizmos.color = Color.red;
            // Draw a wireframe box to represent the OverlapBox area
            //this sets the origin of the next gizmos command to attackcenter.position, with the rotation of the attack center, and a scale of one (no scaling)
            Gizmos.matrix = Matrix4x4.TRS(attackCenter.position, attackCenter.rotation, Vector3.one); 
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one*attackRadius);  // Use the box size and center position
        }
    }
}
