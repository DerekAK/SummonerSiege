using Unity.Netcode;
using UnityEngine;

public class HealOnCollision : MonoBehaviour
{

    private float healTickRate = 1f;
    private float healAmount = 10f;
    private float timeSinceLastHeal;

    private void Awake(){
        timeSinceLastHeal = healTickRate;
    }
    private void OnTriggerStay(Collider other){
        if(other.gameObject.TryGetComponent(out HealthComponent health)){
            if(timeSinceLastHeal >= healTickRate){
                health.Heal(healAmount);
                timeSinceLastHeal = 0f;
            }
            else{
                timeSinceLastHeal += Time.deltaTime;
            }
        }
    }
}
