using UnityEngine;
using System;
using static RoomManager;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    public Room currentRoom;

    public string mySessionId;
    public int MyPlayerNumber { get; private set; }
    public bool IsHost { get; private set; }

    public event Action<Room> OnLobbyUpdated;
    public event Action<int> OnGameStartTimer;
    public event Action OnTimerCancelled;
    public event Action<Room> OnLoadGameScene;
    public event Action OnMySpawnReady;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (WebSocketManager.Instance != null)
            WebSocketManager.Instance.OnServerMessage += HandleServerMessage;
    }
    private void OnEnable()
    {
        OnMySpawnReady += SpawnMyPlayer;
    }

    private void OnDisable()
    {
        OnMySpawnReady -= SpawnMyPlayer;
    }

    private void OnDestroy()
    {
        if (WebSocketManager.Instance != null)
            WebSocketManager.Instance.OnServerMessage -= HandleServerMessage;
    }

    private void HandleServerMessage(string json)
    {
        BaseEvent baseEvent = JsonUtility.FromJson<BaseEvent>(json);

        switch (baseEvent.eventType)
        {
            case "LOBBY_UPDATE":
                LobbyUpdateEvent lobbyEvent = JsonUtility.FromJson<LobbyUpdateEvent>(json);
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
        if (currentRoom?.players == null) return;

        for (int i = 0; i < currentRoom.players.Length; i++)
        {
            if (currentRoom.players[i].sessionId == mySessionId)
            {
                MyPlayerNumber = i + 1;
                break;
            }
        }

        IsHost = (currentRoom.hostSessionId == mySessionId);

        Debug.Log($"[Lobby] PlayerNum={MyPlayerNumber}, Host={IsHost}");
    }

    public void UpdateMyPlayerNumber(Room room)
    {
        if (room.players == null) return;

        for (int i = 0; i < room.players.Length; i++)
        {
            if (room.players[i].sessionId == mySessionId)
            {
                MyPlayerNumber = i + 1;
                Debug.Log($"[Lobby] I am Player {MyPlayerNumber}");
                OnMySpawnReady?.Invoke();
                return;
            }
        }
    }
   

    public void NotifyLobbyUpdated(Room room)
    {
        OnLobbyUpdated?.Invoke(room);
    }
    public void LeaveRoom()
    {
        if (WebSocketManager.Instance == null) return;

        LeaveRoomRequest dto = new LeaveRoomRequest();
        string json = JsonUtility.ToJson(dto);
        WebSocketManager.Instance.Send(json);

        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    }

    public void StartGame()
    {
        if (WebSocketManager.Instance == null) return;

        StartGameRequest dto = new StartGameRequest();
        string json = JsonUtility.ToJson(dto);
        WebSocketManager.Instance.Send(json);
    }

    private void SpawnMyPlayer()
    {
        if (SpawnManager.Instance == null || currentRoom == null) return;

        int index = MyPlayerNumber - 1;
        Vector3 spawnPos = SpawnManager.Instance.GetSpawnPosition(index);

        if (PlayerSpawnManager.Instance.GetPlayerObject(mySessionId) == null)
        {
            GameObject player = Instantiate(RoomManager.Instance.playerPrefab, spawnPos, Quaternion.identity);
            PlayerSpawnManager.Instance.RegisterPlayer(mySessionId, player);
        }
        else
        {
            GameObject player = PlayerSpawnManager.Instance.GetPlayerObject(mySessionId);
            player.transform.position = spawnPos;
        }

        Debug.Log("SpawnManager.Instance: " + SpawnManager.Instance);
        Debug.Log("PlayerSpawnManager.Instance: " + PlayerSpawnManager.Instance);
        Debug.Log("currentRoom: " + currentRoom);
    }

}

[Serializable]
public class BaseEvent { public string eventType; }

[Serializable]
public class LobbyUpdateEvent { public string eventType; public Room room; }

[Serializable]
public class GameStartTimerEvent { public string eventType; public int seconds; }

[Serializable]
public class TimerCancelledEvent { public string eventType; }

[Serializable]
public class LoadGameSceneEvent { public string eventType; public Room room; }

[Serializable]
public class LeaveRoomRequest { public string action = "LEAVE_ROOM"; }

[Serializable]
public class StartGameRequest { public string action = "START_GAME"; }
