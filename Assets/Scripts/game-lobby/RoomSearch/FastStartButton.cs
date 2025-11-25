using UnityEngine;

public class FastStartButton : MonoBehaviour
{
    public GameObject noRoomPopup;

    void Start()
    {
        noRoomPopup.SetActive(false);

        // RoomManager 이벤트 등록
        RoomManager.Instance.OnFastStartNoRoom += ShowNoRoomPopup;
        RoomManager.Instance.OnFastStartFoundRoom += OnFastStartFoundRoom;
    }

    public void OnClickFastStart()
    {
        // 이제 WebSocketManager를 직접 호출하지 않는다
        RoomManager.Instance.RequestFastStart();
    }

    private void OnFastStartFoundRoom(string roomId)
    {
        Debug.Log("빠른 시작 방 ID: " + roomId);
        // 그냥 JoinRoom은 RoomManager가 알아서 처리함
    }

    void OnDestroy()
    {
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.OnFastStartNoRoom -= ShowNoRoomPopup;
            RoomManager.Instance.OnFastStartFoundRoom -= OnFastStartFoundRoom;
        }
    }
    private void ShowNoRoomPopup()
    {
        noRoomPopup.SetActive(true);
    }

    public void HideNoRoomPopup()
    {
        noRoomPopup.SetActive(false);
    }
}
