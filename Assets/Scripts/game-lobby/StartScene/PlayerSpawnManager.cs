using UnityEngine;
using System.Collections.Generic;

public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance;

    public Transform[] spawnPoints;

    private Dictionary<string, GameObject> playerObjects = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public GameObject GetPlayerObject(string sessionId)
    {
        if (playerObjects.TryGetValue(sessionId, out GameObject obj))
            return obj;
        return null;
    }

    public void RegisterPlayer(string sessionId, GameObject playerObj)
    {
        if (!playerObjects.ContainsKey(sessionId))
            playerObjects.Add(sessionId, playerObj);
    }

    public GameObject SpawnPlayer(string sessionId, int playerNumber, GameObject playerPrefab)
    {
        int index = playerNumber - 1;
        if (spawnPoints.Length == 0 || index < 0)
        {
            Debug.LogError("스폰 포인트 인덱스 오류!");
            return null;
        }

        GameObject existing = GetPlayerObject(sessionId);
        if (existing != null)
        {
            existing.transform.position = spawnPoints[index % spawnPoints.Length].position;
            existing.transform.rotation = spawnPoints[index % spawnPoints.Length].rotation;
            return existing;
        }

        GameObject player = Instantiate(playerPrefab,
                                        spawnPoints[index % spawnPoints.Length].position,
                                        spawnPoints[index % spawnPoints.Length].rotation);
        RegisterPlayer(sessionId, player);
        return player;
    }
}
