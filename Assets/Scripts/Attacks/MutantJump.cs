using System.Collections;
using UnityEngine;
using UnityEngine.AI;
public class MutantJump : BaseAttackScript{

    //enemy ai script will call the animation to play
    // the animation requires attributes from the enemy ai script including the transform of the player 
    // question: The animation events will call functions that are on the specific attack script, this makes sense. However, how 
    // will those functions access the attributes from the enemy ai script? I don't think that the attack script can access the enemy ai script.
    // this is because the attack script is located on a prefab and that prefab is a serializefield reference in 
    private NavMeshAgent _agent;
    private Rigidbody _rb;
    private float jumpUpDuration = 0.4f;
    private float jumpDownDuration = 0.3f;
    private float attackRadius = 10f;
    private float forceMultiplier = 50f;
    private Transform attackCenter;
    
    public override void ExecuteAttack(object sender, EnemyAI4.AttackEvent e){ //in this case, its the start of the jump
        _enemyScript.AnimationAttackEvent -= ExecuteAttack;
        _enemyScript.AnimationAttackEvent += JumpUp;
        _agent = _enemyGameObject.GetComponent<NavMeshAgent>();
        _rb = _enemyGameObject.GetComponent<Rigidbody>();
        attackCenter = transform;
    }
    private void JumpUp(object sender, EnemyAI4.AttackEvent e){
        _enemyScript.AnimationAttackEvent -= JumpUp;
        _enemyScript.AnimationAttackEvent += CrashDown;
        StartCoroutine(JumpUp(_enemyScript.GetCurrentTarget()));
    }
    private IEnumerator JumpUp(Transform playerTransform) //this should run for the amount of time between the two attackanimations (in jumpduration)
    {
        _agent.enabled = false;
        _rb.useGravity = false;
        Vector3 endDestination = playerTransform.position + Vector3.up * 30f;
        Vector3 origin = _enemyGameObject.transform.position;
        Vector3 destination = origin + (endDestination - origin) * 0.7f;
        float elapsedTime = 0f;
        while (elapsedTime < jumpUpDuration){
            // Interpolate horizontal position only
            _enemyGameObject.transform.LookAt(new Vector3(playerTransform.position.x, _enemyGameObject.transform.position.y, playerTransform.position.z));
            float t = elapsedTime / jumpUpDuration;
            Vector3 newPosition = Vector3.Lerp(origin, destination, t);
            _enemyGameObject.transform.position = newPosition;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        yield break;
    }

    private void CrashDown(object sender, EnemyAI4.AttackEvent e){ //in this case, its the end of the jump
        _enemyScript.AnimationAttackEvent -= CrashDown;
        _enemyScript.AnimationAttackEvent += TrackHits;
        StartCoroutine(JumpDown(_enemyGameObject.transform, _enemyScript.GetCurrentTarget()));
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
            _enemyGameObject.transform.LookAt(new Vector3(playerTransform.position.x, _enemyGameObject.transform.position.y, playerTransform.position.z));
            yield return null;
        }
        _agent.enabled = true;
    }

    private void TrackHits(object sender, EnemyAI4.AttackEvent e){ 
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
