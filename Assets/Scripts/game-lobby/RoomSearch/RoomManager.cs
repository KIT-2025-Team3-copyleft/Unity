using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;
    public event Action<List<Room>> OnRoomListUpdated;
    public Room CurrentRoom { get; private set; }

    public string CurrentRoomId { get; private set; }
    public string CurrentRoomCode { get; private set; }

    public bool IsHost { get; private set; }

    public event Action<bool> OnJoinResult;      // JOIN 성공/실패
    public event Action<Room> OnLobbyUpdated;    // LOBBY_UPDATE 도착 시

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(RegisterWebSocket());
    }

    IEnumerator RegisterWebSocket()
    {
        while (WebSocketManager.Instance == null)
            yield return null;

        WebSocketManager.Instance.OnServerMessage -= Handle;
        WebSocketManager.Instance.OnServerMessage += Handle;
        Debug.Log("[RoomManager] WebSocket registered");
    }

    // ===============================================================
    // 🔥 서버 메시지 처리
    // ===============================================================
    void Handle(string json)
    {
        Debug.Log("[RoomManager] Received JSON: " + json);
        BaseEvent b;
        try
        {
            b = JsonUtility.FromJson<BaseEvent>(json);
        }
        catch
        {
            Debug.LogWarning("[RoomManager] Failed to parse JSON");
            return;
        }

        if (b == null || string.IsNullOrEmpty(b.@event))
        {
            Debug.LogWarning("[RoomManager] Invalid event in JSON");
            return;
        }

        Debug.Log("[RoomManager] Handling event: " + b.@event);

        switch (b.@event)
        {
            case "JOIN_SUCCESS": HandleJoinSuccess(json); break;
            case "LOBBY_UPDATE": HandleLobbyUpdate(json); break;
            case "JOIN_FAILED": HandleJoinFailed(json); break;
            case "LEAVE_SUCCESS": HandleLeave(json); break;
            case "ROOM_LIST_UPDATE":
                {
                    var data = JsonUtility.FromJson<RoomListUpdateEvent>(json);
                    Debug.Log("[RoomManager] ROOM_LIST_UPDATE received: " + data.rooms.Count + " rooms");
                    HandleRoomListUpdated(data.rooms);
                }
                break;
        }
    }

    // ===============================================================
    // 🔥 JOIN_SUCCESS
    // ===============================================================
    void HandleJoinSuccess(string json)
    {
        Debug.Log("[RoomManager] JOIN_SUCCESS received");
        if (string.IsNullOrEmpty(WebSocketManager.Instance.ClientSessionId))
        {
            Debug.Log("[RoomManager] Waiting for ClientSessionId...");
            StartCoroutine(WaitForSession(json));
            return;
        }
        ProcessJoinSuccess(json);
    }

    IEnumerator WaitForSession(string json)
    {
        while (string.IsNullOrEmpty(WebSocketManager.Instance.ClientSessionId))
            yield return null;

        Debug.Log("[RoomManager] ClientSessionId obtained: " + WebSocketManager.Instance.ClientSessionId);
        ProcessJoinSuccess(json);
    }

    void ProcessJoinSuccess(string json)
    {
        var join = JsonUtility.FromJson<JoinSuccessEvent>(json);

        CurrentRoomId = join.data.roomId;
        CurrentRoomCode = join.data.roomCode;
        CurrentRoom = join.data;

        string mySession = WebSocketManager.Instance.ClientSessionId;
        IsHost = (CurrentRoom.hostSessionId == mySession);

        // 1) 씬 로드 (동기 로드; 비동기 원하면 LoadSceneAsync 사용)
        Debug.Log("[RoomManager] Loading LobbyScene due to JOIN_SUCCESS");
        ClearListeners();
        SceneManager.LoadScene("LobbyScene");

        // 2) sceneLoaded 콜백을 통해 씬 로드 완료 후 안전하게 이벤트 호출
        SceneManager.sceneLoaded += OnSceneLoaded_InvokeLobbyUpdated;

        OnJoinResult?.Invoke(true);
    }

    // sceneLoaded 이벤트에서 한 번만 호출하고 자동으로 언레지스터
    private void OnSceneLoaded_InvokeLobbyUpdated(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("[RoomManager] Scene loaded callback: " + scene.name);

        // 이벤트 지연 실행
        StartCoroutine(DelayedLobbyUpdated());
    }

    public void InvokeLobbyUpdated(Room room)
    {
        OnLobbyUpdated?.Invoke(room);
    }

    private IEnumerator DelayedLobbyUpdated()
    {
        yield return null; // 1프레임 대기
        Debug.Log("[RoomManager] Invoking OnLobbyUpdated (delayed 1 frame)");
        OnLobbyUpdated?.Invoke(CurrentRoom);
    }

    // ===============================================================
    // 🔥 LOBBY_UPDATE
    // ===============================================================
    void HandleLobbyUpdate(string json)
    {
        Debug.Log("[RoomManager] LOBBY_UPDATE received");
        var lobby = JsonUtility.FromJson<LobbyUpdateEvent>(json);

        CurrentRoom = lobby.data;

        string mySession = WebSocketManager.Instance.ClientSessionId;

        // 로컬 클라이언트 기준 호스트 여부만 판단
        IsHost = (CurrentRoom.hostSessionId == mySession);

        Debug.Log($"[RoomManager] LOBBY_UPDATE processed: RoomId={CurrentRoom.roomId}, Players={(CurrentRoom.players != null ? CurrentRoom.players.Length : 0)}, IsHost={IsHost}");

        // 새로 들어온 플레이어들의 호스트 여부를 함께 확인하고 싶다면
        foreach (var player in CurrentRoom.players)
        {
            bool playerIsHost = (player.sessionId == CurrentRoom.hostSessionId);
            Debug.Log($"Player {player.nickname}, sessionId={player.sessionId}, isHost={playerIsHost}");
            // 필요하면 Dictionary 등으로 저장해서 다른 로직에서 활용 가능
        }

        OnLobbyUpdated?.Invoke(CurrentRoom);
    }

    // ===============================================================
    // 🔥 JOIN_FAILED
    // ===============================================================
    void HandleJoinFailed(string json)
    {
        var f = JsonUtility.FromJson<JoinFailedEvent>(json);
        Debug.LogWarning("[RoomManager] JOIN_FAILED: " + f.message);
        OnJoinResult?.Invoke(false);
    }

    void HandleLeave(string json)
    {
        var l = JsonUtility.FromJson<LeaveSuccessEvent>(json);
        Debug.Log("[RoomManager] LEAVE_SUCCESS: " + l.message);
    }

    public void HandleRoomListUpdated(List<Room> rooms)
    {
        Debug.Log("[RoomManager] HandleRoomListUpdated: " + rooms.Count + " rooms");
        OnRoomListUpdated?.Invoke(rooms);
    }

    public void ClearListeners()
    {
        OnLobbyUpdated = null;
    }


    // ===============================================================
    // 🔥 요청 API
    // ===============================================================
    public void RequestQuickJoin()
    {
        Debug.Log("[RoomManager] RequestQuickJoin called");
        SendAction("QUICK_JOIN");
    }

    public void RequestCreateRoom()
    {
        Debug.Log("[RoomManager] RequestCreateRoom called");
        SendAction("CREATE_ROOM");
    }

    public void JoinRoom(string code)
    {
        Debug.Log("[RoomManager] JoinRoom called with code: " + code);
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
        Debug.Log("[RoomManager] SendAction: " + action);
        string json = "{ \"action\": \"" + action + "\" }";
        WebSocketManager.Instance.Send(json);
    }

    // ===============================================================
    // 🔥 JSON 구조체
    // ===============================================================
    [Serializable] public class RoomListUpdateEvent { public List<RoomManager.Room> rooms; }
    [Serializable] public class BaseEvent { public string @event; }
    [Serializable] public class LobbyUpdateEvent : BaseEvent { public Room data; }
    [Serializable] public class JoinSuccessEvent : BaseEvent { public JoinSuccessData data; }
    [Serializable] public class JoinSuccessData : Room { }
    [Serializable] public class LeaveSuccessEvent : BaseEvent { public string message; }
    [Serializable] public class JoinFailedEvent : BaseEvent { public string code; public string message; }

    // ===============================================================
    // 🔥 Room 구조
    // ===============================================================
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
    public class PlayerData
    {
        public string sessionId;
        public string nickname;
        public bool host;
        public string color;
    }

}
