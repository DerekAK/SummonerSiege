using Unity.Netcode;
using UnityEngine;

public class TakeDamageOnCollide : MonoBehaviour
{

    private float damageTickRate = 1f;
    private float damageAmount = 10f;
    private float timeSinceLastDamage;

    private void Awake(){
        timeSinceLastDamage = damageTickRate;
    }
    private void OnTriggerStay(Collider other){
        if(other.gameObject.TryGetComponent(out HealthComponent health)){
            if(timeSinceLastDamage >= damageTickRate){
                health.TakeDamage(damageAmount);
                timeSinceLastDamage = 0f;
            }
            else{
                timeSinceLastDamage += Time.deltaTime;
            }
        }
    }
}
