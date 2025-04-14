using System.Collections.Generic;
using UnityEngine;

public class PlayerState: MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private string lockOnTargetTag;
    public List<Transform> lockOnTargets {get; private set;} = new List<Transform>();
    public bool InAir = false;
    public bool Attacking{get; private set;} = false;
    public bool Rolling = false;
    public AttackSO currentAttack;

    public float TimeSinceLastSpawn;

    private void Start(){
        TimeSinceLastSpawn = 0f;
    }
    private void Update(){
        TimeSinceLastSpawn += Time.deltaTime;
    }

    public void ChangeAttackStatus(bool attacking){
        Attacking = attacking;
    }
    private void OnTriggerEnter(Collider other){
        if(other.CompareTag(lockOnTargetTag)){lockOnTargets.Add(other.transform);}
    }
    
    private void OnTriggerExit(Collider other){
        if(other.CompareTag(lockOnTargetTag)){lockOnTargets.Remove(other.transform);}
    }
}