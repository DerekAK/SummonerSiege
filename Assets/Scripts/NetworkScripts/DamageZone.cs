using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class DamageZone : NetworkBehaviour
{
    [SerializeField] private float tickRate = 1f;
    [SerializeField] private float damageAmount = 10f;
    private float timeSinceLastTick;
    private List<HealthComponent> playersInZone = new List<HealthComponent>();

    private void Awake(){timeSinceLastTick = tickRate;}

    private void Update(){
        if(!IsServer){return;}
        timeSinceLastTick += Time.deltaTime;
        if (timeSinceLastTick >= tickRate){
            ApplyDamageToAllPlayers();
            timeSinceLastTick = 0f;
        }
    }

    private void OnTriggerEnter(Collider other){
        if (!IsServer) return;
        if (other.gameObject.TryGetComponent(out HealthComponent health)){
            if (!playersInZone.Contains(health))
                playersInZone.Add(health);
        }
    }

    private void OnTriggerExit(Collider other){
        if (!IsServer){return;}
        if (other.gameObject.TryGetComponent(out HealthComponent health))
            playersInZone.Remove(health);
    }

    private void ApplyDamageToAllPlayers(){
        foreach (HealthComponent health in playersInZone){health.TakeDamage(damageAmount);}
    }
}
