using UnityEngine;
using System.Collections;

public class FollowTarget: MonoBehaviour
{

    [SerializeField] private Vector3 positionOffset;
    [SerializeField] private Vector3 rotationOffset;

    public IEnumerator FollowTargetCoroutine(Transform target){
        while(true){
            transform.position = target.position + positionOffset;
            transform.eulerAngles = target.eulerAngles + rotationOffset;
            yield return null;
        }
    }
}