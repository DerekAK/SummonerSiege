using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class DamageCollider : NetworkBehaviour
{
    [Header("General settings inherent to the collider")]
    [SerializeField] private float baseDamage;
    [SerializeField] private float baseKnockback;
    private BaseAttackSO.Element elementType;
    private BaseAttackSO.eDamageType damageType;
    private float tickRate;
    private float damageMultiplier = 1;
    private float knockbackMultiplier = 1;
    //private StatusEffect[] statusEffects;
    private List<HealthComponent> healthComponentsToDamage = new List<HealthComponent>();

    public void SetInfo(BaseAttackSO.Hitbox hitbox){
        elementType = hitbox.ElementType;
        damageType = hitbox.DamageType;
        tickRate = hitbox.TickRate;
        damageMultiplier = hitbox.DamageMultiplier;
        knockbackMultiplier = hitbox.KnockbackMultiplier;
        //statusEffects = hitbox.StatusEffects;
    }

    public override void OnNetworkSpawn(){
       GetComponent<Collider>().enabled = false;
    }

    private void OnTriggerEnter(Collider other){
        if (!IsServer) return;

        NetworkObject hitNetworkObject = other.GetComponentInParent<NetworkObject>();
        NetworkObject ownNetworkObject = GetComponentInParent<NetworkObject>();
        if (hitNetworkObject == null || hitNetworkObject == ownNetworkObject) return;

        if (other.TryGetComponent(out HealthComponent healthComponent)){
            float totalDamage = ComputeDamage(baseDamage, damageMultiplier, elementType);
            Debug.Log($"Total damage: {totalDamage}");
            if (damageType == BaseAttackSO.eDamageType.Single){
                healthComponent.TakeDamage(totalDamage);
                ApplyKnockback(other);
            }
            else{
                StartCoroutine(TakeContinuousDamage(totalDamage, healthComponent, other));
            }
        }
    }

    private void OnTriggerExit(Collider other){
        if(other.TryGetComponent(out HealthComponent health)){
            if(healthComponentsToDamage.Contains(health)){
                healthComponentsToDamage.Remove(health);
            }
        }
    }

    public void DisableManually(){healthComponentsToDamage.Clear();}

    private IEnumerator TakeContinuousDamage(float damage, HealthComponent health, Collider other){
        
        float timeSinceLastTick = tickRate + 1; // First tick instant
        healthComponentsToDamage.Add(health);
        while (healthComponentsToDamage.Contains(health)){
            if (timeSinceLastTick > tickRate){
                health.TakeDamage(damage);
                ApplyKnockback(other);
                timeSinceLastTick = 0;
            }
            timeSinceLastTick += Time.deltaTime;
            yield return null;
        }
    }

    private float ComputeDamage(float damage, float damageMultiplier, BaseAttackSO.Element element){
        return damage * damageMultiplier;
    }

    private void ApplyKnockback(Collider other){
        if (other.TryGetComponent(out PlayerMovement movementComponent)){
            Vector3 forceDir = (other.transform.position - transform.position).normalized;
            forceDir.y = Random.Range(0.2f, 0.5f);
            forceDir *= baseKnockback * knockbackMultiplier;
            movementComponent.ApplyForce(forceDir);
        }
    }
}