using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RoomListUIManager : MonoBehaviour
{
    public GameObject roomItemPrefab;
    public Transform content;

    private void OnEnable()
    {
        // RoomManager 이벤트 구독
        if (RoomManager.Instance != null)
            RoomManager.Instance.OnRoomListUpdated += UpdateRoomList;
    }

    private void OnDisable()
    {
        if (RoomManager.Instance != null)
            RoomManager.Instance.OnRoomListUpdated -= UpdateRoomList;
    }

    void Start()
    {
        // WebSocket 연결되면 자동으로 방 목록 요청
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
            WebSocketManager.Instance.Send("{\"action\":\"getRooms\"}");
        }
        else
        {
            Debug.LogWarning("[RoomListUIManager] Cannot refresh room list. WebSocket not connected.");
        }
    }

    private void UpdateRoomList(List<string> rooms)
    {
        // 기존 항목 제거
        foreach (Transform child in content)
            Destroy(child.gameObject);

        // 새로운 항목 생성
        foreach (string room in rooms)
        {
            var obj = Instantiate(roomItemPrefab, content);
            var txt = obj.GetComponentInChildren<TMP_Text>();
            if (txt != null)
                txt.text = room;
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
