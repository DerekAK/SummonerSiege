using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(EntityStats))]
public class HealthComponent : NetworkBehaviour
{
    private EntityStats _entityStats;
    void Awake()
    {
        _entityStats = GetComponent<EntityStats>();
    }

    public override void OnNetworkSpawn()
    {

        _entityStats.OnStatValueChanged += HandleChangeInHealth;
    }

    private void HandleChangeInHealth(StatType type, float newValue)
    {
        if (type != StatType.Health) return;
        if (newValue == 0) Die();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Damage(10);
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Heal(10);
        }
    }    
    
    public void Damage(float amount)
    {
        if (!IsServer) return;

        float damageAmount = -amount;
        _entityStats.ModifyStatServerRpc(StatType.Health, damageAmount);
    }

    
    public void Heal(float healAmount)
    {
        if (!IsServer) return;

        _entityStats.ModifyStatServerRpc(StatType.Health, healAmount);
    }

    private void Die()
    {
        Debug.Log("Object " + gameObject.name + " Died!");
        // Add death logic here: spawn loot, play animation, destroy object etc.
        // Example: NetworkObject.Despawn();
    }
    
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (_entityStats != null)
        {
            _entityStats.OnStatValueChanged -= HandleChangeInHealth;
        }
    }
}