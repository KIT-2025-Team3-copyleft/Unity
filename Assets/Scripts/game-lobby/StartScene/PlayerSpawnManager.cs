using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance;

    [Header("세팅")]
    public string spawnRootName = "SpawnRoot";
    public GameObject[] playerPrefabs;
    public Transform playerRoot;

    private Transform[] spawnPoints = new Transform[0];
    private Dictionary<string, GameObject> spawnedPlayers = new Dictionary<string, GameObject>();
    private const int MaxPlayers = 4;

    // 구독 상태 추적
    private bool isRoomSubscribed = false;

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

        // 씬 로드 콜백 등록 (씬 바뀔 때 정리)
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        TrySubscribeRoomManager();
    }

    private void OnDestroy()
    {
        if (RoomManager.Instance != null)
            RoomManager.Instance.OnLobbyUpdated -= OnLobbyUpdated;
    }

    private void TrySubscribeRoomManager()
    {
        if (isRoomSubscribed) return;

        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.OnLobbyUpdated -= OnLobbyUpdated; // 안전하게 중복 제거
            RoomManager.Instance.OnLobbyUpdated += OnLobbyUpdated;
            isRoomSubscribed = true;
            Debug.Log("✔ PlayerSpawnManager: RoomManager 구독 완료");
        }
        else
        {
            // RoomManager가 아직 없으면 StartCoroutine으로 대기 후 구독 시도
            StartCoroutine(WaitAndSubscribeRoomManager());
        }
    }

    private IEnumerator WaitAndSubscribeRoomManager()
    {
        float timeout = 5f;
        float t = 0f;
        while (RoomManager.Instance == null && t < timeout)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.OnLobbyUpdated -= OnLobbyUpdated;
            RoomManager.Instance.OnLobbyUpdated += OnLobbyUpdated;
            isRoomSubscribed = true;
            Debug.Log("✔ PlayerSpawnManager: (대기 후) RoomManager 구독 완료");
        }
        else
        {
            Debug.LogWarning("⚠ PlayerSpawnManager: RoomManager를 찾지 못해 이벤트 구독 실패(타임아웃)");
        }
    }

    private IEnumerator ActivateLocalCamera()
    {
        // spawnedPlayers가 채워질 때까지 대기
        yield return new WaitUntil(() =>
            spawnedPlayers.Values.Any(go => go.name.Contains(WebSocketManager.Instance.ClientSessionId)));

        var localPlayer = spawnedPlayers.Values
            .First(go => go.name.Contains(WebSocketManager.Instance.ClientSessionId));

        var cam = localPlayer.GetComponentInChildren<Camera>(true);
        if (cam != null)
        {
            // 기존 MainCamera 태그 제거
            var allMainCams = GameObject.FindGameObjectsWithTag("MainCamera");
            foreach (var c in allMainCams)
            {
                if (c != cam.gameObject)
                    c.tag = "Untagged";
            }

            cam.gameObject.SetActive(true);
            cam.tag = "MainCamera";
            Debug.Log("Local camera activated!");
        }
    }

    private void UnsubscribeRoomManager()
    {
        if (!isRoomSubscribed) return;
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.OnLobbyUpdated -= OnLobbyUpdated;
        }
        isRoomSubscribed = false;
    }

    // 씬 로드 시 정리 (Destroyed 된 오브젝트 정리 및 SpawnPoints 리프레시)
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 제거될(이미 Destroy된) spawnedPlayers 정리
        var deadKeys = spawnedPlayers.Where(kv => kv.Value == null).Select(kv => kv.Key).ToList();
        foreach (var k in deadKeys) spawnedPlayers.Remove(k);
        if (deadKeys.Count > 0)
            Debug.Log($"✔ SceneLoaded: Destroy된 플레이어 정리됨 ({deadKeys.Count})");

        // 씬이 바뀌었으니 SpawnPoint도 다시 찾기 시도
        StartCoroutine(ReTryFindSpawnRoot());
    }

    #region SpawnPoint 찾기
    private void RefreshSpawnPoints()
    {
        Debug.Log("▶ RefreshSpawnPoints() 실행됨");
        GameObject root = GameObject.Find(spawnRootName);
        if (root == null)
        {
            Debug.LogWarning("⚠ SpawnRoot를 찾지 못함. 재시도 예약.");
            spawnPoints = new Transform[0];
            // 재시도를 coroutine으로 수행
            StartCoroutine(ReTryFindSpawnRoot());
            return;
        }

        // 포함된 Transform들 중 Root 자신은 제외
        spawnPoints = root.GetComponentsInChildren<Transform>(true)
                          .Where(t => t != root.transform)
                          .ToArray();

        Debug.Log($"✔ SpawnRoot 찾음: {root.name}, spawnPoint 개수={spawnPoints.Length}");
    }

    private IEnumerator ReTryFindSpawnRoot(float maxWait = 1.0f, float interval = 0.05f)
    {
        float waited = 0f;
        while (waited < maxWait)
        {
            GameObject root = GameObject.Find(spawnRootName);
            if (root != null)
            {
                spawnPoints = root.GetComponentsInChildren<Transform>(true)
                                  .Where(t => t != root.transform)
                                  .ToArray();
                Debug.Log($"✔ ReTryFindSpawnRoot 성공: spawnPoint 개수={spawnPoints.Length}");
                yield break;
            }

            yield return new WaitForSeconds(interval);
            waited += interval;
        }

        Debug.LogWarning("❌ ReTryFindSpawnRoot: SpawnRoot를 찾지 못함 (타임아웃)");
    }
    #endregion
   

    private void OnLobbyUpdated(RoomManager.Room room)
    {
        if (SceneManager.GetActiveScene().name != "LobbyScene")
            return;
        // 방 정보가 이상하면 무시
        if (room == null || room.players == null)
        {
            Debug.LogError("❌ OnLobbyUpdated: room or players null");
            return;
        }

        // 안전: RoomManager에서 콜백을 보내는 동안 씬이 바뀔 수 있으므로 TrySubscribe 한 번 더 보장
        TrySubscribeRoomManager();

        // SpawnPoints 최신화 시도 (없으면 ReTry 코루틴이 동작함)
        RefreshSpawnPoints();

        StartCoroutine(SpawnPlayersAndUpdateUI(room));

        // spawnPoints 배열 자체에 Destroy된 Transform이 들어있을 수 있음 -> 필터링해서 로컬 복사
        var localSpawnPoints = spawnPoints?.Where(t => t != null).ToArray() ?? new Transform[0];
        if (localSpawnPoints.Length == 0)
        {
            Debug.LogError("❌ SpawnPoints가 0개 → SpawnRoot가 안 보이나? (또는 모두 Destroy됨)");
            return;
        }

        string localSessionId = WebSocketManager.Instance != null ? WebSocketManager.Instance.ClientSessionId : "";
        Debug.Log($"✔ 로컬 Session ID: {localSessionId}");

        var activeSessions = new HashSet<string>(room.players.Select(p => p.sessionId));

        // spawnedPlayers에서 더 이상 없는 세션 제거 및 null 참조 정리
        var spawnedKeys = spawnedPlayers.Keys.ToList(); // 안전한 반복
        foreach (var key in spawnedKeys)
        {
            if (!activeSessions.Contains(key) || spawnedPlayers[key] == null)
            {
                // 비활성 처리 혹은 제거
                if (spawnedPlayers.TryGetValue(key, out var go) && go != null)
                    Destroy(go);
                spawnedPlayers.Remove(key);
            }
        }

        int count = Mathf.Min(room.players.Length, MaxPlayers);
        Debug.Log($"▶ 스폰 루프 시작: count={count}");

        for (int i = 0; i < count; i++)
        {
            var p = room.players[i];

            // 안전: spawn index가 localSpawnPoints 범위 내인지 체크
            if (localSpawnPoints.Length == 0)
            {
                Debug.LogError("❌ 유효한 spawnPoint 없음. 루프 중단.");
                break;
            }
            Transform spawnPoint = localSpawnPoints[i % localSpawnPoints.Length];

            // spawnPoint가 null이면 건너뜀(씬 전환 중일 수 있음)
            if (spawnPoint == null)
            {
                Debug.LogWarning($"⚠ spawnPoint[{i}]가 null(파괴됨). 건너뜀.");
                continue;
            }

            // 지금 로그 출력(안전하게 position 접근)
            Vector3 spawnPos;
            Quaternion spawnRot;
            try
            {
                spawnPos = spawnPoint.position;
                spawnRot = spawnPoint.rotation;
            }
            catch (MissingReferenceException)
            {
                Debug.LogWarning("⚠ spawnPoint의 Transform이 파괴됨(참조 무효). 건너뜀.");
                continue;
            }

            Debug.Log($"→ {i}번 플레이어: {p.nickname}, sessionId={p.sessionId}, spawnPos={spawnPos}");

            // 기존에 스폰된 플레이어가 있으면 재사용(하지만 null 검사 필수)
            if (spawnedPlayers.ContainsKey(p.sessionId))
            {
                GameObject obj = spawnedPlayers[p.sessionId];
                if (obj == null)
                {
                    Debug.LogWarning($"⚠ 기존 플레이어 오브젝트가 Destroy됨 (session={p.sessionId}). 다시 생성합니다.");
                    spawnedPlayers.Remove(p.sessionId);
                }
                else
                {
                    Debug.Log($"  ↳ 기존 플레이어 위치 갱신");
                    // 안전하게 SetActive/transform 접근
                    try
                    {
                        obj.SetActive(true);
                        obj.transform.position = spawnPos;
                        obj.transform.rotation = spawnRot;
                    }
                    catch (MissingReferenceException)
                    {
                        Debug.LogWarning("⚠ 기존 오브젝트 접근 도중 MissingReference 발생. 제거 후 재생성.");
                        spawnedPlayers.Remove(p.sessionId);
                    }
                    continue;
                }
            }

            // 새로 생성
            GameObject prefab = playerPrefabs.Length > 0 ? playerPrefabs[i % playerPrefabs.Length] : null;
            if (prefab == null)
            {
                Debug.LogError($"❌ playerPrefabs[{i}]가 null 또는 playerPrefabs 비어있음");
                continue;
            }

            Debug.Log($"  ↳ Instantiate 시작 → prefab={prefab.name}");
            GameObject newPlayer = null;
            try
            {
                newPlayer = Instantiate(prefab, spawnPos, spawnRot, playerRoot);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Instantiate 실패: {ex}");
                continue;
            }

            if (newPlayer == null)
            {
                Debug.LogError("❌ Instantiate 했지만 null 반환됨");
                continue;
            }

            newPlayer.name = $"{prefab.name}_{p.nickname}";
            spawnedPlayers[p.sessionId] = newPlayer;

            bool isLocal = p.sessionId == localSessionId;
            var cameras = newPlayer.GetComponentsInChildren<Camera>(true);
            foreach (var cam in cameras)
            {
                cam.gameObject.SetActive(isLocal);
                cam.tag = isLocal ? "MainCamera" : "Untagged";
            }

            Debug.Log($"  ✔ Spawn 완료 → nickname={p.nickname}, isLocal={isLocal}, pos={spawnPos}");
        }


        foreach (var kv in spawnedPlayers)
            Debug.Log($"spawnedPlayers key={kv.Key}, name={kv.Value?.name}");

        /// 플레이어 카메라 및 UI 활성화 코드
        // 모든 플레이어 카메라를 관리하면서, 로컬 플레이어 카메라만 활성화
        bool isHost = room.hostSessionId == localSessionId;

        int playerIndex = 0;
        foreach (var pObj in spawnedPlayers.Values)
        {
            if (pObj == null) continue;

            // 각 플레이어 카메라는 활성화
            var cams = pObj.GetComponentsInChildren<Camera>(true);
            foreach (var c in cams)
            {
                c.gameObject.SetActive(true);

                // 각 플레이어에 맞는 태그를 할당 (순서대로 model1, model2, model3, model4)
                if (playerIndex == 0)
                    c.tag = "model1";
                else if (playerIndex == 1)
                    c.tag = "model2";
                else if (playerIndex == 2)
                    c.tag = "model3";
                else if (playerIndex == 3)
                    c.tag = "model4";

                playerIndex++;
            }

            // UI를 각 플레이어 카메라에 맞게 보여지도록 설정
            var canvas = pObj.GetComponentInChildren<Canvas>(true);
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                var cam = cams.FirstOrDefault();
                if (cam != null)
                    canvas.worldCamera = cam;  // 해당 플레이어 카메라를 UI의 worldCamera로 설정
            }
        }

        // 로컬 플레이어 찾기
        var localPlayer = spawnedPlayers.Values.FirstOrDefault(go => go.name.Contains(localSessionId));
        if (localPlayer != null)
        {
            var localCam = localPlayer.GetComponentInChildren<Camera>(true);
            if (localCam != null)
            {
                // 로컬 플레이어의 카메라는 활성화, 다른 카메라는 비활성화
                foreach (var cam in spawnedPlayers.Values.SelectMany(p => p.GetComponentsInChildren<Camera>(true)))
                {
                    if (cam.tag != "Untagged" && !cam.tag.Equals(localCam.tag))
                    {
                        cam.gameObject.SetActive(false);  // 다른 카메라는 비활성화
                    }
                }

                // 로컬 카메라는 그대로 활성화
                localCam.tag = "MainCamera";
            }

            // 게임 시작 버튼 처리 (호스트일 경우만 활성화)
            var startButton = GameObject.Find("StartButton");
            if (startButton != null)
            {
                startButton.SetActive(isHost);  // 호스트만 활성화
            }
        }

        if (playerPrefabs.Length == 0)
        {
            Debug.LogError("❌ playerPrefabs 배열이 비어있습니다.");
            return;
        }

        if (spawnPoints.Length == 0)
        {
            Debug.LogError("❌ spawnPoints 배열이 비어있습니다.");
            return;
        }

        Debug.Log("▶ OnLobbyUpdated() 종료");


    }

    private IEnumerator SpawnPlayersAndUpdateUI(RoomManager.Room room)
    {
        // 플레이어 스폰
        PlayerSpawnManager.Instance.SpawnPlayers(room);

        // 스폰 완료까지 대기 (여기서 1초 정도 딜레이를 추가)
        yield return new WaitForSeconds(1f); // 스폰 후 약간의 여유를 두기 위해 1초 대기

        // playersSpawned 플래그가 true로 설정될 때까지 기다림
        yield return new WaitUntil(() => PlayerSpawnManager.Instance.playersSpawned); // 플레이어 스폰 완료될 때까지 기다리기

        // UI 업데이트는 스폰 후에 실행
        Debug.Log("[PlayerSpawnManager] UI 업데이트는 자동으로 처리됩니다.");
    }

    public bool playersSpawned = false;
    public void SpawnPlayers(RoomManager.Room room)
    {
        if (room == null || room.players == null || room.players.Length == 0)
        {
            Debug.LogError("❌ SpawnPlayers failed: No players found in room.");
            return;
        }

        // 이미 플레이어가 스폰되었으면 다시 스폰하지 않음
        if (playersSpawned)
        {
            Debug.Log("Players have already been spawned.");
            return;
        }

        for (int i = 0; i < room.players.Length; i++)
        {
            var playerData = room.players[i];
            Transform spawnPoint = spawnPoints[i % spawnPoints.Length];

            if (spawnPoint != null)
            {
                Vector3 spawnPos = spawnPoint.position;
                Quaternion spawnRot = spawnPoint.rotation;

                // playerNumber가 없으면 할당하기
                if (playerData.playerNumber == -1) // 예시: -1이면 아직 playerNumber가 할당되지 않은 상태
                {
                    playerData.playerNumber = i;  // 플레이어 순서대로 번호 할당
                    Debug.Log($"Assigning playerNumber: {playerData.nickname} -> {playerData.playerNumber}");
                }

                // Ensure prefab is not null
                GameObject prefab = playerPrefabs.Length > 0 ? playerPrefabs[i % playerPrefabs.Length] : null;
                if (prefab == null)
                {
                    Debug.LogError($"❌ playerPrefabs[{i}]가 null 또는 playerPrefabs 비어있음");
                    continue;
                }

                GameObject newPlayer = Instantiate(prefab, spawnPos, spawnRot, playerRoot);
                var camera = newPlayer.GetComponentInChildren<Camera>();
                if (camera != null)
                {
                    bool isLocalPlayer = (playerData.sessionId == WebSocketManager.Instance.ClientSessionId);
                    camera.gameObject.SetActive(isLocalPlayer);
                    camera.tag = isLocalPlayer ? "MainCamera" : "Untagged";
                }

                spawnedPlayers[playerData.sessionId] = newPlayer;

                // 이름 설정을 완료하고 그 후에 playersSpawned 플래그를 true로 설정
                newPlayer.name = $"{prefab.name}_{playerData.nickname}";
                Debug.Log($"Player {playerData.nickname} spawned with playerNumber {playerData.playerNumber}");
            }
        }

        // 플레이어가 모두 스폰된 후 flag 설정
        playersSpawned = true;

        Debug.Log($"✔ {room.players.Length} players spawned.");
    }


}
