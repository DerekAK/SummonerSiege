using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class HealthComponent: NetworkBehaviour
{   
    private NetworkStat healthNetworkStat;
    [SerializeField] private RectTransform localHealthBar;
    [SerializeField] private RectTransform worldSpaceHealthBar;
    private NetworkStats _stats;
    
    private void Awake(){
        _stats = GetComponent<NetworkStats>();
    }

    private void Start()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            Initialize();
        }
    }

    public override void OnNetworkSpawn(){
        Initialize();
    }
    
    private void Initialize()
    {
        healthNetworkStat = _stats.HealthStat;
        if (healthNetworkStat != null)
        {
            healthNetworkStat.Stat.OnValueChanged += OnNetworkHealthChanged;
            healthNetworkStat.OnEqualsZero += Die;
            StartCoroutine(WaitForStatsLoaded());
        }
    }

    private void Shutdown()
    {
        if (healthNetworkStat != null)
        {
            healthNetworkStat.Stat.OnValueChanged -= OnNetworkHealthChanged;
            healthNetworkStat.OnEqualsZero -= Die;
        }
    }
    
    public override void OnNetworkDespawn(){
        Shutdown();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (NetworkManager.Singleton == null || (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient))
        {
            Shutdown();
        }
    }
    
    private IEnumerator WaitForStatsLoaded(){
        yield return new WaitForSeconds(0.5f); //wait one second, enough time for stats to be loaded joining clients
        OnNetworkHealthChanged(healthNetworkStat.Stat.Value, healthNetworkStat.Stat.Value);
    }
    
    private void OnNetworkHealthChanged(float previousValue, float newValue)
    {
        float fillAmount = 0f; // Default the fill amount to 0.

        // FIX: Check if max health is not zero to prevent a division error.
        if (healthNetworkStat.MaxStat.Value > 0)
        {
            fillAmount = newValue / healthNetworkStat.MaxStat.Value;
        }
        
        bool isOwnerOrSinglePlayer = IsOwner || (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient);

        if (isOwnerOrSinglePlayer)
        {
            if (localHealthBar != null)
            {
                Vector3 currHealthBarVector3 = localHealthBar.localScale;
                currHealthBarVector3.x = fillAmount;
                localHealthBar.localScale = currHealthBarVector3;
            }
        }
        else
        {
            if (worldSpaceHealthBar != null)
            {
                Vector3 currHealthBarVector3 = worldSpaceHealthBar.localScale;
                currHealthBarVector3.x = fillAmount;
                worldSpaceHealthBar.localScale = currHealthBarVector3;
            }
        }
    }

    public void TakeDamage(float damageAmount){
        healthNetworkStat.Decrease(damageAmount);
    }

    public void Heal(float healAmount){
        Debug.Log("HEALING!");
        healthNetworkStat.Increase(healAmount);
    }

    private void Die(object sender, EventArgs e){
        Debug.Log("Player " + OwnerClientId + " Died!");
    }   
}