using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RoomListUIManager : MonoBehaviour
{
    public GameObject roomItemPrefab;
    public Transform content;

    void Start()
    {
        // 웹소켓 연결 완료 후 콜백 등록
        WebSocketManager.Instance.OnConnected += OnWebSocketConnected;
    }

    void OnWebSocketConnected()
    {
        // 방 목록 업데이트 이벤트 등록
        RoomManager.Instance.OnRoomListUpdated += UpdateRoomList;

        // 서버에 방 목록 요청
        RequestRefresh();
    }

    public void RequestRefresh()
    {
        WebSocketManager.Instance.Send("{\"action\":\"getRooms\"}");
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
            obj.GetComponentInChildren<TMP_Text>().text = room;
        }
    }

    void OnDestroy()
    {
        // 이벤트 해제
        if (WebSocketManager.Instance != null)
            WebSocketManager.Instance.OnConnected -= OnWebSocketConnected;

        if (RoomManager.Instance != null)
            RoomManager.Instance.OnRoomListUpdated -= UpdateRoomList;
    }
}
