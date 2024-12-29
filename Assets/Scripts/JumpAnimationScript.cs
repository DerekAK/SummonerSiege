using UnityEngine;
using UnityEngine.AI;

public class JumpAnimationScript : ArmedAnimationScript
{
    private Rigidbody _rb;
    private NavMeshAgent _agent;
    private void TestFunction(){
        _rb = GetComponent<Rigidbody>();
        _agent = GetComponent<NavMeshAgent>();
        Debug.Log(_rb);
        Debug.Log(_agent);
    }
}
