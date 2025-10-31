using System.Collections;
using Unity.Netcode;
using UnityEngine;
public class EnemySpawner : NetworkBehaviour
{
    [SerializeField] private float spawnInterval = 10f;
    [SerializeField] private float spawnRadius = 20f;
    [SerializeField] private GameObject pfEnemy;


    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        StartCoroutine(GlobalEnemySpawning());

    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T) && IsServer)
        {
            Vector3 serverPlayerObjecPos = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(0).transform.position;
            SpawnNetworkObject(pfEnemy, serverPlayerObjecPos);
        }
    }

    private IEnumerator GlobalEnemySpawning()
    {
        // Run this loop forever
        while (true)
        {
            // Check all connected clients
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                // --- THIS IS THE FIX ---
                // Instead of client.PlayerObject, get the object from the SpawnManager
                NetworkObject playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(client.ClientId);

                // If the SpawnManager can't find a player for this client, skip them
                if (playerObject == null)
                {
                    Debug.LogWarning($"SpawnManager has no player object for Client {client.ClientId}. Skipping.");
                    continue;
                }

                // Now it's safe to get the component
                PlayerState playerState = playerObject.GetComponent<PlayerState>();
                if (playerState == null)
                {
                    Debug.LogError($"Client {client.ClientId}'s player object is missing a PlayerState component!");
                    continue;
                }

                if (playerState.TimeSinceLastEnemySpawn > spawnInterval)
                {
                    Debug.Log($"Spawning enemy for Client {client.ClientId}");
                    playerState.TimeSinceLastEnemySpawn = 0f;
                    Vector3 offset = new Vector3(Random.Range(-spawnRadius, spawnRadius), 1, Random.Range(-spawnRadius, spawnRadius));
                    Vector3 spawnLocation = playerObject.transform.position + offset;
                    SpawnNetworkObject(pfEnemy, UtilityFunctions.FindNavMeshPosition(spawnLocation, spawnLocation));
                }
            }

            // Wait for 1 second before checking all clients again.
            yield return new WaitForSeconds(1.0f);
        }
    }

    private void SpawnNetworkObject(GameObject pfEnemy, Vector3 position)
    {

        Debug.Log($"<color=orange>[EnemySpawner] Attempting to Instantiate/Spawn '{pfEnemy.name}'...</color>");

        NetworkObject enemy = NetworkObjectPool.Singleton.GetObject(
            pfEnemy.GetComponent<NetworkObject>(),
            position,
            Quaternion.identity,
            destroyWithScene: false
        );

        StartCoroutine(ReturnToObjectPoolAfterTime(10, enemy));

    }

    
    
    private IEnumerator ReturnToObjectPoolAfterTime(float time, NetworkObject enemyInstance)
    {
        yield return new WaitForSeconds(time);
        Debug.Log("Returning Enemy to network pool!");

        NetworkObjectPool.Singleton.ReturnObject(enemyInstance, pfEnemy.GetComponent<NetworkObject>());
    }
}
