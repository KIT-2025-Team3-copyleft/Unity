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
        Debug.Log("[RoomListUIManager] UpdateRoomList called");

        foreach (Transform child in content)
            Destroy(child.gameObject);

        if (rooms == null)
        {
            Debug.Log("[RoomListUIManager] rooms is null");
            return;
        }

        Debug.Log("[RoomListUIManager] rooms count = " + rooms.Count);

        foreach (var room in rooms)
        {
            var obj = Instantiate(roomItemPrefab, content);
            var txt = obj.GetComponentInChildren<TMP_Text>();

            if (txt != null)
            {
                int playerCount = room.players != null ? room.players.Length : room.currentCount;

                // 서버 JSON에는 roomCode가 없고 roomTitle만 있음
                // {"roomTitle":"123님의 방","currentCount":1,"maxCount":4,"playing":false}
                var title = !string.IsNullOrEmpty(room.roomTitle) ? room.roomTitle : room.roomCode;

                txt.text = $"{room.roomTitle} - {room.currentCount}/{room.maxCount}명 참여 중";
                ;
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
