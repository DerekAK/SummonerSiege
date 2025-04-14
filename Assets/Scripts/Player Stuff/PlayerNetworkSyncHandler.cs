using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkSyncHandler : NetworkBehaviour
{
    
    public event EventHandler NetworkSyncEvent;
    private float syncInterval = 10f;
    private float timeSinceLastSync;

    public override void OnNetworkSpawn(){
        if(IsLocalPlayer){
            StartCoroutine(SyncingCoroutine());
            timeSinceLastSync = syncInterval;
        }
    }

    private IEnumerator SyncingCoroutine(){
        if(!IsLocalPlayer){yield break;}
        while(true){
            if(timeSinceLastSync > syncInterval){
                timeSinceLastSync = 0;
                NetworkSyncEvent?.Invoke(this, EventArgs.Empty);
            }
            timeSinceLastSync += Time.deltaTime;
            yield return null;
        }
    }

}
