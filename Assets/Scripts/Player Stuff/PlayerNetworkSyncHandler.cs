using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkSyncHandler : NetworkBehaviour
{
    
    public event EventHandler NetworkSyncEvent;
    private float syncInterval = 3f;
    private float timeSinceLastSync = 0;

    public override void OnNetworkSpawn(){
        if(IsLocalPlayer){
            StartCoroutine(SyncingCoroutine());
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
