using UnityEngine;
using TMPro;

public class GetRoomId : MonoBehaviour
{
    public TMP_Text roomIdText;   // UI에 표시할 Text

    void Start()
    {
        // RoomManager에서 roomId 가져오기
        string roomCode = RoomManager.Instance.CurrentRoomCode;

        if (string.IsNullOrEmpty(roomCode))
        {
            roomIdText.text = "방 ID: 없음";
            Debug.LogWarning("RoomID가 비어 있음. 아직 서버에서 못 받은 상태?");
        }
        else
        {
            roomIdText.text = roomCode;
            Debug.Log("로비에 진입 — 현재 방 ID: " + roomCode);
        }
    }
}
