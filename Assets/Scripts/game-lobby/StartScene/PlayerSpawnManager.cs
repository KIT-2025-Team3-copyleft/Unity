using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance { get; private set; }

    [Header("Spawn Root / Points")]
    [SerializeField] private Transform spawnRoot;      // Spawn1~4가 자식으로 있는 오브젝트
    [SerializeField] private Transform[] spawnPoints;  // 실제 자리(Transform)들

    [Header("Player Prefabs (4개)")]
    [SerializeField] private GameObject[] playerPrefabs;

    [Header("Parent for Players")]
    [SerializeField] private Transform playerRoot;     // 플레이어를 담아둘 부모

    // sessionId → 해당 플레이어 오브젝트
    private readonly Dictionary<string, GameObject> playersBySessionId = new();

    public bool playersSpawned { get; private set; }

    // =====================================================================
    //  싱글톤 & 생명주기
    // =====================================================================
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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (RoomManager.Instance != null)
            RoomManager.Instance.OnLobbyUpdated += OnLobbyUpdated;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (RoomManager.Instance != null)
            RoomManager.Instance.OnLobbyUpdated -= OnLobbyUpdated;
    }

    // =====================================================================
    //  씬 로딩될 때 SpawnPoint 찾기
    // =====================================================================
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "LobbyScene")
            return;

        RefreshSpawnPoints();

        // 로비 씬으로 들어왔는데 이미 방 정보가 있다면, 바로 스폰 시도
        if (RoomManager.Instance != null && RoomManager.Instance.CurrentRoom != null)
        {
            SpawnPlayers(RoomManager.Instance.CurrentRoom);
        }
    }

    private void RefreshSpawnPoints()
    {
        Debug.Log("▶ RefreshSpawnPoints() 실행됨");

        if (spawnRoot == null)
        {
            var rootObj = GameObject.Find("SpawnRoot");
            spawnRoot = rootObj != null ? rootObj.transform : null;
        }

        if (spawnRoot == null)
        {
            Debug.LogError("❌ SpawnRoot를 찾지 못했습니다. 이름이 'SpawnRoot'인지 확인하세요.");
            spawnPoints = Array.Empty<Transform>();
            return;
        }

        // SpawnRoot의 자식들을 전부 SpawnPoint로 사용
        List<Transform> list = new List<Transform>();
        foreach (Transform child in spawnRoot)
        {
            list.Add(child);
        }

        spawnPoints = list.ToArray();
        Debug.Log($"✔ SpawnPoint 개수 = {spawnPoints.Length}");
    }

    // =====================================================================
    //  RoomManager에서 LOBBY_UPDATE 올 때마다 호출
    // =====================================================================
    private void OnLobbyUpdated(RoomManager.Room room)
    {
        if (SceneManager.GetActiveScene().name != "LobbyScene")
        {
            // 로비씬 아닐 때는 스폰 안 함
            return;
        }

        SpawnPlayers(room);
    }

    // =====================================================================
    //  실제 스폰 로직
    // =====================================================================
    public void SpawnPlayers(RoomManager.Room room)
    {
        if (room == null || room.players == null || room.players.Length == 0)
        {
            Debug.LogError("❌ SpawnPlayers: room 또는 players가 비어 있음");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("❌ SpawnPlayers: spawnPoints가 아직 설정되지 않음. RefreshSpawnPoints 먼저 필요");
            return;
        }

        string myNick = PlayerPrefs.GetString("PlayerNickname", "Guest");

        Debug.Log($"[SpawnDebug] ===== SpawnPlayers 시작: players={room.players.Length} =====");

        playersSpawned = false;

        // 이번 프레임에 실제로 존재해야 하는 세션 ID 목록
        HashSet<string> aliveSessionIds = new HashSet<string>();

        for (int i = 0; i < room.players.Length; i++)
        {
            var p = room.players[i];
            if (string.IsNullOrEmpty(p.sessionId))
                continue;

            // 자리 인덱스는 playerNumber 우선 사용, 없으면 i
            int seatIndex = p.playerNumber >= 0 ? p.playerNumber : i;
            Transform spawnPoint = spawnPoints[seatIndex % spawnPoints.Length];

            Vector3 pos = spawnPoint.position;
            Quaternion rot = spawnPoint.rotation;

            aliveSessionIds.Add(p.sessionId);

            // 이미 존재하면 위치만 갱신
            if (playersBySessionId.TryGetValue(p.sessionId, out GameObject existing) && existing != null)
            {
                existing.transform.SetPositionAndRotation(pos, rot);
                SetupCameras(existing, p.nickname == myNick);

                Debug.Log($"[SpawnDebug] ↻ Move player={p.nickname}, playerNumber={p.playerNumber}, " +
                          $"seatIndex={seatIndex}, pos={pos}, rot={rot.eulerAngles}");
            }
            else
            {
                // 새로 생성
                GameObject prefab = playerPrefabs[seatIndex % playerPrefabs.Length];
                GameObject obj = Instantiate(prefab, pos, rot, playerRoot);
                obj.name = $"{prefab.name}_{p.nickname}";

                playersBySessionId[p.sessionId] = obj;
                SetupCameras(obj, p.nickname == myNick);

                Debug.Log($"[SpawnDebug] ✚ Instantiate player={p.nickname}, playerNumber={p.playerNumber}, " +
                          $"seatIndex={seatIndex}, pos={pos}, rot={rot.eulerAngles}");
            }
        }

        // 방에서 사라진 플레이어는 정리
        List<string> toRemove = new List<string>();
        foreach (var kv in playersBySessionId)
        {
            if (!aliveSessionIds.Contains(kv.Key))
            {
                if (kv.Value != null)
                    Destroy(kv.Value);

                toRemove.Add(kv.Key);
            }
        }
        foreach (var key in toRemove)
        {
            playersBySessionId.Remove(key);
        }

        ActivateOnlyLocalCamera();
        playersSpawned = true;

        Debug.Log($"✔ [SpawnDebug] SpawnPlayers 완료. 현재 인원: {playersBySessionId.Count}");
    }

    // =====================================================================
    //  카메라 세팅 / 한 개만 활성화
    // =====================================================================
    private void SetupCameras(GameObject playerObj, bool isLocal)
    {
        var cams = playerObj.GetComponentsInChildren<Camera>(true);
        foreach (var cam in cams)
        {
            cam.gameObject.SetActive(isLocal);
            cam.tag = isLocal ? "MainCamera" : "Untagged";
        }
    }

    private void ActivateOnlyLocalCamera()
    {
        var cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        if (cams == null || cams.Length == 0) return;

        Camera main = null;
        foreach (var cam in cams)
        {
            if (cam.CompareTag("MainCamera"))
            {
                main = cam;
                break;
            }
        }

        if (main == null)
        {
            Debug.LogWarning("[SpawnDebug] MainCamera 태그를 가진 카메라가 없음");
            return;
        }

        foreach (var cam in cams)
        {
            cam.enabled = (cam == main);
        }

        Debug.Log($"[SpawnDebug] LOCAL MAIN CAMERA: {main.name}, " +
                  $"pos={main.transform.position}, rot={main.transform.rotation.eulerAngles}");
    }
}
