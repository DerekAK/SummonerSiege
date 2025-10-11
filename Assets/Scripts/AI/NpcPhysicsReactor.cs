using UnityEngine;
using UnityEngine.AI;
using System.Collections; // Required for Coroutines

public class NpcPhysicsReactor : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Rigidbody _rb;
    private bool _isAgentControlActive = true; // Our new state flag

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only trigger if agent control is active and we find a Rigidbody
        if (_isAgentControlActive && other.TryGetComponent(out Rigidbody incoming_rb))
        {
            // Optional but recommended: check if the incoming Rigidbody is dynamic
            if (!incoming_rb.isKinematic)
            {
                Debug.Log("Dynamic Rigidbody entering my area!");
                SwitchToPhysicsMode();
            }
        }
    }

    private void SwitchToPhysicsMode()
    {
        _isAgentControlActive = false; // Immediately block further triggers
        _agent.enabled = false;
        _rb.isKinematic = false;

        StartCoroutine(MonitorPhysicsAndRevert());
    }

    private IEnumerator MonitorPhysicsAndRevert()
    {
        // Wait a brief moment to ensure the collision has time to impart force
        yield return new WaitForFixedUpdate();

        // Now, wait until the Rigidbody has almost stopped moving
        yield return new WaitUntil(() => _rb.linearVelocity.magnitude < 0.5f);

        // The physics simulation has settled. Revert control to the agent.
        //_rb.isKinematic = true;
        _agent.enabled = true;
        _agent.Warp(transform.position); // Sync agent to the new position
        
        _isAgentControlActive = true; // Re-enable triggers now that we're done
    }
}