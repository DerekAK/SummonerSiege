using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(SphereCollider))]
public class DamageCollider : MonoBehaviour
{
    private CombatManager _combatManager;
    private SphereCollider _sphereCollider;

    [Header("General settings inherent to the collider")]
    [SerializeField] private float baseDamage;
    [SerializeField] private float baseKnockback;
    public BaseAttackSO.eBodyPart BodyPart;
    private BaseAttackSO.eElement elementType;
    private float knockbackMultiplier = 1;
    private BaseStatusEffectSO[] statusEffectSOs;

    public void Awake()
    {
        _combatManager = transform.root.GetComponent<CombatManager>();
        _sphereCollider = GetComponent<SphereCollider>();
    }
    public void Start()
    {
        // will need to change this eventually because a weapon's damage collider will not be able to register with a combat manager until equipped
        _combatManager.RegisterDamageCollider(this);
        
        _sphereCollider.isTrigger = true;
        _sphereCollider.enabled = false;
    }

    public void SetInfoForDamageCollider(BaseAttackSO.Hitbox hitbox)
    {
        elementType = hitbox.element;
        knockbackMultiplier = hitbox.knockbackMultiplier;
        statusEffectSOs = hitbox.statusEffectSOs;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        NetworkObject hitNetworkObject = other.GetComponentInParent<NetworkObject>();
        NetworkObject ownNetworkObject = GetComponentInParent<NetworkObject>();
        if (hitNetworkObject == null || hitNetworkObject == ownNetworkObject) return;

        if (other.TryGetComponent(out StatusEffectManager statusEffectManager))
        {
            foreach(BaseStatusEffectSO effectSO in statusEffectSOs)
            {
                statusEffectManager.ApplyEffect(effectSO);
            }
        }
    }

    private void ApplyKnockback(Collider other)
    {
        if (other.TryGetComponent(out PlayerMovement movementComponent))
        {
            Vector3 forceDir = (other.transform.position - transform.position).normalized;
            forceDir.y = Random.Range(0.2f, 0.5f);
            forceDir *= baseKnockback * knockbackMultiplier;
            movementComponent.ApplyForce(forceDir);
        }
    }
}