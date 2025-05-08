using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class DamageCollider : NetworkBehaviour
{
    [Header("General settings inherent to the collider")]
    [SerializeField] private float baseDamage;
    private BaseAttackSO.Element elementType;
    private BaseAttackSO.Damage damageType;
    private float tickRate;
    private float damageMultiplier;
    private float knockbackMultiplier;
    //private StatusEffect[] statusEffects;
    private Coroutine continuousDamageCoroutine;

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

        if (other.TryGetComponent(out HealthComponent healthComponentToDamage)){
            float totalDamage = ComputeDamage(baseDamage, damageMultiplier, elementType);
            Debug.Log($"Total damage: {totalDamage}");
            if (damageType == BaseAttackSO.Damage.Single){
                healthComponentToDamage.TakeDamage(totalDamage);
                ApplyKnockback(other);
            }
            else{
                continuousDamageCoroutine = StartCoroutine(TakeContinuousDamage(totalDamage, healthComponentToDamage, other));
            }
        }
    }

    private void OnTriggerExit(Collider other){
        if (continuousDamageCoroutine != null){
            StopCoroutine(continuousDamageCoroutine);
        }
    }

    private IEnumerator TakeContinuousDamage(float damage, HealthComponent healthComponent, Collider other){
        float timeSinceLastTick = tickRate + 1; // First tick instant
        while (true){
            if (timeSinceLastTick > tickRate){
                healthComponent.TakeDamage(damage);
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
            forceDir *= knockbackMultiplier;
            movementComponent.ApplyForce(forceDir);
        }
    }
}