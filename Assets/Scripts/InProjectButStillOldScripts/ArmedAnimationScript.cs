using UnityEngine;

public class ArmedAnimationScript : MonoBehaviour
{
    [SerializeField] private AnimationClip clip;
    [SerializeField] private Vector3 positionOffset1;
    [SerializeField] private Vector3 rotationOffset1;
    
    //for second weapons or shields
    [SerializeField] private Vector3 positionOffset2;
    [SerializeField] private Vector3 rotationOffset2;
    public AnimationClip GetAnimationClip(){return clip;}
    public Vector3 GetPositionOffset1(){return positionOffset1;}
    public Vector3 GetRotationOffset1(){return rotationOffset1;}
    public Vector3 GetPositionOffset2(){return positionOffset2;}
    public Vector3 GetRotationOffset2(){return rotationOffset2;}
}
