using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;

    private string currentRoomId;
    public string CurrentRoomId => currentRoomId;
    public bool IsHost { get; private set; }

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

    void OnEnable()
    {
        if (WebSocketManager.Instance != null)
            WebSocketManager.Instance.OnServerMessage += Handle;
    }

    void OnDisable()
    {
        if (WebSocketManager.Instance != null)
            WebSocketManager.Instance.OnServerMessage -= Handle;
    }

    // ================================
    // 서버 메시지 처리
    // ================================
    void Handle(string json)
    {
        Debug.Log("[RoomManager] Handle : " + json);
        BaseEvent baseMsg;

        try
        {
            baseMsg = JsonUtility.FromJson<BaseEvent>(json);
        }
        catch
        {
            return; // action/event 존재 안하면 무시
        }

        if (baseMsg == null) return;

        switch (baseMsg.@event)
        {
            case "JOIN_SUCCESS":
                var join = JsonUtility.FromJson<JoinSuccessEvent>(json);
                currentRoomId = join.room.roomId;
                PlayerPrefs.SetString("RoomID", currentRoomId);
                PlayerPrefs.Save();
                OnJoinResult?.Invoke(true);
                SceneManager.LoadScene("LobbyScene");
                break;

            case "LOBBY_UPDATE":
                var update = JsonUtility.FromJson<LobbyUpdateEvent>(json);
                //LobbyUI.Instance?.Refresh(update.room);
                break;

            case "JOIN_FAILED":
                var failed = JsonUtility.FromJson<JoinFailedEvent>(json);
                OnJoinResult?.Invoke(false);
                break;
        }
    }

    // ================================
    // Unity → 서버 요청
    // ================================
    public void RequestQuickJoin()
    {
        string json = "{ \"action\": \"QUICK_JOIN\" }";
        Debug.Log("[SEND] " + json);
        WebSocketManager.Instance.Send(json);
    }

    public void RequestCreateRoom()
    {
        string json = "{ \"action\": \"CREATE_ROOM\" }";
        Debug.Log("[SEND] " + json);
        WebSocketManager.Instance.Send(json);
    }

    public void JoinRoom(string roomCode)
    {
        var payload = new Dictionary<string, object>
        {
            { "roomCode", roomCode },
            { "nickname", PlayerPrefs.GetString("PlayerNickname", "Guest") } // payload 추가 예시
        };

        var data = new Dictionary<string, object>
        {
            { "action", "JOIN_BY_CODE" },
            { "payload", payload }
        };

        string json = MiniJSON.Json.Serialize(data);
        Debug.Log("[SEND] " + json);
        WebSocketManager.Instance.Send(json);
    }

    // ================================
    // JSON 구조체 정의
    // ================================
    [Serializable]
    public class BaseEvent
    {
        public string @event; // event 이름
    }

    [Serializable]
    public class JoinSuccessEvent : BaseEvent
    {
        public Room room;
    }

    [Serializable]
    public class LobbyUpdateEvent : BaseEvent
    {
        public Room room;
    }

    [Serializable]
    public class JoinFailedEvent : BaseEvent
    {
        public string code;
        public string message;
    }

    [Serializable]
    public class Room
    {
        public string roomId;
        public string roomCode;
        public string roomTitle;
        public string status;
        public string hostSessionId;
        public Player[] players;
    }

    [Serializable]
    public class Player
    {
        public string sessionId;
        public string nickname;
        public string color;
    }

    public event Action<List<string>> OnRoomListUpdated;
    public event Action<bool> OnJoinResult;
}