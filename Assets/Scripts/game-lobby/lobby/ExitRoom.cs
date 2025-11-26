using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitRoom : MonoBehaviour
{
    private bool isLeavingRoom = false;

public void OnClickExitRoomButton()
    {
        if (isLeavingRoom) return;
        isLeavingRoom = true;

        Debug.Log("▶ 방 나가기 요청 전송 (서버 응답 기다리지 않음)");
        WebSocketManager.Instance.Send("{\"action\":\"LEAVE_ROOM\"}");
    }


}
