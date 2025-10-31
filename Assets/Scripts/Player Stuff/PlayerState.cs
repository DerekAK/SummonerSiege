using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerState: NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private string lockOnTargetTag;
    [Tooltip("Mark as false if just want debugging for single person world")]
    public List<Transform> lockOnTargets { get; private set; } = new List<Transform>();
    public bool InAir = false;
    public bool Attacking{get; private set;} = false;
    public bool Rolling = false;
    public BaseAttackSO currentAttack;

    private float timeSinceLastEnemySpawn = 0f;
    public float TimeSinceLastEnemySpawn
    {
        get { return timeSinceLastEnemySpawn; }
        set { timeSinceLastEnemySpawn = value; }
    }

    public override void OnNetworkSpawn()
    {
        EndlessTerrain endlessTerrain = FindFirstObjectByType<EndlessTerrain>();
        endlessTerrain.SetViewerTransform(transform);
    }

    private void Start()
    {
        timeSinceLastEnemySpawn = 0f;
    }
    private void Update(){
        timeSinceLastEnemySpawn += Time.deltaTime;
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