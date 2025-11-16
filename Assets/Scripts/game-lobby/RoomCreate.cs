using UnityEngine;
using UnityEngine.SceneManagement; // 씬 이동 위해 추가

public class RoomCreator : MonoBehaviour
{
    public PrivateModeToggle privateModeToggle;

    public void OnClickCreateRoom()
    {
        bool isPrivate = privateModeToggle != null && privateModeToggle.GetPrivateState();

        var request = new CreateRoomRequest { action = "createRoom", isPrivate = isPrivate };
        string json = JsonUtility.ToJson(request);

        // 콘솔 출력 (보낸 데이터 확인)
        Debug.Log($"[RoomCreator] Sending CreateRoom Request: {json}");

        // 서버로 전송
        WebSocketManager.Instance.Send(json);

        //씬 전환
        Debug.Log("[RoomCreator] Moving to LobbyScene...");
        SceneManager.LoadScene("LobbyScene");
    }

    [System.Serializable]
    public class CreateRoomRequest
    {
        public string action;
        public bool isPrivate;
    }
}
