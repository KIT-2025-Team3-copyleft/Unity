using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static RoomManager;

public class RoomManager : MonoBehaviour
{
    public string MySessionId { get; private set; }
    public static RoomManager Instance;

    public string MyNickname { get; private set; }

    // 이벤트들
    public event Action<string> OnErrorMessage;
    public event Action<int> OnGameStartTimer;   // seconds
    public event Action OnTimerCancelled;
    public event Action<Room> OnLoadGameScene;

    public event Action<List<Room>> OnRoomListUpdated;
    public event Action<bool> OnJoinResult;
    public event Action<Room> OnLobbyUpdated;

    // 현재 방/상태
    public Room CurrentRoom { get; private set; }
    public string CurrentRoomId { get; private set; }
    public string CurrentRoomCode { get; private set; }

    public bool IsHost { get; private set; }

    // 내 플레이어 번호 (0~3), 없으면 -1
    public int MyPlayerNumber { get; private set; } = -1;

    private bool isLobbyUpdatedProcessed = false;

    public string HostNickname { get; private set; }


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

    // ===========================
    // 서버 메시지 처리
    // ===========================
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
            case "ROOM_LIST":
                {
                    var data = JsonUtility.FromJson<RoomListEvent>(json);

                    var rooms = data?.data?.rooms;
                    Debug.Log("[RoomManager] ROOM_LIST received: " + (rooms != null ? rooms.Count : 0) + " rooms");

                    HandleRoomListUpdated(rooms);
                }
                break;

            case "ROOM_LIST_UPDATE":
                {
                    var data = JsonUtility.FromJson<RoomListUpdateEvent>(json);
                    Debug.Log("[RoomManager] ROOM_LIST_UPDATE received: " + (data.rooms != null ? data.rooms.Count : 0) + " rooms");
                    HandleRoomListUpdated(data.rooms);
                }
                break;
            case "GAME_START_TIMER": HandleGameStartTimer(json); break;
            case "TIMER_CANCELLED": HandleTimerCancelled(json); break;
            case "LOAD_GAME_SCENE": HandleLoadGameScene(json); break;
            case "ERROR_MESSAGE": HandleErrorMessage(json); break;
        }
    }

    void HandleErrorMessage(string json)
    {
        var err = JsonUtility.FromJson<ErrorMessageEvent>(json);
        Debug.Log("[RoomManager] ERROR_MESSAGE: " + err.message);
        OnErrorMessage?.Invoke(err.message);
    }

    void HandleGameStartTimer(string json)
    {
        Debug.Log("[RoomManager] GAME_START_TIMER received");
        var timer = JsonUtility.FromJson<GameStartTimerEvent>(json);
        OnGameStartTimer?.Invoke(timer.seconds);
    }

    void HandleTimerCancelled(string json)
    {
        Debug.Log("[RoomManager] TIMER_CANCELLED received");
        OnTimerCancelled?.Invoke();
    }

    void HandleLoadGameScene(string json)
    {
        Debug.Log("[RoomManager] LOAD_GAME_SCENE received");
        var data = JsonUtility.FromJson<LoadGameSceneEvent>(json);
        CurrentRoom = data.room;
        OnLoadGameScene?.Invoke(CurrentRoom);
    }

    // ===========================
    // JOIN_SUCCESS (한 번만)
    // =================================
    void HandleJoinSuccess(string json)
    {
        Debug.Log("[RoomManager] JOIN_SUCCESS received");

        // 🔥 세션ID 기다리지 말고 바로 처리
        ProcessJoinSuccess(json);
    }

    void ProcessJoinSuccess(string json)
    {
        var join = JsonUtility.FromJson<JoinSuccessEvent>(json);

        if (join == null || join.data == null)
        {
            Debug.LogError("[RoomManager] Failed to parse JOIN_SUCCESS data or data is null");
            return;
        }

        // 방 기본 정보
        CurrentRoom = join.data;
        CurrentRoomId = join.data.roomId;
        CurrentRoomCode = join.data.roomCode;

        // 내 닉네임
        string myNick = PlayerPrefs.GetString("PlayerNickname", "Guest");
        MySessionId = null;

        if (CurrentRoom.players != null)
        {
            foreach (var p in CurrentRoom.players)
            {
                if (p.nickname == myNick)
                {
                    {
                        if (GameManager.Instance != null && !string.IsNullOrEmpty(p.sessionId))
                        {
                            // 🌟 찾은 플레이어의 sessionId를 GameManager에 할당
                            GameManager.Instance.SetMySessionId(p.sessionId);
                        }

                    }
                }
            }

            // 호스트 여부
            IsHost = false;
            if (CurrentRoom.players != null)
            {
                foreach (var p in CurrentRoom.players)
                {
                    if (p.nickname == myNick && p.host)
                    {
                        IsHost = true;
                        break;
                    }
                }
            }

            Debug.Log($"[RoomManager] ProcessJoinSuccess: roomCode={CurrentRoomCode}, myNick={myNick}, IsHost={IsHost}");

            // LOBBY_UPDATE 처음 한 번 처리되게 플래그 초기화
            isLobbyUpdatedProcessed = false;

            OnJoinResult?.Invoke(true);
        }
    }

    // ===========================
    // LOBBY_UPDATE
    // ===========================
    void HandleLobbyUpdate(string json)
    {
        Debug.Log("[RoomManager] LOBBY_UPDATE received");
        var lobby = JsonUtility.FromJson<LobbyUpdateEvent>(json);

        CurrentRoom = lobby.data;
        var players = CurrentRoom.players;

        if (players == null || players.Length == 0)
            return;

        // 1) 호스트 찾기
        HostNickname = null;

        // 1-1. hostSessionId 기준으로 찾기
        if (!string.IsNullOrEmpty(CurrentRoom.hostSessionId))
        {
            foreach (var p in players)
            {
                if (p.sessionId == CurrentRoom.hostSessionId)
                {
                    HostNickname = p.nickname;
                    break;
                }
            }
        }

        // 1-2. 그래도 못 찾으면 host 플래그로 찾기 (백업)
        if (HostNickname == null)
        {
            foreach (var p in players)
            {
                if (p.host)
                {
                    HostNickname = p.nickname;
                    break;
                }
            }
        }

        // 2) 플레이어 번호 정리
        AssignPlayerNumbers();
        SetMyPlayerNumber();

        // 3) 나는 호스트인가? → 닉네임으로만 판단
        string myNick = PlayerPrefs.GetString("PlayerNickname", "Guest");
        IsHost = (HostNickname == myNick);

        Debug.Log($"[RoomManager] LOBBY_UPDATE processed: RoomId={CurrentRoom.roomId}, " +
                  $"Players={players.Length}, HostNick={HostNickname}, MyNick={myNick}, IsHost={IsHost}");

        bool shouldLoadLobbyScene = false;

        if (CurrentRoom.status == "WAITING" || CurrentRoom.status == "STARTING")
        {
            if (SceneManager.GetActiveScene().name == "GamePlay" &&
            GameManager.Instance != null &&
            GameManager.Instance.IsShowingGameOverUI)
            {
                Debug.LogWarning("[RoomManager] WAITING 상태 수신. 그러나 유저 선택 대기 중이므로 씬 전환을 무시합니다.");
                shouldLoadLobbyScene = false;
            }
            else
            {
                // **B. 그 외의 경우 (정상적인 씬 전환)**
                if (SceneManager.GetActiveScene().name != "LobbyScene")
                {
                    shouldLoadLobbyScene = true;
                    Debug.Log("[RoomManager] Status is WAITING/STARTING. Loading LobbyScene.");
                    SceneManager.LoadScene("LobbyScene");
                }
            }
        }
        
        OnLobbyUpdated?.Invoke(CurrentRoom);
        
    }



    void AssignPlayerNumbers()
    {
        if (CurrentRoom.players != null)
        {
            for (int i = 0; i < CurrentRoom.players.Length; i++)
            {
                CurrentRoom.players[i].playerNumber = i;
                Debug.Log($"Player {CurrentRoom.players[i].nickname} assigned playerNumber: {CurrentRoom.players[i].playerNumber}");
            }
        }
    }

    void SetMyPlayerNumber()
    {
        if (CurrentRoom == null || CurrentRoom.players == null)
        {
            MyPlayerNumber = -1;
            Debug.LogWarning("[RoomManager] SetMyPlayerNumber: CurrentRoom or players is null");
            return;
        }

        string myNick = PlayerPrefs.GetString("PlayerNickname", "Guest");

        foreach (var player in CurrentRoom.players)
        {
            if (player.nickname == myNick)
            {
                MyPlayerNumber = player.playerNumber;
                Debug.Log($"[RoomManager] My player number is {MyPlayerNumber} (nickname={myNick})");
                return;
            }
        }

        MyPlayerNumber = -1;
        Debug.LogWarning($"[RoomManager] My playerNumber not found (nickname={myNick})");
    }

    // ===========================
    // 기타 이벤트
    // ===========================
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
        Debug.Log("[RoomManager] HandleRoomListUpdated: " + (rooms != null ? rooms.Count : 0) + " rooms");
        OnRoomListUpdated?.Invoke(rooms);
    }

    public void ClearListeners()
    {
        OnLobbyUpdated = null;
    }

    // ===========================
    // 요청 API
    // ===========================
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

        // Note: MiniJSON.Json.Serialize is assumed to exist in the actual project.
        string json = $"{{ \"action\": \"JOIN_BY_CODE\", \"payload\": {{ \"roomCode\": \"{code}\", \"nickname\": \"{PlayerPrefs.GetString("PlayerNickname", "Guest")}\" }} }}";

        WebSocketManager.Instance.Send(json);
    }

    void SendAction(string action)
    {
        Debug.Log("[RoomManager] SendAction: " + action);
        string json = "{ \"action\": \"" + action + "\" }";
        WebSocketManager.Instance.Send(json);
    }


    public void RequestStartGame()
    {
        Debug.Log("[RoomManager] RequestStartGame");
        SendAction("START_GAME");
    }

   

    void UpdateLobbyUI(Room room)
    {
        Debug.Log("[RoomManager] UpdateLobbyUI called (debug only)");
    }

    // ===========================
    // JSON 구조체
    // ===========================
    [Serializable]
    public class ErrorMessageEvent : BaseEvent
    {
        public string code;
        public string message;
    }

    [Serializable]
    public class GameStartTimerEvent : BaseEvent
    {
        public int seconds;
    }

    [Serializable]
    public class TimerCancelledEvent : BaseEvent
    {
    }

    [Serializable]
    public class LoadGameSceneEvent : BaseEvent
    {
        public Room room;
    }

    [Serializable] public class RoomListUpdateEvent { public List<Room> rooms; }
    [Serializable] public class BaseEvent { public string @event; }
    [Serializable] public class LobbyUpdateEvent : BaseEvent { public Room data; }
    [Serializable] public class JoinSuccessEvent : BaseEvent { public JoinSuccessData data; }
    [Serializable] public class JoinSuccessData : Room { }
    [Serializable] public class LeaveSuccessEvent : BaseEvent { public string message; }
    [Serializable] public class JoinFailedEvent : BaseEvent { public string code; public string message; }

    // ===========================
    // Room 구조
    // ===========================
    [Serializable]
    public class Room
    {
        public string roomId;
        public string roomCode;
        public string hostSessionId;
        public PlayerData[] players;
        public string status;

        public string roomTitle;
        public int currentCount;
        public int maxCount;
        public bool playing;
    }

    [Serializable]
    public class PlayerData
    {
        public string sessionId;
        public string nickname;
        public bool host;
        public string color;
        public int playerNumber = -1;
    }
}

[Serializable]
public class RoomListEvent : BaseEvent
{
    public RoomListData data;
}

[Serializable]
public class RoomListData
{
    public List<Room> rooms;
}