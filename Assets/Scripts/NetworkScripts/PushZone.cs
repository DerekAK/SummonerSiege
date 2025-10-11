using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class PushZone : NetworkBehaviour
{
    [SerializeField] private float tickRate = 0.2f;
    private float pushForce = 200;

    private float timeSinceLastTick;
    private List<GameObject> objectsInZone = new();

    private void Awake() { timeSinceLastTick = tickRate; }

    private void Update(){
        if (!IsServer) return;
        timeSinceLastTick += Time.deltaTime;
        if (timeSinceLastTick >= tickRate){
            ApplyPushToAll();
            timeSinceLastTick = 0f;
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (!IsServer) return;
        if (other.gameObject.TryGetComponent(out PlayerMovement _))
        {
            objectsInZone.Add(other.gameObject);
        }

        else if (other.gameObject.TryGetComponent(out Rigidbody _)){
            objectsInZone.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other) {
        if (!IsServer) return;
        if (other.gameObject.TryGetComponent(out PlayerMovement _))
        {
            objectsInZone.Remove(other.gameObject);
        }

        else if (other.gameObject.TryGetComponent(out Rigidbody _))
        {
            objectsInZone.Remove(other.gameObject);
        }

    }

    private void ApplyPushToAll()
    {
        if (!IsServer) return;
        foreach (GameObject obj in objectsInZone)
        {
            Vector3 force = (obj.transform.position - transform.position).normalized;
            force.y = 0.5f;
            force *= pushForce;

            if (obj.TryGetComponent(out PlayerMovement movementComponent))
            {
                movementComponent.ApplyForce(force);
            }
            else if (obj.TryGetComponent(out Rigidbody rb))
            {
                Debug.Log($"Applying force to {rb.gameObject.name}");
                rb.AddForce(force);
            }
        }

        
    }
}