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
    [SerializeField] private TMP_Text countdownText;  // 3초 카운트다운용

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // ❌ DontDestroyOnLoad 안 씀. 로비 씬 안에서만 존재
    }

    private void OnEnable()
    {
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.OnLobbyUpdated -= UpdateLobbyUI;
            RoomManager.Instance.OnLobbyUpdated += UpdateLobbyUI;

            RoomManager.Instance.OnErrorMessage -= ShowError;
            RoomManager.Instance.OnErrorMessage += ShowError;

            // 이미 방 정보가 있으면 한 번 바로 그림
            if (RoomManager.Instance.CurrentRoom != null)
            {
                UpdateLobbyUI(RoomManager.Instance.CurrentRoom);
            }
        }
    }

    private void OnDisable()
    {
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.OnLobbyUpdated -= UpdateLobbyUI;
            RoomManager.Instance.OnErrorMessage -= ShowError;
        }
    }

    // -----------------------------
    // 에러 표시
    // -----------------------------
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
    // 실제 UI 업데이트
    // -----------------------------
    public void UpdateLobbyUI(RoomManager.Room room)
    {
        if (SceneManager.GetActiveScene().name != "LobbyScene")
            return;

        if (room == null || room.players == null || room.players.Length == 0)
            return;

        Debug.Log($"[LobbyUI] Updating Lobby UI, PlayerCount={room.players.Length}");

        // 1) 인원 수 표시
        if (playerCountText != null)
            playerCountText.text = $"{room.players.Length}/4";

        // 2) 기존 목록 삭제
        if (playerListContainer != null)
        {
            for (int i = playerListContainer.childCount - 1; i >= 0; i--)
                Destroy(playerListContainer.GetChild(i).gameObject);
        }

        // 3) 새 리스트 생성 + Host 표시
        string hostSessionId = room.hostSessionId;
        RoomManager.PlayerData hostPlayer = null;

        foreach (var p in room.players)
        {
            if (p.sessionId == hostSessionId)
                hostPlayer = p;

            if (playerListItemPrefab != null && playerListContainer != null)
            {
                var obj = Instantiate(playerListItemPrefab, playerListContainer);
                var text = obj.GetComponentInChildren<TMP_Text>(true);

                if (text != null)
                {
                    bool isHostOfRoom = (p.sessionId == hostSessionId);
                    text.text = p.nickname + (isHostOfRoom ? " (Host)" : "");
                }
            }
        }

        // 4) 호스트 여부는 RoomManager.IsHost 를 그대로 신뢰
        bool isLocalHost = RoomManager.Instance != null && RoomManager.Instance.IsHost;
        bool showStart = isLocalHost;

        if (startButton != null)
            startButton.SetActive(showStart);

        string myNick = PlayerPrefs.GetString("PlayerNickname", "Guest");
        string hostNick = hostPlayer != null ? hostPlayer.nickname : "(null)";

        Debug.Log($"[LobbyUI] hostNick={hostNick}, myNick={myNick}, " +
                  $"IsHost(From RM)={isLocalHost}, players={room.players.Length}, showStart={showStart}");
    }

    // -----------------------------
    // 게임 시작 처리 (호스트만)
    // -----------------------------
    public void OnClickStartGame()
    {
        Debug.Log("[LobbyUI] Start Game button clicked!");

        var rm = RoomManager.Instance;
        var room = rm != null ? rm.CurrentRoom : null;

        if (rm == null || room == null || room.players == null)
        {
            Debug.LogError("[LobbyUI] RoomManager or Room is NULL");
            ShowError("방 정보가 없습니다.");
            return;
        }

        // 호스트인지 최종 확인 (세션ID 대신 RoomManager.IsHost 사용)
        if (!rm.IsHost)
        {
            ShowError("호스트만 게임을 시작할 수 있습니다.");
            Debug.LogWarning("[LobbyUI] Not host — cannot start game (IsHost == false)");
            return;
        }

        // 인원 제한 다시 걸고 싶으면 주석 해제
        // if (room.players.Length != 4)
        // {
        //     ShowError("게임 시작을 위해서는 4명이 필요합니다.");
        //     Debug.LogWarning("[LobbyUI] Need 4 players to start game");
        //     return;
        // }

        // 서버에 START_GAME 전송
        string json = @"{ ""action"": ""START_GAME"" }";
        WebSocketManager.Instance.Send(json);
        Debug.Log("[LobbyUI] START_GAME sent to server (by host)");

        // 로컬 카운트다운 (씬 전환은 서버 LOAD_GAME_SCENE 이벤트에서 처리)
        StartCoroutine(StartGameCountdown());
    }

    private IEnumerator StartGameCountdown()
    {
        float countdown = 3f;

        while (countdown > 0f)
        {
            if (countdownText != null)
                countdownText.text = $"게임 시작까지 {Mathf.Ceil(countdown)}초 남았습니다.";

            countdown -= Time.deltaTime;
            yield return null;
        }

        if (countdownText != null)
            countdownText.text = string.Empty;

        Debug.Log("[LobbyUI] Countdown finished. (씬 전환은 서버 LOAD_GAME_SCENE 처리에 맞춰 진행)");
        // SceneManager.LoadScene("GameScene");  // 서버에서 LOAD_GAME_SCENE 보낼 때 RoomManager 쪽에서 처리
    }
}
