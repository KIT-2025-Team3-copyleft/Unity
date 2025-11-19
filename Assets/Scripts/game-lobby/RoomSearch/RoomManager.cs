using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;

    private string currentRoomId;
    private bool currentRoomPrivate;

    // 현재 방 ID 가져가기
    public string CurrentRoomId => currentRoomId;

    // 현재 방이 private인지 가져가기
    public bool IsCurrentRoomPrivate => currentRoomPrivate;

    public List<string> roomList = new List<string>();

    // FastStart 관련 이벤트
    public event Action OnFastStartNoRoom;
    public event Action<string> OnFastStartFoundRoom;
    public event Action<List<string>> OnRoomListUpdated;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        StartCoroutine(RegisterWebSocketEvent());
    }

    IEnumerator RegisterWebSocketEvent()
    {
        while (WebSocketManager.Instance == null)
            yield return null;

        WebSocketManager.Instance.OnServerMessage += Handle;
        Debug.Log("[RoomManager] WebSocket 이벤트 구독 완료");
    }

    void OnDisable()
    {
        if (WebSocketManager.Instance != null)
            WebSocketManager.Instance.OnServerMessage -= Handle;
    }

    void Handle(string json)
    {
        Debug.Log("[RoomManager] Handle 호출: " + json);

        BaseMsg baseMsg;
        try
        {
            baseMsg = JsonUtility.FromJson<BaseMsg>(json);
        }
        catch
        {
            return; // Json 타입이 아니면 무시
        }

        if (baseMsg == null || string.IsNullOrEmpty(baseMsg.action))
            return;

        switch (baseMsg.action)
        {
            case "roomCreated":
                RoomCreated created = JsonUtility.FromJson<RoomCreated>(json);
                currentRoomId = created.room_id;
                currentRoomPrivate = created.isPrivate;

                PlayerPrefs.SetString("RoomID", currentRoomId);
                PlayerPrefs.SetInt("IsPrivate", currentRoomPrivate ? 1 : 0);
                PlayerPrefs.Save();

                SceneManager.LoadScene("LobbyScene");
                break;

            case "roomList":
                RoomList list = JsonUtility.FromJson<RoomList>(json);
                roomList = new List<string>(list.rooms);
                Debug.Log("[RoomManager] roomList 업데이트 완료");

                OnRoomListUpdated?.Invoke(roomList); // UI 알림
                break;

            case "joinSuccess":
                NotifyRoomJoinResult(true);
                SceneManager.LoadScene("LobbyScene");
                break;

            case "joinFail":
                NotifyRoomJoinResult(false);
                break;

            case "noRoom":
                Debug.Log("[RoomManager] FastStart - noRoom");
                OnFastStartNoRoom?.Invoke();
                break;

            case "fastStartRoom":
                FastStartRoom fs = JsonUtility.FromJson<FastStartRoom>(json);
                Debug.Log("[RoomManager] FastStart - fastStartRoom: " + fs.room_id);

                OnFastStartFoundRoom?.Invoke(fs.room_id);
                JoinRoom(fs.room_id);
                break;
        }
    }

    public void RequestFastStart()
    {
        string json = "{ \"action\": \"fastStart\" }";
        WebSocketManager.Instance.Send(json);
    }

    public void JoinRoom(string roomId)
    {
        JoinRoomReq req = new JoinRoomReq
        {
            action = "joinRoom",
            room_id = roomId
        };

        WebSocketManager.Instance.Send(JsonUtility.ToJson(req));
    }

    private void NotifyRoomJoinResult(bool success)
    {
        RoomJoin ui = FindFirstObjectByType<RoomJoin>();
        if (ui != null)
            ui.OnJoinResult(success);
    }

    // JSON 클래스들
    [Serializable] public class BaseMsg { public string action; }
    [Serializable] public class RoomCreated : BaseMsg { public string room_id; public bool isPrivate; }
    [Serializable] public class RoomList : BaseMsg { public string[] rooms; }
    [Serializable] public class JoinRoomReq { public string action; public string room_id; }
    [Serializable] public class FastStartRoom : BaseMsg { public string room_id; }
}
