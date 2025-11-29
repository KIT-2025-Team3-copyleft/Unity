using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static RoomManager;

public class RoomManager : MonoBehaviour
{
    public event Action<string> OnErrorMessage;
    public event Action<int> OnGameStartTimer; // seconds
    public event Action OnTimerCancelled;
    public event Action<Room> OnLoadGameScene;

    public static RoomManager Instance;
    public event Action<List<Room>> OnRoomListUpdated;
    public Room CurrentRoom { get; private set; }

    public string CurrentRoomId { get; private set; }
    public string CurrentRoomCode { get; private set; }

    public bool IsHost { get; private set; }

    public event Action<bool> OnJoinResult;      // JOIN 성공/실패
    public event Action<Room> OnLobbyUpdated;    // LOBBY_UPDATE 도착 시

    // ← 여기 추가: 내 플레이어 번호 (스폰/카메라 등에서 사용)
    public int MyPlayerNumber { get; private set; } = -1;

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
                    Debug.Log("[RoomManager] ROOM_LIST_UPDATE received: " + (data.rooms != null ? data.rooms.Count : 0) + " rooms");
                    HandleRoomListUpdated(data.rooms);
                }
                break;
            case "GAME_START_TIMER": HandleGameStartTimer(json); break;
            case "TIMER_CANCELLED": HandleTimerCancelled(json); break;
            case "LOAD_GAME_SCENE": HandleLoadGameScene(json); break;
            case "ERROR_MESSAGE":
                HandleErrorMessage(json);
                break;
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

    // ===============================================================
    // 🔥 JOIN_SUCCESS (1명만 받는 이벤트)
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

        Debug.Log("[RoomManager] ClientSessionId obtained");
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

    // 방 정보는 방 ID, 방 코드만 저장하고 플레이어 목록은 LOBBY_UPDATE에서 받음
    CurrentRoomId = join.data.roomId;
    CurrentRoomCode = join.data.roomCode;

    string mySession = WebSocketManager.Instance.ClientSessionId;
    IsHost = (join.data.hostSessionId == mySession);

    Debug.Log("[RoomManager] Loading LobbyScene (JOIN_SUCCESS)");

    // 플레이어 목록을 JOIN_SUCCESS에서 받아와서 UI 초기화
    if (join.data.players != null && join.data.players.Length > 0)
    {
        // 로비 UI 초기화
        if (LobbyUI.Instance != null)
        {
            LobbyUI.Instance.UpdateLobbyUI(join.data); // 플레이어 목록을 UI에 전달
        }
        else
        {
            Debug.LogWarning("[RoomManager] LobbyUI instance is null.");
        }
    }
    else
    {
        Debug.LogWarning("[RoomManager] No players found in the join data.");
    }

    // Clear listeners to avoid duplicated subscriptions
    ClearListeners();
    
    // 씬 로딩 전에 UI가 정상적으로 초기화되도록 순서를 조정
    SceneManager.LoadScene("LobbyScene");

    // 구독을 즉시 추가하여 LOBBY_UPDATE 이벤트 처리 가능하도록 설정
    SceneManager.sceneLoaded += OnSceneLoaded_InvokeLobbyUpdated;

    OnJoinResult?.Invoke(true);
}



    private void OnSceneLoaded_InvokeLobbyUpdated(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("[RoomManager] Scene loaded: " + scene.name);

        // 이제 LOBBY_UPDATE를 기다리지 않고, 방 정보 및 플레이어 목록을 업데이트
        TrySubscribeToLobbyUpdate();  // LOBBY_UPDATE 구독을 여기에 추가
    }

    private void TrySubscribeToLobbyUpdate()
    {
        // RoomManager의 OnLobbyUpdated 이벤트를 구독
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.OnLobbyUpdated -= UpdateLobbyUI;  // 중복 구독 방지
            RoomManager.Instance.OnLobbyUpdated += UpdateLobbyUI;  // 구독 시작
            Debug.Log("[RoomManager] Subscribed to OnLobbyUpdated");

            // 여기서 LOBBY_UPDATE 이벤트가 처리될 때 UpdateLobbyUI 호출
        }
        else
        {
            // RoomManager가 아직 준비되지 않은 경우 대기 후 구독 시도
            StartCoroutine(WaitAndSubscribe());
        }
    }

    private IEnumerator WaitAndSubscribe()
    {
        while (RoomManager.Instance == null)
            yield return null;

        RoomManager.Instance.OnLobbyUpdated -= UpdateLobbyUI;
        RoomManager.Instance.OnLobbyUpdated += UpdateLobbyUI;

        Debug.Log("[RoomManager] Subscribed to OnLobbyUpdated after waiting");
    }


    // ===============================================================
    // 🔥 LOBBY_UPDATE
    // ===============================================================
    private bool isLobbyUpdatedProcessed = false;

    void HandleLobbyUpdate(string json)
    {
        if (isLobbyUpdatedProcessed) return;
        isLobbyUpdatedProcessed = true;

        Debug.Log("[RoomManager] LOBBY_UPDATE received");
        var lobby = JsonUtility.FromJson<LobbyUpdateEvent>(json);

        // 방 정보 최신화
        CurrentRoom = lobby.data;

        string mySession = WebSocketManager.Instance.ClientSessionId;

        // 내 호스트 여부 갱신
        IsHost = (CurrentRoom.hostSessionId == mySession);
        Debug.Log($"[RoomManager] LOBBY_UPDATE processed: RoomId={CurrentRoom.roomId}, Players={(CurrentRoom.players != null ? CurrentRoom.players.Length : 0)}, IsHost={IsHost}");

        // 디버깅용 (각 플레이어 정보 출력)
        if (CurrentRoom.players != null)
        {
            foreach (var player in CurrentRoom.players)
            {
                bool playerIsHost = (player.sessionId == CurrentRoom.hostSessionId);
                Debug.Log($"Player {player.nickname}, sessionId={player.sessionId}, isHost={playerIsHost}, playerNumber={player.playerNumber}");
            }
        }

        // 내 PlayerNumber 찾기 (스폰/카메라용)
        SetMyPlayerNumber();

        // 플레이어 번호 할당
        AssignPlayerNumbers();

        IsHost = (CurrentRoom.hostSessionId == mySession);

        // UI 갱신 이벤트 (LobbyUI가 동작)
        OnLobbyUpdated?.Invoke(CurrentRoom);

        // 스폰 실행 (씬 안에 존재할 때만)
        PlayerSpawnManager.Instance?.SpawnPlayers(CurrentRoom);

      
        CurrentRoom = lobby.data;

       
         // 호스트 여부 갱신

        // UI 갱신 (게임 시작 버튼 상태 업데이트)
    }

    void AssignPlayerNumbers()
    {
        if (CurrentRoom.players != null)
        {
            for (int i = 0; i < CurrentRoom.players.Length; i++)
            {
                CurrentRoom.players[i].playerNumber = i;  // 순차적으로 playerNumber 할당
                Debug.Log($"Player {CurrentRoom.players[i].nickname} assigned playerNumber: {CurrentRoom.players[i].playerNumber}");
            }
        }
    }

    void SetMyPlayerNumber()
    {
        string mySession = WebSocketManager.Instance.ClientSessionId;
        foreach (var player in CurrentRoom.players)
        {
            if (player.sessionId == mySession)
            {
                MyPlayerNumber = player.playerNumber;
                Debug.Log($"My player number is {MyPlayerNumber}");
                return;
            }
        }
        MyPlayerNumber = -1;  // 내 playerNumber를 찾지 못했을 경우
        Debug.LogWarning("My playerNumber not found");
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
        Debug.Log("[RoomManager] HandleRoomListUpdated: " + (rooms != null ? rooms.Count : 0) + " rooms");
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

    public void RequestStartGame()
    {
        Debug.Log("[RoomManager] RequestStartGame");
        SendAction("START_GAME");
    }

    void UpdateLobbyUI(Room room)
    {
        Debug.Log("[RoomManager] UpdateLobbyUI called");

        // 예시로, 플레이어 수를 로그로 출력하는 코드
        Debug.Log($"RoomId: {room.roomId}, Players Count: {room.players.Length}");

        // 실제 UI 업데이트 로직을 여기에 추가하세요
        // 예: 플레이어 목록 갱신, 방 상태 변경 등
        foreach (var player in room.players)
        {
            Debug.Log($"Player: {player.nickname}, Player Number: {player.playerNumber}");
        }
    }
    // ===============================================================
    // 🔥 JSON 구조체
    // ===============================================================

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

        // ← 여기 추가: 서버가 보내는 플레이어 번호를 담기 위한 필드
        public int playerNumber = -1;
    }

}
