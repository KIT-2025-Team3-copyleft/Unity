using UnityEngine;
using TMPro;
using System.Collections;
using System.Linq;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private GameObject startButton;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerListItemPrefab;

    private void Start()
    {
        StartCoroutine(SubscribeRoomManager());
    }

    private IEnumerator SubscribeRoomManager()
    {
        while (RoomManager.Instance == null)
            yield return null;

        RoomManager.Instance.OnLobbyUpdated += UpdateLobbyUI;
        Debug.Log("[LobbyUI] Subscribed to RoomManager.OnLobbyUpdated");

        if (RoomManager.Instance.CurrentRoom != null)
            UpdateLobbyUI(RoomManager.Instance.CurrentRoom);
    }

    private void OnDestroy()
    {
        if (RoomManager.Instance != null)
            RoomManager.Instance.OnLobbyUpdated -= UpdateLobbyUI;
    }

    private void UpdateHostButton(bool isHost)
    {
        if (startButton != null)
            startButton.SetActive(isHost);
    }

    private void UpdateLobbyUI(RoomManager.Room room)
    {
        if (room == null || room.players == null) return;

        Debug.Log($"[LobbyUI] Updating UI, Players={room.players.Length}");

        // 플레이어 수 UI
        playerCountText.text = $"{room.players.Length}/4";

        // 기존 리스트 삭제
        foreach (Transform child in playerListContainer)
            Destroy(child.gameObject);

        // 플레이어 목록 생성
        foreach (var p in room.players)
        {
            var obj = Instantiate(playerListItemPrefab, playerListContainer);
            var text = obj.GetComponent<TMP_Text>();
            if (text != null)
                text.text = $"{p.nickname}" + (p.host ? " (Host)" : "");
        }

        // 호스트 버튼 갱신 (로컬 세션 기준)
        bool isLocalHost = room.players.Any(p => p.sessionId == WebSocketManager.Instance.ClientSessionId && p.host);
        UpdateHostButton(isLocalHost);
    }
}
