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
    [SerializeField] private TMP_Text countdownText;  // 3초 카운트다운용 (화면 중앙 크게 배치 추천)

    private Coroutine countdownRoutine;
    private bool countdownCancelled = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // 로비씬 안에서만 존재 (DontDestroyOnLoad 안 씀)
    }

    private void OnEnable()
    {
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.OnLobbyUpdated -= UpdateLobbyUI;
            RoomManager.Instance.OnErrorMessage -= ShowError;
            RoomManager.Instance.OnGameStartTimer -= HandleGameStartTimer;
            RoomManager.Instance.OnTimerCancelled -= HandleTimerCancelled;
            RoomManager.Instance.OnLoadGameScene -= HandleLoadGameScene;

            RoomManager.Instance.OnLobbyUpdated += UpdateLobbyUI;
            RoomManager.Instance.OnErrorMessage += ShowError;
            RoomManager.Instance.OnGameStartTimer += HandleGameStartTimer;
            RoomManager.Instance.OnTimerCancelled += HandleTimerCancelled;
            RoomManager.Instance.OnLoadGameScene += HandleLoadGameScene;

            // 이미 방 정보가 있으면 한 번 바로 그림
            if (RoomManager.Instance.CurrentRoom != null)
            {
                UpdateLobbyUI(RoomManager.Instance.CurrentRoom);
            }
        }

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
            countdownText.text = string.Empty;
        }
    }

    private void OnDisable()
    {
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.OnLobbyUpdated -= UpdateLobbyUI;
            RoomManager.Instance.OnErrorMessage -= ShowError;
            RoomManager.Instance.OnGameStartTimer -= HandleGameStartTimer;
            RoomManager.Instance.OnTimerCancelled -= HandleTimerCancelled;
            RoomManager.Instance.OnLoadGameScene -= HandleLoadGameScene;
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
        bool showStart = isLocalHost;   // 인원 제한을 여기서도 걸고 싶으면 && room.players.Length == 4;

        if (startButton != null)
            startButton.SetActive(showStart);

        string myNick = RoomManager.Instance != null ? RoomManager.Instance.MyNickname : PlayerPrefs.GetString("PlayerNickname", "Guest");
        string hostNick = hostPlayer != null ? hostPlayer.nickname : "(null)";

        Debug.Log($"[LobbyUI] hostNick={hostNick}, myNick={myNick}, " +
                  $"IsHost(From RM)={isLocalHost}, players={room.players.Length}, showStart={showStart}");
    }

    // -----------------------------
    // 게임 시작 버튼 (호스트만)
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

        // 호스트인지 최종 확인
        if (!rm.IsHost)
        {
            ShowError("호스트만 게임을 시작할 수 있습니다.");
            Debug.LogWarning("[LobbyUI] Not host — cannot start game (IsHost == false)");
            return;
        }

        // 여기서는 서버에 요청만 보냄. 실제 카운트다운은 GAME_START_TIMER 이벤트로 시작.
        string json = @"{ ""action"": ""START_GAME"" }";
        WebSocketManager.Instance.Send(json);
        Debug.Log("[LobbyUI] START_GAME sent to server (by host)");

        // 버튼 연타 방지
        if (startButton != null)
            startButton.SetActive(false);
    }

    // -----------------------------
    // 서버 → GAME_START_TIMER(seconds) 수신
    // -----------------------------
    private void HandleGameStartTimer(int seconds)
    {
        Debug.Log("[LobbyUI] HandleGameStartTimer: " + seconds);

        if (countdownRoutine != null)
            StopCoroutine(countdownRoutine);

        countdownCancelled = false;
        countdownRoutine = StartCoroutine(GameStartCountdownRoutine(seconds));
    }

    // TIMER_CANCELLED 수신
    private void HandleTimerCancelled()
    {
        Debug.Log("[LobbyUI] HandleTimerCancelled");

        countdownCancelled = true;

        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }

        if (countdownText != null)
        {
            countdownText.text = string.Empty;
            countdownText.gameObject.SetActive(false);
        }

        // 호스트면 다시 버튼 활성화
        if (startButton != null && RoomManager.Instance != null && RoomManager.Instance.IsHost)
            startButton.SetActive(true);
    }

    // LOAD_GAME_SCENE 수신 → 실제 씬 전환
    private void HandleLoadGameScene(RoomManager.Room room)
    {
        Debug.Log("[LobbyUI] HandleLoadGameScene → Load GameScene");

        // 혹시 남아 있는 카운트다운 코루틴 정리
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }

        if (countdownText != null)
        {
            countdownText.text = "게임 시작!";
        }

        // 약간의 딜레이 후 씬 전환 (무스 느낌 약간 살짝 연출)
        StartCoroutine(LoadGameSceneAfterDelay(0.5f));
    }

    private IEnumerator LoadGameSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene("GameScene");
    }

    // -----------------------------
    // 3,2,1 카운트다운 연출 (무스 느낌)
    // -----------------------------
    private IEnumerator GameStartCountdownRoutine(int seconds)
    {
        if (countdownText == null)
            yield break;

        countdownText.gameObject.SetActive(true);

        // 예: 3 → 2 → 1 → "게임 시작!"
        for (int t = seconds; t > 0; t--)
        {
            if (countdownCancelled) break;

            countdownText.text = t.ToString();

            // 간단한 크기 연출 (무스 느낌 살짝)
            countdownText.fontSize = 100;
            float time = 0f;
            while (time < 1f)
            {
                if (countdownCancelled) break;

                time += Time.deltaTime;
                // 서서히 작아지게 (100 → 80 정도)
                countdownText.fontSize = Mathf.Lerp(100f, 80f, time);
                yield return null;
            }
        }

        if (!countdownCancelled)
        {
            countdownText.text = "게임 시작!";
        }

        // 실제 씬 이동은 LOAD_GAME_SCENE 이벤트에서 처리.
        // 여기서는 텍스트만 보여준다.
    }
}
