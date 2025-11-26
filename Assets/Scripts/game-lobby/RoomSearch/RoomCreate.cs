using UnityEngine;
// using UnityEngine.SceneManagement; // 이제 RoomManager가 씬 전환을 담당하므로 여기선 필요 없음

public class RoomCreator : MonoBehaviour
{
    public PrivateModeToggle privateModeToggle;

    void OnEnable()
    {
        if (WebSocketManager.Instance != null)
            WebSocketManager.Instance.OnServerMessage += OnMessage;
    }

    void OnDisable()
    {
        if (WebSocketManager.Instance != null)
            WebSocketManager.Instance.OnServerMessage -= OnMessage;
    }

    public void OnClickCreateRoom()
    {
        bool isPrivate = privateModeToggle != null && privateModeToggle.GetPrivateState();

        var request = new CreateRoomRequest { action = "CREATE_ROOM", isPrivate = isPrivate };
        string json = JsonUtility.ToJson(request);

        Debug.Log($"[RoomCreator] Sending CreateRoom Request: {json}");

        WebSocketManager.Instance.Send(json);

        // 이전: SceneManager.LoadScene("LobbyScene");
        // 변경: 씬 전환은 서버의 roomCreated 응답을 RoomManager가 받으면 SceneManager.LoadScene("LobbyScene") 하도록 함.
        Debug.Log("[RoomCreator] createRoom 요청 발송 완료 - 서버 응답 대기");
    }

    // RoomCreator 자체에서 특별히 메시지를 처리할 필요 없으면 빈 핸들러 유지하거나 제거 가능
    void OnMessage(string msg)
    {
        // 현재 RoomManager에서 모든 handle을 담당하므로 여기서는 처리하지 않음.
    }

    [System.Serializable]
    public class CreateRoomRequest
    {
        public string action;
        public bool isPrivate;
    }
}
