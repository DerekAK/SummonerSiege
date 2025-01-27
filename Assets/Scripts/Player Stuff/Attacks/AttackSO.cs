using UnityEngine;

[CreateAssetMenu(fileName = "AttackData", menuName = "Scriptable Objects/AttackData")]
public class AttackSO : ScriptableObject
{
    public enum Element{
        None,
        Fire,
        Earth,
        Ice,
        Lightning
    }
    public Element ElementType;
    public AnimationClip attackClip;
    public bool airAttack;
    public float movementSpeedFactor;

}
