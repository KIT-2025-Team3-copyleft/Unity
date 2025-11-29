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
            StartCoroutine(RebindUI());
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

        Debug.Log("[LobbyUI] UI Rebound successfully");
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
    public void UpdateHostButton(bool isHost)
    {
        if (startButton != null)
            startButton.SetActive(isHost);
    }

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
    UpdateHostButton(isLocalHost);
}

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

        // JSON 형태로 직접 보내기
        string json = @"{ ""action"": ""START_GAME"" }";

        WebSocketManager.Instance.Send(json);

        Debug.Log("[LobbyUI] START_GAME sent to server");
    }

}
