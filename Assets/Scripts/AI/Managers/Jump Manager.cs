using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class JumpManager: MonoBehaviour
{
    private NavMeshAgent _agent;
    private Animator _anim;
    [SerializeField] private AnimationCurve jumpPositionCurve;
    [SerializeField] private AnimationCurve jumpSpeedCurve;
    private bool inJump = false;
    public bool InJump => inJump;
    private Coroutine jumpCoroutine;
    private Vector3 startPos;
    private Vector3 endPos;
    private int AnimJump = Animator.StringToHash("Jump");
    private int AnimLand = Animator.StringToHash("Land");

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<Animator>();
    }

    public void InitiateJump(Vector3 startPos, Vector3 endPos)
    {
        if (inJump) return;
        inJump = true;

        _agent.enabled = false;

        this.startPos = startPos;
        this.endPos = endPos;
        _anim.SetTrigger(AnimJump);
    }

    private IEnumerator JumpCoroutine(Vector3 startPos, Vector3 endPos)
    {
        // aiming for around 1 - 2 seconds, will want to make this a function of the character's jump height and distance probably
        float duration = 0.8f;
        float elapsedTime = 0;
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;

            float speedEvaluatedT = jumpSpeedCurve.Evaluate(t);

            Vector3 newPos = Vector3.Lerp(startPos, endPos, speedEvaluatedT);

            newPos.y += newPos.y * jumpPositionCurve.Evaluate(speedEvaluatedT);

            transform.position = newPos;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        _anim.SetTrigger(AnimLand);

    }

    // Animation event call these
    private void JumpLaunch()
    {
        if (jumpCoroutine != null)
        {
            StopCoroutine(jumpCoroutine);
            jumpCoroutine = null;
        }
        jumpCoroutine = StartCoroutine(JumpCoroutine(startPos, endPos));
    }

    private void FinishLand()
    {
        if (jumpCoroutine != null)
        {
            StopCoroutine(jumpCoroutine);
            jumpCoroutine = null;
        }
        _agent.enabled = true;
        _agent.Warp(transform.position);

        inJump = false;
        _agent.CompleteOffMeshLink();
    }
}