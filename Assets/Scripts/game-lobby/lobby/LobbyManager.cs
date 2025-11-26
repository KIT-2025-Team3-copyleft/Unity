using UnityEngine;
using System;
using static RoomManager;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public Room currentRoom;

    public event Action<Room> OnLobbyUpdated;
    public event Action<int> OnGameStartTimer;
    public event Action OnTimerCancelled;
    public event Action<Room> OnLoadGameScene;

    private void Start()
    {
        if (WebSocketManager.Instance != null)
            WebSocketManager.Instance.OnServerMessage += HandleServerMessage;
    }

    private void OnDestroy()
    {
        if (WebSocketManager.Instance != null)
            WebSocketManager.Instance.OnServerMessage -= HandleServerMessage;
    }
    public string mySessionId; // WebSocket 연결 시 서버에서 받아온 세션ID
    public int MyPlayerNumber { get; private set; } // 1,2,3,4
    public bool IsHost { get; private set; }
    private void HandleServerMessage(string json)
    {
        BaseEvent baseEvent = JsonUtility.FromJson<BaseEvent>(json);

        switch (baseEvent.eventType)
        {
            case "LOBBY_UPDATE":
                var lobbyEvent = JsonUtility.FromJson<LobbyUpdateEvent>(json);
                currentRoom = lobbyEvent.room;
                UpdateMyPlayerNumber(currentRoom);
                UpdatePlayerStatus();
                OnLobbyUpdated?.Invoke(currentRoom);
                break;

            case "GAME_START_TIMER":
                GameStartTimerEvent timerEvent = JsonUtility.FromJson<GameStartTimerEvent>(json);
                OnGameStartTimer?.Invoke(timerEvent.seconds);
                break;

            case "TIMER_CANCELLED":
                OnTimerCancelled?.Invoke();
                break;

            case "LOAD_GAME_SCENE":
                LoadGameSceneEvent loadEvent = JsonUtility.FromJson<LoadGameSceneEvent>(json);
                currentRoom = loadEvent.room;
                OnLoadGameScene?.Invoke(currentRoom);
                break;
        }
    }

    private void UpdatePlayerStatus()
    {
        if (currentRoom == null || currentRoom.players == null) return;

        // 플레이어 번호 (players 배열 index)
        for (int i = 0; i < currentRoom.players.Length; i++)
        {
            if (currentRoom.players[i] == mySessionId)
            {
                MyPlayerNumber = i + 1; // index 0 → Player1
                break;
            }
        }

        // 방장 여부 체크
        IsHost = (currentRoom.hostSessionId == mySessionId);

        Debug.Log($"[Lobby] 내 Player 번호: {MyPlayerNumber}, 방장?: {IsHost}");
    }

    // 방 나가기 요청
    public void LeaveRoom()
    {
        if (WebSocketManager.Instance == null) return;

        LeaveRoomRequest dto = new LeaveRoomRequest();
        string json = JsonUtility.ToJson(dto);
        WebSocketManager.Instance.Send(json);

        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    }

    // 게임 시작 요청
    public void StartGame()
    {
        if (WebSocketManager.Instance == null) return;

        StartGameRequest dto = new StartGameRequest();
        string json = JsonUtility.ToJson(dto);
        WebSocketManager.Instance.Send(json);
    }

    public event Action OnMySpawnReady;

    public void UpdateMyPlayerNumber(Room room)
    {
        if (room.players == null) return;

        for (int i = 0; i < room.players.Length; i++)
        {
            if (room.players[i] == mySessionId)
            {
                MyPlayerNumber = i + 1; // 번호는 1부터
                Debug.Log($"[LobbyManager] 나는 Player {MyPlayerNumber} 입니다.");
                OnMySpawnReady?.Invoke();
                return;
            }
        }
    }
}

[Serializable]
public class LeaveRoomRequest
{
    public string action = "LEAVE_ROOM";
}

[Serializable]
public class StartGameRequest
{
    public string action = "START_GAME";
}

// 서버 → 클라이언트 이벤트 DTO
[Serializable]
public class BaseEvent
{
    public string eventType;
}

[Serializable]
public class LobbyUpdateEvent
{
    public string eventType;
    public Room room;
}

[Serializable]
public class GameStartTimerEvent
{
    public string eventType;
    public int seconds;
}

[Serializable]
public class TimerCancelledEvent
{
    public string eventType;
}

[Serializable]
public class LoadGameSceneEvent
{
    public string eventType;
    public Room room;
}

// Room DTO
[Serializable]
public class Room
{
    public string roomId;
    public string roomTitle;
    public string hostSessionId;
    public string[] players;
    public string status;
}
