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

    public override void OnNetworkSpawn(){
        base.OnNetworkSpawn();
        healthNetworkStat = _stats.HealthStat;
        healthNetworkStat.Stat.OnValueChanged += OnNetworkHealthChanged;
        healthNetworkStat.OnEqualsZero += Die;

        StartCoroutine(WaitForStatsLoaded());
    }

    private IEnumerator WaitForStatsLoaded(){
        yield return new WaitForSeconds(0.5f); //wait one second, enough time for stats to be loaded joining clients
        OnNetworkHealthChanged(healthNetworkStat.Stat.Value, healthNetworkStat.Stat.Value);
    }

    public override void OnNetworkDespawn(){
        base.OnNetworkDespawn();
        healthNetworkStat.Stat.OnValueChanged -= OnNetworkHealthChanged;
    }

    private void OnNetworkHealthChanged(float previousValue, float newValue){
        float fillAmount = newValue/healthNetworkStat.MaxStat.Value;
        //Debug.Log($"Client {OwnerClientId} fill amount of {fillAmount}, previousVal is {previousValue}, newValue is {newValue}");
        if(IsOwner){
            Vector3 currHealthBarVector3 = localHealthBar.localScale;
            currHealthBarVector3.x = fillAmount;
            localHealthBar.localScale = currHealthBarVector3;
        }
        else{
            // intuition is that when one player changes health, every other client calls this but is referencing that player's worldspacehealthbar
            Vector3 currHealthBarVector3 = worldSpaceHealthBar.localScale;
            currHealthBarVector3.x = fillAmount;
            worldSpaceHealthBar.localScale = currHealthBarVector3;
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
