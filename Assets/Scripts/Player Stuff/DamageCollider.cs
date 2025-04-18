using UnityEngine;
using Unity.Netcode;
using System.Collections;

/*
    Attach this to either a weapon, a projectile, or a gameobject with hitboxtag on it
*/

public class DamageCollider : NetworkBehaviour
{
    [SerializeField] private float baseDamage;
    private BaseAttackSO.Element elementType;
    private BaseAttackSO.Damage damageType;
    private float tickRate;
    private float damageMultiplier;

    public void SetInfo(BaseAttackSO.Hitbox hitbox){
        elementType = hitbox.ElementType;
        damageType = hitbox.DamageType;
        tickRate = hitbox.TickRate;
        damageMultiplier = hitbox.DamageMultiplier;
    }

    private void OnTriggerEnter(Collider other){
        if (!IsServer){return;}
        
        NetworkObject hitNetworkObject = other.GetComponentInParent<NetworkObject>();
        NetworkObject ownNetworkObject = GetComponentInParent<NetworkObject>();
        if (hitNetworkObject == null || hitNetworkObject == ownNetworkObject){return;}

        if (other.TryGetComponent(out HealthComponent healthComponentToDamage)){    
            float totalDamage = ComputeDamage(baseDamage, damageMultiplier, elementType);
            if(damageType == BaseAttackSO.Damage.Single){
                Debug.Log($"Total Damage! {totalDamage}");
                healthComponentToDamage.TakeDamage(totalDamage);
            }
            else{StartCoroutine(TakeContinuousDamage(totalDamage, healthComponentToDamage));}
        }
    }

    private IEnumerator TakeContinuousDamage(float damage, HealthComponent healthComponent){
        float timeSinceLastTick = tickRate + 1; //first damage tick will happen instantly
        while(true){
            if(timeSinceLastTick > tickRate){
                healthComponent.TakeDamage(damage);
                timeSinceLastTick = 0;
            }
            timeSinceLastTick += Time.deltaTime;    
            yield return null;
        }
    }

    private float ComputeDamage(float damage, float damageMultiplier, BaseAttackSO.Element element){
        return damage * damageMultiplier;
    }

}