using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RoomListUIManager : MonoBehaviour
{
    public GameObject roomItemPrefab;
    public Transform content;
    private bool IsDestroyed(UnityEngine.Object obj)
    {
        return obj == null || obj.Equals(null);
    }
    private void OnEnable()
    {
        if (!IsDestroyed(RoomManager.Instance))
            RoomManager.Instance.OnRoomListUpdated += UpdateRoomList;
    }

    private void OnDisable()
    {
        if (!IsDestroyed(RoomManager.Instance))
            RoomManager.Instance.OnRoomListUpdated -= UpdateRoomList;
    }
    void Start()
    {
        if (WebSocketManager.Instance != null)
        {
            WebSocketManager.Instance.OnConnected += OnWebSocketConnected;

            if (WebSocketManager.Instance.IsConnected)
                OnWebSocketConnected();
        }
    }

    private void OnWebSocketConnected()
    {
        Debug.Log("[RoomListUIManager] WebSocket connected → 방 목록 요청");
        RequestRefresh();
    }

    public void RequestRefresh()
    {
        if (WebSocketManager.Instance != null && WebSocketManager.Instance.IsConnected)
        {
            WebSocketManager.Instance.Send("{\"action\":\"GET_ROOM_LIST\"}");
        }
        else
        {
            Debug.LogWarning("[RoomListUIManager] Cannot refresh room list. WebSocket not connected.");
        }
    }

    private void UpdateRoomList(List<RoomManager.Room> rooms)
    {
        // 기존 UI 삭제
        foreach (Transform child in content)
            Destroy(child.gameObject);

        if (rooms == null)
        {
            Debug.Log("[RoomListUIManager] rooms is null");
            return;
        }

        // UI 생성
        foreach (var room in rooms)
        {
            var obj = Instantiate(roomItemPrefab, content);
            var txt = obj.GetComponentInChildren<TMP_Text>();

            if (txt != null)
            {
                int playerCount = room.players != null ? room.players.Length : 0;

                txt.text = $"{room.roomCode} - {playerCount}명 참여 중";
            }
        }
    }

    private void OnDestroy()
    {
        if (WebSocketManager.Instance != null)
            WebSocketManager.Instance.OnConnected -= OnWebSocketConnected;

        if (RoomManager.Instance != null)
            RoomManager.Instance.OnRoomListUpdated -= UpdateRoomList;
    }
}
