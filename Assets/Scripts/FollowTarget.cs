using UnityEngine;

public class FollowTarget: MonoBehaviour
{

    public Transform FollowTargetTransform;
    private void LateUpdate(){
        if(FollowTargetTransform is null){
            Debug.LogError($"No target to follow for {name}!");
            return;
        }

        transform.position = FollowTargetTransform.position;
        transform.rotation = FollowTargetTransform.rotation;
    }
}