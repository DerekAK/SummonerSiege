using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class DamageCollider : MonoBehaviour
{
    private CombatManager _combatManager;
    private EntityStats _entityStats;
    private Collider _collider;
    private List<HealthComponent> healthComponentsToDamage = new List<HealthComponent>();


    // settings inherent to the enemy's damage collider
    [Header("General settings inherent to the collider")]
    public BaseAttackSO.eBodyPart BodyPart;

    // settings inherent to the attack itself
    private float attackDamage;

    // settings inherent to the specific hitbox of the attack 
    private BaseAttackSO.eDamageType damageType;
    private float damageTickRate;
    private float damageMultiplier;
    private BaseAttackSO.eElement elementType;
    private BaseStatusEffectSO[] statusEffectSOs;    

    public void Awake()
    {
        _combatManager = transform.root.GetComponent<CombatManager>();
        _entityStats = transform.root.GetComponent<EntityStats>();
        _collider = GetComponent<Collider>();

    }
    public void Start()
    {
        // will need to change this eventually because a weapon's damage collider will not be able to register with a combat manager until equipped
        _combatManager.RegisterDamageCollider(this);
        
        _collider.isTrigger = true;
        _collider.enabled = false;
    }

    public void SetInfoForDamageCollider(BaseAttackSO.Hitbox hitbox, float attackDamage)
    {

        this.attackDamage = attackDamage;
        elementType = hitbox.element;
        damageType = hitbox.damageType;
        damageTickRate = hitbox.damageTickRate;
        damageMultiplier = hitbox.damageMultiplier;
        statusEffectSOs = hitbox.statusEffectSOs;
    }

    private void OnTriggerEnter(Collider other)
    {
        // for now removing this check because enabling hitbox detection on clients for responsiveness and laziness too tbh
        if (!NetworkManager.Singleton.IsServer) return;

        NetworkObject hitNetworkObject = other.GetComponentInParent<NetworkObject>();
        NetworkObject ownNetworkObject = GetComponentInParent<NetworkObject>();
        if (hitNetworkObject == null || hitNetworkObject == ownNetworkObject) return;


        // apply status effects
        if (other.TryGetComponent(out StatusEffectManager statusEffectManager))
        {
            foreach (BaseStatusEffectSO effectSO in statusEffectSOs)
            {
                statusEffectManager.ApplyEffect(transform.root.gameObject, effectSO);
            }
        }

        // apply damage
        if (other.TryGetComponent(out HealthComponent healthComponent))
        {
            Debug.Log("Detected opponent health component!");
            float totalDamage = ComputeDamage();

            if (damageType == BaseAttackSO.eDamageType.Single)
            {
                healthComponent.Damage(totalDamage);
            }
            else
            {
                StartCoroutine(TakeContinuousDamage(totalDamage, healthComponent, other));
            }
        }
    }

    private IEnumerator TakeContinuousDamage(float damage, HealthComponent health, Collider other)
    {
        //if (healthComponentsToDamage.Contains(health)) yield break;
        healthComponentsToDamage.Add(health);
        float timeSinceLastTick = damageTickRate + 1; // First tick instant
        while (healthComponentsToDamage.Contains(health)){
            if (timeSinceLastTick > damageTickRate){
                health.Damage(damage);
                timeSinceLastTick = 0;
            }
            timeSinceLastTick += Time.deltaTime;
            yield return null;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (other.TryGetComponent(out HealthComponent health))
        {
            if (healthComponentsToDamage.Contains(health))
            {
                healthComponentsToDamage.Remove(health);
            }
        }
    }
    
    // this is important to have because if the hitboxes are disabled but the list isn't cleared, they will continue to take damage i think
    public void ManualDisable()
    {
        healthComponentsToDamage.Clear();
    }

    private float ComputeDamage()
    {
        // include element stuff later on
        float finalDamage = attackDamage * damageMultiplier;

        if (_entityStats != null & _entityStats.TryGetStat(StatType.Strength, out NetStat strengthStat))
        {
            finalDamage *= strengthStat.CurrentValue;
        }

        return finalDamage;
    }
    


    // private void ApplyKnockback(Collider other)
    // {
    //     if (other.TryGetComponent(out PlayerMovement movementComponent))
    //     {
    //         Vector3 forceDir = (other.transform.position - transform.position).normalized;
    //         forceDir.y = Random.Range(0.2f, 0.5f);
    //         forceDir *= baseKnockback * knockbackMultiplier;
    //         movementComponent.ApplyForce(forceDir);
    //     }
    // }
}