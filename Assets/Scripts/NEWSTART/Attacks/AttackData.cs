using UnityEditor.Rendering;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackData", menuName = "Scriptable Objects/AttackData")]
public class AttackData : ScriptableObject
{
    public enum Element{
        fire,
        earth,
        ice,
        lightning
    }

    public Element ElementType {get; private set;}

    public AnimationClip unequipClip{get; private set;}
}
