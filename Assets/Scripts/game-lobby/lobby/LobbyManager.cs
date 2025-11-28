using System;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    [Header("현재 방 정보")]
    public RoomManager.Room CurrentRoom { get; private set; }
    public bool IsHost { get; private set; }

    public string MySessionId => WebSocketManager.Instance.ClientSessionId;

    // 🔹 이벤트: UI, SpawnManager, HostUI에서 구독
    public event Action<RoomManager.Room> OnLobbyUpdated;
    public event Action<bool> OnHostChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[LobbyManager] Awake: Instance set");
        }
        else
        {
            Destroy(gameObject);
            Debug.Log("[LobbyManager] Awake: Duplicate destroyed");
        }
    }

    private void OnEnable()
    {
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.OnLobbyUpdated += HandleLobby;
            HandleLobby(RoomManager.Instance.CurrentRoom);
            Debug.Log("[LobbyManager] OnEnable: Subscribed to RoomManager.OnLobbyUpdated");
        }
    }

    private void OnDisable()
    {
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.OnLobbyUpdated -= HandleLobby;
            Debug.Log("[LobbyManager] OnDisable: Unsubscribed from RoomManager.OnLobbyUpdated");
        }
    }

    // 🔹 서버에서 받은 LOBBY_UPDATE를 모든 구독자에게 전달
    private void HandleLobby(RoomManager.Room room)
    {
        if (room == null)
        {
            Debug.Log("[LobbyManager] HandleLobby: Room is null");
            return;
        }

        CurrentRoom = room;
        IsHost = (CurrentRoom.hostSessionId == MySessionId);

        Debug.Log($"[LobbyManager] HandleLobby: RoomId={CurrentRoom.roomId}, Players={CurrentRoom.players.Length}, IsHost={IsHost}");

        OnHostChanged?.Invoke(IsHost);
        OnLobbyUpdated?.Invoke(room);
    }

}
