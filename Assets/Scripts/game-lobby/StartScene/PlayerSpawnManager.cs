using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance;

    [Header("세팅")]
    public string spawnRootName = "SpawnRoot";
    public GameObject[] playerPrefabs; // 4개 프리팹 지정
    public Transform playerRoot;

    private Transform[] spawnPoints;
    private Dictionary<string, GameObject> spawnedPlayers = new Dictionary<string, GameObject>();
    private const int MaxPlayers = 4;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (RoomManager.Instance != null)
            RoomManager.Instance.OnLobbyUpdated += OnLobbyUpdated;
    }

    private void OnDestroy()
    {
        if (RoomManager.Instance != null)
            RoomManager.Instance.OnLobbyUpdated -= OnLobbyUpdated;
    }

    private void RefreshSpawnPoints()
    {
        GameObject root = GameObject.Find(spawnRootName);
        if (root != null)
            spawnPoints = root.GetComponentsInChildren<Transform>();
        else
            spawnPoints = new Transform[0];
    }

    private void OnLobbyUpdated(RoomManager.Room room)
    {
        if (room == null || room.players == null) return;

        RefreshSpawnPoints();
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        string localSessionId = WebSocketManager.Instance.ClientSessionId;
        var activeIds = new HashSet<string>();
        foreach (var p in room.players)
            activeIds.Add(p.sessionId);

        // 기존 플레이어 비활성화
        foreach (var kv in spawnedPlayers)
        {
            if (!activeIds.Contains(kv.Key))
                kv.Value.SetActive(false);
        }

        // 최대 4명만 처리
        int count = Mathf.Min(room.players.Length, MaxPlayers);

        for (int i = 0; i < count; i++)
        {
            var p = room.players[i];
            Transform spawnPoint = spawnPoints[i % spawnPoints.Length];
            if (spawnPoint == null) continue;

            if (spawnedPlayers.ContainsKey(p.sessionId))
            {
                var existingObj = spawnedPlayers[p.sessionId];
                existingObj.SetActive(true);
                existingObj.transform.position = spawnPoint.position;
                continue;
            }

            // 지정한 프리팹 사용
            GameObject prefab = playerPrefabs[i % playerPrefabs.Length];
            GameObject obj = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, playerRoot);
            spawnedPlayers.Add(p.sessionId, obj);

            bool isLocal = p.sessionId == localSessionId;

            // 카메라 설정
            var cameras = obj.GetComponentsInChildren<Camera>(true);
            foreach (var cam in cameras)
            {
                cam.gameObject.SetActive(isLocal);
                cam.tag = isLocal ? "MainCamera" : "Untagged";
            }

            var model = obj.transform.Find("Model");
            if (model != null) model.gameObject.SetActive(true);

            Debug.Log($"Spawned {p.nickname} at {spawnPoint.position}, prefab={prefab.name}, isLocal={isLocal}");
        }
    }
}
