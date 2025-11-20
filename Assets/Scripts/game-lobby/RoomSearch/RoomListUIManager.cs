using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RoomListUIManager : MonoBehaviour
{
    public GameObject roomItemPrefab;
    public Transform content;

    void OnEnable()
    {
        // WebSocketManager 인스턴스가 존재할 때만 직접 구독
        if (WebSocketManager.Instance != null)
            WebSocketManager.Instance.OnServerMessage += Handle;
    }

    void OnDisable()
    {
        if (WebSocketManager.Instance != null)
            WebSocketManager.Instance.OnServerMessage -= Handle;
    }

    void Start()
    {
        // WebSocket이 아직 연결 전일 수 있으므로 OnConnected로 안전하게 작업
        if (WebSocketManager.Instance != null)
        {
            WebSocketManager.Instance.OnConnected += OnWebSocketConnected;
            // 만약 이미 연결되어 있다면 바로 요청
            if (WebSocketManager.Instance.IsConnected)
                OnWebSocketConnected();
        }
        else
        {
            Debug.LogWarning("[RoomListUIManager] WebSocketManager.Instance is null in Start()");
        }
    }

    void OnWebSocketConnected()
    {
        // RoomManager 이벤트 연결
        if (RoomManager.Instance != null)
            RoomManager.Instance.OnRoomListUpdated += UpdateRoomList;

        // 서버에 방 목록 요청
        RequestRefresh();
    }

    public void RequestRefresh()
    {
        if (WebSocketManager.Instance != null && WebSocketManager.Instance.IsConnected)
            WebSocketManager.Instance.Send("{\"action\":\"getRooms\"}");
        else
            Debug.LogWarning("[RoomListUIManager] WebSocket not connected - cannot request room list");
    }

    void UpdateRoomList(List<string> rooms)
    {
        // 기존 리스트 제거
        foreach (Transform child in content)
            Destroy(child.gameObject);

        // 새 리스트 생성
        foreach (string room in rooms)
        {
            var obj = Instantiate(roomItemPrefab, content);
            var txt = obj.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.text = room;
        }
    }

    void OnDestroy()
    {
        if (WebSocketManager.Instance != null)
            WebSocketManager.Instance.OnConnected -= OnWebSocketConnected;

        if (RoomManager.Instance != null)
            RoomManager.Instance.OnRoomListUpdated -= UpdateRoomList;
    }

    // 필요하면 메시지 처리용 Handle 함수 유지 (현재는 사용X)
    void Handle(string msg)
    {
        // Optional: Server에서 직접 오는 메시지를 처리하려면 구현
    }
}
