using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Start is called before the first frame update
    public static GameManager Instance {get; private set;}
    [SerializeField] private Transform playerPrefab; // Reference to the player's Transform
    private List<Transform> players = new List<Transform>();
    [SerializeField] private int numPlayers;

    private Vector3 spawnCenter;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        spawnCenter = transform.position;
        for (int i = 0; i < numPlayers; i++){
            Vector3 spawnPosition = new Vector3(spawnCenter.x + i*5, spawnCenter.y, spawnCenter.z + i*5);
            SpawnPlayer(playerPrefab, spawnPosition);
        }
    }
    public List<Transform> getPlayerTransforms(){
        return players;
    }
    public void RegisterPlayer(Transform playerTransform)
    {
        if (!players.Contains(playerTransform))
        {
            Debug.Log("added a player to players");
            players.Add(playerTransform);
        }
    }
    public void UnregisterPlayer(Transform playerTransform)
    {
        if (players.Contains(playerTransform))
        {
            players.Remove(playerTransform);
        }
    }

    private void SpawnPlayer(Transform playerPrefab, Vector3 spawnPosition)
    {
        Transform playerTransform = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        RegisterPlayer(playerTransform);
    }
}
