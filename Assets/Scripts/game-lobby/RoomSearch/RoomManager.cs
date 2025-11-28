using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;

    public GameObject playerPrefab;

    private Room room;

    public event Action<List<Room>> OnRoomListUpdated;
    // 🔹 현재 접속한 방 정보
    public string CurrentRoomId { get; private set; }
    public string CurrentRoomCode { get; private set; }
    public bool IsHost { get; private set; }

    // 🔹 JOIN 결과 이벤트
    public event Action<bool> OnJoinResult;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        StartCoroutine(RegisterWebSocket());
    }

    // 🔹 WebSocket 준비되면 메세지 등록
    IEnumerator RegisterWebSocket()
    {
        while (WebSocketManager.Instance == null)
            yield return null;

        WebSocketManager.Instance.OnServerMessage -= Handle;
        WebSocketManager.Instance.OnServerMessage += Handle;
    }

    // 🔹 서버 메세지 처리
    void Handle(string json)
    {
        BaseEvent b;
        try { b = JsonUtility.FromJson<BaseEvent>(json); }
        catch { return; }

        if (b == null || string.IsNullOrEmpty(b.@event))
            return;

        switch (b.@event)
        {
            case "JOIN_SUCCESS":
                HandleJoinSuccess(json);
                break;

            case "LOBBY_UPDATE":
                HandleLobbyUpdate(json);
                break;

            case "JOIN_FAILED":
                HandleJoinFailed(json);
                break;

            case "LEAVE_SUCCESS":
                HandleLeave(json);
                break;
        }
    }

    // -----------------------------
    // 🔥 이벤트 처리
    // -----------------------------

    void HandleJoinSuccess(string json)
    {
        var join = JsonUtility.FromJson<JoinSuccessEvent>(json);

        CurrentRoomId = join.data.roomId;
        CurrentRoomCode = join.data.roomCode;

        PlayerPrefs.SetString("RoomID", CurrentRoomId);
        PlayerPrefs.SetString("RoomCode", CurrentRoomCode);
        PlayerPrefs.Save();

        // 🔹 players 배열에서 자기 세션 ID 찾기
        string mySessionId = WebSocketManager.Instance.ClientSessionId;

        foreach (var player in join.data.players)
        {
            Debug.Log($"Player {player.nickname}, sessionId={player.sessionId}, host={player.host}");

            IsHost = player.host;
            break;
        }
        

        Debug.Log($"[JOIN_SUCCESS] roomCode={CurrentRoomCode}, isHost={IsHost}");

        OnJoinResult?.Invoke(true);

        SceneManager.LoadScene("LobbyScene");
    }

    void HandleLobbyUpdate(string json)
    {
        var lobby = JsonUtility.FromJson<LobbyUpdateEvent>(json);

        if (lobby == null || lobby.data == null)
        {
            Debug.LogError("LobbyUpdateEvent parsing failed: data is null");
            return;
        }

        LobbyManager.Instance?.NotifyLobbyUpdated(lobby.data);
    }

    void HandleJoinFailed(string json)
    {
        var fail = JsonUtility.FromJson<JoinFailedEvent>(json);

        UnityMainThreadDispatcher.EnqueueOnMainThread(() =>
        {
            RoomJoin.Instance.ShowMessage(fail.message);
        });

        OnJoinResult?.Invoke(false);
    }

    void HandleLeave(string json)
    {
        var leave = JsonUtility.FromJson<LeaveSuccessEvent>(json);
        Debug.Log("[LEAVE] " + leave.message);
    }

    // -----------------------------
    // 🔥 요청 함수
    // -----------------------------

    public void RequestQuickJoin()
    {
        SendAction("QUICK_JOIN");
    }

    public void RequestCreateRoom()
    {
        SendAction("CREATE_ROOM");
    }

    public void JoinRoom(string code)
    {
        var payload = new Dictionary<string, object>
        {
            { "roomCode", code },
            { "nickname", PlayerPrefs.GetString("PlayerNickname", "Guest") }
        };

        var data = new Dictionary<string, object>
        {
            { "action", "JOIN_BY_CODE" },
            { "payload", payload }
        };

        string json = MiniJSON.Json.Serialize(data);
        WebSocketManager.Instance.Send(json);
    }

    void SendAction(string action)
    {
        string json = "{ \"action\": \"" + action + "\" }";
        WebSocketManager.Instance.Send(json);
    }

    // -----------------------------
    // 🔥 JSON 구조체들
    // -----------------------------

    [Serializable]
    public class BaseEvent
    {
        public string @event;
    }

    [Serializable]
    public class JoinSuccessEvent : BaseEvent
    {
        public string message;
        public JoinSuccessData data;
    }

    [Serializable]
    public class JoinSuccessData
    {
        public string roomTitle;
        public string roomId;
        public string roomCode;
        public string hostSessionId;
        public PlayerData[] players;
    }

    [Serializable]
    public class PlayerData
    {
        public string sessionId;
        public string nickname;
        public string color;
        public bool host;
    }

    [Serializable]
    public class LobbyUpdateEvent : BaseEvent
    {
        public Room data;
    }

    [Serializable]
    public class Room
    {
        public string roomId;
        public string roomCode;
        public string hostSessionId;
        public PlayerData[] players;
        public string status;
    }

    [Serializable]
    public class JoinFailedEvent : BaseEvent
    {
        public string code;
        public string message;
    }

    [Serializable]
    public class LeaveSuccessEvent : BaseEvent
    {
        public string message;
    }
}
