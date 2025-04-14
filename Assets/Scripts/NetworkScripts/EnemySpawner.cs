using System.Collections;
using Unity.Netcode;
using UnityEngine;
public class EnemySpawner : NetworkBehaviour
{
    public static EnemySpawner Instance;
    [SerializeField] private float spawnInterval = 10f;
    [SerializeField] private float spawnRadius = 20f;
    [SerializeField] private Transform pfEnemy;

    
    public override void OnNetworkSpawn(){
        if(IsServer){
            // Only need to create the EnemySpawner and a reference to the instance on the server, 
            // since server will be the one spawning in enemies
            if(Instance == null){
                Instance = this;
            }
            StartCoroutine(GlobalEnemySpawning());
        }
        else{
            Destroy(gameObject);
        }
    }

    private IEnumerator GlobalEnemySpawning(){
        bool mybool = true;
        while(mybool){
            foreach(NetworkClient client in NetworkManager.Singleton.ConnectedClientsList){
                if(client.PlayerObject.GetComponent<PlayerState>().TimeSinceLastSpawn > spawnInterval){
                    client.PlayerObject.GetComponent<PlayerState>().TimeSinceLastSpawn = 0f;
                    Vector3 offset = new Vector3(Random.Range(-spawnRadius, spawnRadius), 1, Random.Range(-spawnRadius, spawnRadius));
                    Vector3 spawnLocation = client.PlayerObject.transform.position + offset;
                    SpawnNetworkObject(pfEnemy, UtilityFunctions.FindNavMeshPosition(spawnLocation, spawnLocation));
                    mybool = false;
                }
            }
            yield return null;
        }
    }

    private void SpawnNetworkObject(Transform networkObj, Vector3 position){
        Transform networkTransform = Instantiate(networkObj, position, Quaternion.identity);
        networkTransform.GetComponent<NetworkObject>().Spawn(true);
    }
}
