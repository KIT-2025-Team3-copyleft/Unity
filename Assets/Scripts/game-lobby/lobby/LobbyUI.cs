using UnityEngine;
using TMPro;
using System.Collections;
using System.Linq;
using UnityEngine.SceneManagement;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance;

    [SerializeField] private GameObject startButton;
    [SerializeField] private TMP_Text playerCountText; // 또는 TextMeshProUGUI
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerListItemPrefab;

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
        // 안전하게 RoomManager가 준비되어 있으면 구독, 아니면 나중에 Subscribe 시도
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.OnLobbyUpdated -= UpdateLobbyUI;
            RoomManager.Instance.OnLobbyUpdated += UpdateLobbyUI;
            Debug.Log("[LobbyUI] Subscribed to RoomManager.OnLobbyUpdated (OnEnable)");
        }
        else
        {
            // RoomManager가 아직 없을 수 있으니 코루틴으로 대기 후 구독
            StartCoroutine(WaitAndSubscribe());
        }
    }

    private IEnumerator WaitAndSubscribe()
    {
        while (RoomManager.Instance == null)
            yield return null;

        RoomManager.Instance.OnLobbyUpdated -= UpdateLobbyUI;
        RoomManager.Instance.OnLobbyUpdated += UpdateLobbyUI;
        Debug.Log("[LobbyUI] Subscribed to RoomManager.OnLobbyUpdated (WaitAndSubscribe)");
    }

    private void OnDisable()
    {
        if (RoomManager.Instance != null)
            RoomManager.Instance.OnLobbyUpdated -= UpdateLobbyUI;
    }

    private void OnDestroy()
    {
        if (RoomManager.Instance != null)
            RoomManager.Instance.OnLobbyUpdated -= UpdateLobbyUI;
    }

    public void UpdateHostButton(bool isHost)
    {
        if (startButton != null)
            startButton.SetActive(isHost);
    }

    private void UpdateLobbyUI(RoomManager.Room room)
    {
        // 씬 체크 (사실 OnDisable로 구독 해제하면 이 부분은 안전장치)
        if (SceneManager.GetActiveScene().name != "LobbyScene")
            return;

        if (room == null || room.players == null) return;

        Debug.Log($"[LobbyUI] Updating UI, Players={room.players.Length}");

        // 플레이어 수 UI (playerCountText는 TextMeshProUGUI 또는 TMP_Text)
        if (playerCountText != null)
            playerCountText.text = $"{room.players.Length}/4";

        // 기존 리스트 안전하게 삭제 (for loop)
        if (playerListContainer != null)
        {
            for (int i = playerListContainer.childCount - 1; i >= 0; i--)
            {
                var child = playerListContainer.GetChild(i);
                if (child != null)
                    Destroy(child.gameObject);
            }
        }

        // 플레이어 목록 생성
        string hostSessionId = room.hostSessionId;

        foreach (var p in room.players)
        {
            if (playerListItemPrefab == null || playerListContainer == null)
            {
                Debug.LogError("[LobbyUI] playerListItemPrefab 또는 playerListContainer가 설정되지 않음");
                break;
            }

            var obj = Instantiate(playerListItemPrefab, playerListContainer);

            // UI에서는 TextMeshProUGUI 사용 권장
            var text = obj.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text == null)
            {
                // TMP_Text 폴백
                var textFallback = obj.GetComponentInChildren<TMP_Text>(true);
                if (textFallback != null)
                {
                    // host 판단: p.sessionId == hostSessionId
                    textFallback.text = $"{p.nickname}" + (p.sessionId == hostSessionId ? " (Host)" : "");
                }
                else
                {
                    Debug.LogWarning("[LobbyUI] playerListItemPrefab에 TextMeshProUGUI 또는 TMP_Text 컴포넌트가 없음");
                }
            }
            else
            {
                text.text = $"{p.nickname}" + (p.sessionId == hostSessionId ? " (Host)" : "");
            }
        }

        // 로컬 플레이어가 호스트면 Start 버튼 활성화
        bool isLocalHost = WebSocketManager.Instance.ClientSessionId == hostSessionId;
        UpdateHostButton(isLocalHost);
    }
}
