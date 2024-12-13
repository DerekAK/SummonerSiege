using System.Collections;
using UnityEngine;
using UnityEngine.AI;
public class MutantJump : BaseAttackScript
{
    private NavMeshAgent _agent;
    private Rigidbody _rb;
    private float jumpUpDuration = 1f;
    private float jumpDownDuration = 0.2f;
    private float attackRadius = 10f;
    private float forceMultiplier = 50f;
    private Transform attackCenter;
    
    private void Awake(){
        _enemyScript = GetComponent<EnemyAI3>();
        _agent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();
        attackCenter = transform;
        OverrideClip();
    }
    public override void ExecuteAttack(object sender, EnemyAI3.AttackEvent e){ //in this case, its the start of the jump
        _enemyScript.AnimationAttackEvent -= ExecuteAttack;
        _enemyScript.AnimationAttackEvent += JumpUp;
    }
    private void JumpUp(object sender, EnemyAI3.AttackEvent e){
        _enemyScript.AnimationAttackEvent -= JumpUp;
        _enemyScript.AnimationAttackEvent += CrashDown;
        _rb.useGravity = false;
        StartCoroutine(JumpUp(e.TargetTransform));
    }
    private IEnumerator JumpUp(Transform playerTransform) //this should run for the amount of time between the two attackanimations (in jumpduration)
    {
        _agent.enabled = false;
        Vector3 endDestination = playerTransform.position + Vector3.up * 30f;
        Vector3 origin = transform.position;
        Vector3 destination = origin + (endDestination - origin) * 0.7f;

        float elapsedTime = 0f;

        while (elapsedTime < jumpUpDuration){
            // Interpolate horizontal position only
            float t = elapsedTime / jumpUpDuration;
            Vector3 newPosition = Vector3.Lerp(origin, destination, t);
            transform.position = newPosition;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        yield break;
    }

    private void CrashDown(object sender, EnemyAI3.AttackEvent e){ //in this case, its the end of the jump
        _enemyScript.AnimationAttackEvent -= CrashDown;
        _enemyScript.AnimationAttackEvent += TrackHits;
        StartCoroutine(JumpDown(transform, e.TargetTransform));
    }

    private IEnumerator JumpDown(Transform enemyTransform, Transform playerTransform){
        float elapsedTime = 0f;
        Vector3 start= enemyTransform.position;
        Vector3 newPos = playerTransform.position;
        Vector3 end = new Vector3();
        RaycastHit hit; //this is to determine the exact y coordinate of the xz coordinate determined by newpos
        if (Physics.Raycast(new Vector3(newPos.x, 100f, newPos.z), Vector3.down, out hit, Mathf.Infinity)){   
            Debug.DrawRay(new Vector3(newPos.x, 100f, newPos.z), Vector3.down * 200f, Color.red, 3f);
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(hit.point, out navHit, 100f, NavMesh.AllAreas)){end = navHit.position;} // Return the valid NavMesh position
        }
        else{
            end = playerTransform.position;
        }
        _rb.useGravity = true;
        while (elapsedTime < jumpDownDuration){
            float t = elapsedTime / jumpDownDuration;
            Vector3 newPosition = Vector3.Lerp(start, end, t);
            enemyTransform.position = newPosition;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        _agent.enabled = true;
    }

    private void TrackHits(object sender, EnemyAI3.AttackEvent e){ 
        _enemyScript.AnimationAttackEvent -= TrackHits;
        Collider[] hitColliders = Physics.OverlapBox(attackCenter.position, Vector3.one * attackRadius, attackCenter.rotation, e.TargetL);
        foreach (Collider hitCollider in hitColliders){
            NavMeshAgent agent;
            if((agent = hitCollider.gameObject.GetComponent<NavMeshAgent>()) != null){
                agent.enabled = false;
            }

            Rigidbody rb = hitCollider.gameObject.GetComponent<Rigidbody>();
            //Vector3 direction = (e.PlayerTransform.position-attackCenter.position).normalized;
            Vector3 direction = Vector3.up;
            //rb.AddForce(new Vector3(direction.x*forceMultiplier, forceMultiplier/3, direction.z*forceMultiplier), ForceMode.Impulse);
            rb.AddForce(direction*forceMultiplier, ForceMode.Impulse);
            Debug.DrawRay(transform.position, direction, Color.red, 3f);

            StartCoroutine(EnableAgent(hitCollider.gameObject));
        }
    }
    private IEnumerator EnableAgent(GameObject gameObject){
        yield return new WaitForSeconds(0.2f);
        NavMeshAgent agent;
        if((agent = gameObject.GetComponent<NavMeshAgent>()) != null){
            agent.enabled = true;
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
