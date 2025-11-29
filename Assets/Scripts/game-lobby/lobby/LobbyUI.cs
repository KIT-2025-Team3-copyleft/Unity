using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance;

    [Header("UI References")]
    [SerializeField] private GameObject startButton;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerListItemPrefab;
    [SerializeField] private TextMeshProUGUI errorText;
    [SerializeField] private TMP_Text countdownText;  // 타이머 텍스트를 위한 TMP_Text 변수 추가

    private const string LOBBY_UI_ROOT_NAME = "LobbyUI_Root";

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
        TrySubscribe();

        SceneManager.sceneLoaded += OnSceneLoaded;
        RoomManager.Instance.OnErrorMessage -= ShowError;
        RoomManager.Instance.OnErrorMessage += ShowError;
        RoomManager.Instance.OnLobbyUpdated += UpdateLobbyUI;
    }

    private void OnDisable()
    {
        if (RoomManager.Instance != null)
            RoomManager.Instance.OnLobbyUpdated -= UpdateLobbyUI;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (RoomManager.Instance != null)
            RoomManager.Instance.OnLobbyUpdated -= UpdateLobbyUI;
    }

    // -----------------------------
    // 씬이 바뀌면 UI 루트 다시 찾기
    // -----------------------------
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "LobbyScene")
        {
            Debug.Log("[LobbyUI] LobbyScene detected → Rebinding UI references");
            StartCoroutine(RebindUI()); // UI 재바인딩
        }
    }

    private IEnumerator RebindUI()
    {
        yield return null;

        GameObject root = GameObject.Find(LOBBY_UI_ROOT_NAME);

        if (root == null)
        {
            Debug.LogWarning($"[LobbyUI] Cannot find {LOBBY_UI_ROOT_NAME} in scene.");
            yield break;
        }

        // 자동 재바인드
        playerCountText = root.transform.Find("PlayerCountText")?.GetComponent<TMP_Text>();
        startButton = root.transform.Find("StartButton")?.gameObject;
        playerListContainer = root.transform.Find("PlayerList/Viewport/Content");
        countdownText = root.transform.Find("CountdownText")?.GetComponent<TMP_Text>(); // 타이머 텍스트 바인딩

        // startButton이 null인 경우 확인
        if (startButton == null)
        {
            Debug.LogError("[LobbyUI] StartButton is missing in the scene!");
        }

        Debug.Log("[LobbyUI] UI Rebound successfully");

        // 방 정보를 다시 업데이트
        var room = RoomManager.Instance.CurrentRoom;
        if (room != null)
        {
            UpdateLobbyUI(room);
        }
    }

    // -----------------------------
    // RoomManager 이벤트 구독
    // -----------------------------
    private void TrySubscribe()
    {
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.OnLobbyUpdated -= UpdateLobbyUI;
            RoomManager.Instance.OnLobbyUpdated += UpdateLobbyUI;
            Debug.Log("[LobbyUI] Subscribed to RoomManager.OnLobbyUpdated");
        }
        else
        {
            StartCoroutine(WaitAndSubscribe());
        }
    }

    private IEnumerator WaitAndSubscribe()
    {
        while (RoomManager.Instance == null)
            yield return null;

        RoomManager.Instance.OnLobbyUpdated -= UpdateLobbyUI;
        RoomManager.Instance.OnLobbyUpdated += UpdateLobbyUI;

        Debug.Log("[LobbyUI] Subscribed after RoomManager ready");
    }

    public void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.gameObject.SetActive(true);
            errorText.text = message;
        }

        Debug.Log("[LobbyUI] ERROR: " + message);

        CancelInvoke(nameof(HideErrorText));
        Invoke(nameof(HideErrorText), 2f);
    }

    private void HideErrorText()
    {
        if (errorText != null)
            errorText.gameObject.SetActive(false);
    }

    // -----------------------------
    // Host 버튼 활성
    // -----------------------------

    // -----------------------------
    // 실제 UI 업데이트
    // -----------------------------
    public void UpdateLobbyUI(RoomManager.Room room)
    {
        if (SceneManager.GetActiveScene().name != "LobbyScene")
            return;

        // 플레이어 목록이 없다면 종료
        if (room.players == null || room.players.Length == 0) return;

        Debug.Log($"[LobbyUI] Updating Lobby UI, PlayerCount={room.players.Length}");

        // 1) 플레이어 수 표시
        if (playerCountText != null)
            playerCountText.text = $"{room.players.Length}/4";

        // 2) 기존 목록 삭제
        if (playerListContainer != null)
        {
            for (int i = playerListContainer.childCount - 1; i >= 0; i--)
                Destroy(playerListContainer.GetChild(i).gameObject);
        }

        // 3) 새로운 플레이어 리스트 생성
        string hostSessionId = room.hostSessionId;

        foreach (var p in room.players)
        {
            var obj = Instantiate(playerListItemPrefab, playerListContainer);
            var text = obj.GetComponentInChildren<TextMeshProUGUI>(true);

            if (text != null)
            {
                text.text = $"{p.nickname}" + (p.sessionId == hostSessionId ? " (Host)" : "");
            }
        }

        // 4) 본인이 호스트인지 판단해서 버튼 활성
        bool isLocalHost = WebSocketManager.Instance.ClientSessionId == hostSessionId;
        UpdateHostButton(isLocalHost);  // 호스트일 경우만 Start 버튼 활성화
    }

    public void UpdateHostButton(bool isHost)
    {
        if (startButton != null)
            startButton.SetActive(isHost);  // 호스트만 활성화
    }


    // -----------------------------
    // 게임 시작 처리
    // -----------------------------
    public void OnClickStartGame()
    {
        Debug.Log("[LobbyUI] Start Game button clicked!");

        var room = RoomManager.Instance.CurrentRoom; // 또는 currentRoom

        if (room == null)
        {
            Debug.LogError("[LobbyUI] Room is NULL!!");
            return;
        }

        // 호스트인지 확인
        if (room.hostSessionId != WebSocketManager.Instance.ClientSessionId)
        {
            Debug.LogWarning("[LobbyUI] Not host — cannot start game");
            return;
        }

        // 플레이어 수가 4명이 아닌 경우 게임 시작을 막음
        if (room.players.Length != 4)
        {
            Debug.LogWarning("[LobbyUI] 게임 시작을 위해서는 4명이 필요합니다.");
            ShowError("게임 시작을 위해서는 4명이 필요합니다.");
            return;
        }

        // 게임 시작 전에 잠시 대기 (플레이어 이름 설정이 완료될 때까지)
        StartCoroutine(WaitForPlayerNamesAndStartGame(room));
    }

    private IEnumerator WaitForPlayerNamesAndStartGame(RoomManager.Room room)
    {
        // 플레이어가 모두 스폰되고 이름이 설정될 때까지 대기
        while (!PlayerSpawnManager.Instance.playersSpawned)
        {
            yield return null;
        }

        // 게임 시작 메시지 서버로 전송
        string json = @"{ ""action"": ""START_GAME"" }";
        WebSocketManager.Instance.Send(json);

        Debug.Log("[LobbyUI] START_GAME sent to server");

        // 3초 후 게임 시작
        StartCoroutine(StartGameCountdown());
    }



    private IEnumerator StartGameCountdown()
    {
        // 3초 타이머 실행
        float countdown = 3f;
        while (countdown > 0)
        {
            // 타이머 텍스트 갱신
            if (countdownText != null)
                countdownText.text = $"게임 시작까지 {Mathf.Ceil(countdown)}초   남았습니다.";

            countdown -= Time.deltaTime;
            yield return null;
        }

        // 타이머 끝나면 게임 씬으로 전환
        Debug.Log("게임 시작!");
        SceneManager.LoadScene("GameScene");
    }
}
