using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class PushZone : NetworkBehaviour
{
    [SerializeField] private float tickRate;
    private float pushForce = 200;

    private float timeSinceLastTick;
    private List<PlayerMovement> playersInZone = new List<PlayerMovement>();

    private void Awake(){timeSinceLastTick = tickRate;}

    private void Update(){
        if (!IsServer) return;
        timeSinceLastTick += Time.deltaTime;
        if (timeSinceLastTick >= tickRate){
            ApplyPushToAllPlayers();
            timeSinceLastTick = 0f;
        }
    }

    private void OnTriggerEnter(Collider other){
        if (!IsServer) return;
        if (other.gameObject.TryGetComponent(out PlayerMovement movementComponent)){
            playersInZone.Add(movementComponent);
        }
    }

    private void OnTriggerExit(Collider other){
        if (!IsServer) return;
        if (other.gameObject.TryGetComponent(out PlayerMovement movementComponent)){
            playersInZone.Remove(movementComponent);
        }
    }

    private void ApplyPushToAllPlayers(){
        if(!IsServer) return;
        foreach (PlayerMovement movementComponent in playersInZone){
            Vector3 force = (movementComponent.transform.position - transform.position).normalized;
            force.y = 0.5f;
            force *= pushForce;
            movementComponent.ApplyForce(force);
        }
    }
}