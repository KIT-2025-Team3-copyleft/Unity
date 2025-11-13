using UnityEngine;
using WebSocketSharp;
using System;
using TMPro;

public class RoomCreator : MonoBehaviour
{
    private WebSocket ws;
    [SerializeField] private SceneChanger sceneChanger;
    [SerializeField] private PrivateModeToggle privateModeToggle;
    //[SerializeField] private TextMeshProUGUI messageText;

    void Start()
    {
        ws = new WebSocket("ws://localhost:8081/");

        ws.OnOpen += (sender, e) =>
        {
            Debug.Log("웹소켓 서버에 연결됨.");
        };

        ws.OnMessage += (sender, e) =>
        {
            Debug.Log("서버에서 받은 메시지: " + e.Data);
            //messageText.text = "받은 메시지: " + e.Data;

            try
            {
                CreateRoomResponse res = JsonUtility.FromJson<CreateRoomResponse>(e.Data);
                GameData.currentRoomId = res.roomId;
                if (sceneChanger != null)
                    sceneChanger.ChangeScene();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("메시지 파싱 실패: " + ex.Message);
            }
        };

        ws.Connect();
    }

    public void CreateRoomFromToggle()
    {
        bool isPrivate = privateModeToggle != null && privateModeToggle.GetPrivateState();
        Debug.Log("생성 버튼 클릭, 서버 전송 isPrivate: " + isPrivate);

        CreateRoom(isPrivate);
    }

    private void CreateRoom(bool isPrivate)
    {
        var jsonObject = new CreateRoomRequest { isPrivate = isPrivate };
        string jsonStr = JsonUtility.ToJson(jsonObject);
        ws.Send(jsonStr);
        Debug.Log("서버로 메시지 전송: " + jsonStr);
    }

    private void OnDestroy()
    {
        if (ws != null)
            ws.Close();
    }

    [Serializable]
    public class CreateRoomRequest
    {
        public bool isPrivate;
    }

    [Serializable]
    public class CreateRoomResponse
    {
        public string roomId;
        public bool isPrivate;
    }
}

public static class GameData
{
    public static string currentRoomId;
}
