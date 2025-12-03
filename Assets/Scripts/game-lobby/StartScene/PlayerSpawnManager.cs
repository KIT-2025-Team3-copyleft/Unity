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

        if (playerRoot != null)
    {
            DontDestroyOnLoad(playerRoot.gameObject);
        }
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

        // RoomManager.Instance가 Destroy되었을 경우를 대비하여 null 체크를 유지합니다.
        if (RoomManager.Instance != null)
            RoomManager.Instance.OnLobbyUpdated -= OnLobbyUpdated;
    }

    // =====================================================================
    //  씬 로딩될 때 SpawnPoint 찾기
    // =====================================================================
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "LobbyScene") return;

        RefreshSpawnPoints();

        StartCoroutine(DelayedSpawn());
    }

    // PlayerSpawnManager.cs
    private IEnumerator DelayedSpawn()
    {
        yield return null;
        yield return null;

        // 🌟 이 로그가 출력되는지 확인하세요!
        Debug.Log($"[DelayedSpawn] RoomManager.Instance is: {RoomManager.Instance}");

        if (RoomManager.Instance == null)
        {
            Debug.LogError("[DelayedSpawn] RoomManager.Instance가 null입니다. SpawnPlayers를 호출할 수 없습니다.");
            yield break;
        }

        SpawnPlayers(RoomManager.Instance.CurrentRoom);
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
            return;

        SpawnPlayers(room);
    }

    // =====================================================================
    //  실제 스폰 로직 (PlayerSpawner의 로직 흡수)
    // =====================================================================
    public void SpawnPlayers(RoomManager.Room room)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("❌ SpawnPlayers: GameManager.Instance가 null이어서 플레이어 세션 ID를 가져올 수 없습니다. 초기화 순서를 확인하세요.");
            return;
        }


        if (room == null || room.players == null || room.players.Length == 0)
        {
            Debug.LogError("❌ SpawnPlayers: room 또는 players가 비어 있음");

            // 플레이어가 모두 나갔을 경우, 남아있던 오브젝트들을 정리합니다.
            ClearExistingPlayers();
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("❌ SpawnPlayers: spawnPoints가 아직 설정되지 않음. RefreshSpawnPoints 먼저 필요");
            return;
        }

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
            if (seatIndex >= spawnPoints.Length)
            {
                Debug.LogWarning("스폰 포인트가 부족합니다. 이 플레이어는 스폰되지 않습니다.");
                continue; // 스폰 포인트가 부족하면 건너뜁니다.
            }

            Transform spawnPoint = spawnPoints[seatIndex]; // % spawnPoints.Length는 필요하지 않음.

            Vector3 pos = spawnPoint.position;
            Quaternion rot = spawnPoint.rotation;

            string mySessionId = GameManager.Instance.MySessionId; // 중복 선언 방지
            Debug.Log($"[ID Check] Comparing Local ID: {mySessionId} with Player ID: {p.sessionId}");
            bool isLocal = (p.sessionId == mySessionId); // 👈 이 코드는 그대로 유지

            aliveSessionIds.Add(p.sessionId);

            aliveSessionIds.Add(p.sessionId);

            GameObject playerObj;
            PlayerManager pm;

            // 이미 존재하면 위치만 갱신
            if (playersBySessionId.TryGetValue(p.sessionId, out playerObj) && playerObj != null)
            {
                playerObj.transform.SetPositionAndRotation(pos, rot);

                // PlayerManager 업데이트 (호스트 여부, 색상 등)
                pm = playerObj.GetComponent<PlayerManager>();
                if (pm != null)
                {
                    pm.isHost = (p.sessionId == room.hostSessionId);
                    pm.SetColor(p.color);
                }

                Debug.Log($"[SpawnDebug] ↻ Move player={p.nickname}, playerNumber={p.playerNumber}, " +
                          $"seatIndex={seatIndex}, pos={pos}, rot={rot.eulerAngles}");
            }
            else
            {
                // 새로 생성
                // playerPrefabs 배열 길이 초과 방지
                GameObject prefab = playerPrefabs[seatIndex % playerPrefabs.Length];
                playerObj = Instantiate(prefab, pos, rot, playerRoot);
                DontDestroyOnLoad(playerObj);
                playerObj.name = $"{prefab.name}_{p.nickname}";

                playersBySessionId[p.sessionId] = playerObj;

                // 🌟 PlayerManager 컴포넌트 설정 (PlayerSpawner.SpawnPlayer 로직)
                pm = playerObj.GetComponent<PlayerManager>();
                if (pm == null) pm = playerObj.AddComponent<PlayerManager>();

                pm.playerId = p.sessionId;
                pm.nickname = p.nickname;

                // GameManager에 등록 (PlayerSpawner.SpawnPlayer 로직)
                GameManager.Instance.AddPlayer(p.sessionId, pm);

                // 로비 정보 업데이트 (PlayerSpawner.UpdateLobbyPlayers 로직)
                pm.SetColor(p.color);
                pm.isHost = (p.sessionId == room.hostSessionId);

                // 🌟 카메라/캔버스/리스너 활성화/비활성화 (PlayerSpawner.SpawnPlayer 로직)
                AudioListener listener = playerObj.GetComponentInChildren<AudioListener>(true);
                Transform canvasTransform = playerObj.transform.Find("Canvas");
                Camera cam = playerObj.GetComponentInChildren<Camera>(true);

                // --------------------------------------------------------------------------
                // 1. 기본 설정 및 원격 플레이어 처리
                // --------------------------------------------------------------------------
                if (cam != null)
                {
                    cam.enabled = isLocal;
                    if (isLocal)
                    {
                        GameManager.Instance.firstPersonCamera = cam;
                    }
                }

                if (canvasTransform != null)
                {
                    canvasTransform.gameObject.SetActive(false);
                }

                if (listener != null)
                {
                    if (!isLocal)
                    {
                        Destroy(listener);
                    }
                    else
                    {
                        listener.enabled = true;
                    }
                }

                if (isLocal)
                {
                    GameManager.Instance.LinkLocalPlayerUI(playerObj);
                }
            }

            // 방에서 사라진 플레이어는 정리
            ClearRemovedPlayers(aliveSessionIds);

            playersSpawned = true;

            Debug.Log($"✔ [SpawnDebug] SpawnPlayers 완료. 현재 인원: {playersBySessionId.Count}");
        }
    }

    private void ClearExistingPlayers()
    {
        foreach (var kv in playersBySessionId)
        {
            if (kv.Value != null)
                Destroy(kv.Value);
        }
        playersBySessionId.Clear();
        // GameManager에서도 정리하는 로직이 있다면 추가합니다.
    }

    private void ClearRemovedPlayers(HashSet<string> aliveSessionIds)
    {
        List<string> toRemove = new List<string>();
        foreach (var kv in playersBySessionId)
        {
            if (!aliveSessionIds.Contains(kv.Key))
            {
                if (kv.Value != null)
                {
                    Destroy(kv.Value);
                    // GameManager에서 플레이어 정보를 제거하는 로직이 있다면 여기서 호출해야 합니다.
                    // GameManager.Instance.RemovePlayer(kv.Key);
                }

                toRemove.Add(kv.Key);
            }
        }
        foreach (var key in toRemove)
        {
            playersBySessionId.Remove(key);
        }
    }
}