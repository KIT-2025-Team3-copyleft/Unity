using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance;

    [Header("세팅")]
    public string spawnRootName = "SpawnRoot";
    public GameObject[] playerPrefabs;
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

        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.OnLobbyUpdated += OnLobbyUpdated;
            Debug.Log("✔ PlayerSpawnManager Awake에서 이벤트 구독 완료");
        }
    }

    private IEnumerator Start()
    {
        while (RoomManager.Instance == null)
            yield return null;

        RoomManager.Instance.OnLobbyUpdated += OnLobbyUpdated;
    }

    private bool IsSubscribed()
    {
        // 단순 안전 체크: spawnedPlayers가 비어 있고 현재 Room이 있으면 구독된 상태인지 판단할 수 있음
        // (정교하게는 RoomManager에 구독자 체크 API를 만들면 좋음)
        return true; // 생략: 실제 환경에선 별도 플래그로 구독 여부 추적하는게 더 좋음
    }

    private void OnDestroy()
    {
        Debug.Log("▶ PlayerSpawnManager.OnDestroy() 실행됨");
        if (RoomManager.Instance != null)
            RoomManager.Instance.OnLobbyUpdated -= OnLobbyUpdated;
    }

    // (이하는 기존 OnLobbyUpdated, RefreshSpawnPoints 등 기존 코드 동일)
    private void RefreshSpawnPoints()
    {
        Debug.Log("▶ RefreshSpawnPoints() 실행됨");
        GameObject root = GameObject.Find(spawnRootName);
        if (root == null)
        {
            Debug.LogWarning("❌ SpawnRoot를 찾지 못함!");
            spawnPoints = new Transform[0];
            return;
        }

        spawnPoints = root.GetComponentsInChildren<Transform>(true)
                          .Where(t => t != root.transform)
                          .ToArray();

        Debug.Log($"✔ SpawnRoot 찾음: {root.name}, spawnPoint 개수={spawnPoints.Length}");
    }

    private void OnLobbyUpdated(RoomManager.Room room)
    {
        Debug.Log("▶ OnLobbyUpdated() 호출됨");
        if (room == null || room.players == null)
        {
            Debug.LogError("❌ OnLobbyUpdated: room or players null");
            return;
        }

        RefreshSpawnPoints();
        if (spawnPoints.Length == 0)
        {
            Debug.LogError("❌ SpawnPoints가 0개 → SpawnRoot가 안 보이나?");
            return;
        }

        string localSessionId = WebSocketManager.Instance.ClientSessionId;
        Debug.Log($"✔ 로컬 Session ID: {localSessionId}");

        HashSet<string> activeSessions = new HashSet<string>();
        foreach (var p in room.players) activeSessions.Add(p.sessionId);

        foreach (var kv in spawnedPlayers)
            if (!activeSessions.Contains(kv.Key)) kv.Value.SetActive(false);

        int count = Mathf.Min(room.players.Length, MaxPlayers);
        Debug.Log($"▶ 스폰 루프 시작: count={count}");

        for (int i = 0; i < count; i++)
        {
            var p = room.players[i];
            Transform spawnPoint = spawnPoints[i % spawnPoints.Length];
            Debug.Log($"→ {i}번 플레이어: {p.nickname}, sessionId={p.sessionId}, spawnPos={spawnPoint.position}");

            if (spawnedPlayers.ContainsKey(p.sessionId))
            {
                Debug.Log($"  ↳ 기존 플레이어 위치 갱신");
                GameObject obj = spawnedPlayers[p.sessionId];
                obj.SetActive(true);
                obj.transform.position = spawnPoint.position;
                obj.transform.rotation = spawnPoint.rotation;
                continue;
            }

            GameObject prefab = playerPrefabs[i % playerPrefabs.Length];
            if (prefab == null)
            {
                Debug.LogError($"❌ playerPrefabs[{i}]가 null");
                continue;
            }

            Debug.Log($"  ↳ Instantiate 시작 → prefab={prefab.name}");
            GameObject newPlayer = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, playerRoot);
            Debug.Log($"  ✔ Instantiate 완료 → {newPlayer.name}");
            spawnedPlayers.Add(p.sessionId, newPlayer);

            bool isLocal = p.sessionId == localSessionId;
            var cameras = newPlayer.GetComponentsInChildren<Camera>(true);
            foreach (var cam in cameras)
            {
                cam.gameObject.SetActive(isLocal);
                cam.tag = isLocal ? "MainCamera" : "Untagged";
            }

            Debug.Log($"  ✔ Spawn 완료 → nickname={p.nickname}, isLocal={isLocal}, pos={spawnPoint.position}");
        }

        Debug.Log("▶ OnLobbyUpdated() 종료");
    }
}
