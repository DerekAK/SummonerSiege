using System.Collections;
using Unity.Netcode;
using UnityEngine;

class PlayerUI : NetworkBehaviour
{
    [SerializeField] private GameObject screenUI;
    [SerializeField] private GameObject worldUI;
    public override void OnNetworkSpawn(){
        if(IsLocalPlayer){
            Destroy(worldUI);
        }
        else{
            Destroy(screenUI);
        }
    }
}