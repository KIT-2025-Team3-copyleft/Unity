using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;

    private string currentRoomId;
    private string currentRoomCode;            // 추가: roomCode 보관
    public string CurrentRoomId => currentRoomId;
    public string CurrentRoomCode => currentRoomCode; // 추가: 외부에서 접근 가능
    public bool IsHost { get; private set; }

    public GameObject playerPrefab;
    public string myPlayerId; 
    public List<PlayerInfo> playersInRoom = new List<PlayerInfo>();
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

        // 중복 구독 방지
        WebSocketManager.Instance.OnServerMessage -= Handle;
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

                // 기존 roomId 저장
                currentRoomId = join.room.roomId;
                PlayerPrefs.SetString("RoomID", currentRoomId);

                // 추가: roomCode 저장 (UI에 띄울 때 사용)
                currentRoomCode = join.room.roomCode;
                PlayerPrefs.SetString("RoomCode", currentRoomCode);

                PlayerPrefs.Save();

                // host 여부 판단 (기존 코드 유지)
                IsHost = false;
                if (join.room.players != null)
                {
                    foreach (var p in join.room.players)
                    {
                        if (p.host)
                        {
                            IsHost = true;
                            break;
                        }
                    }
                }

                Debug.Log($"[RoomManager] JOIN_SUCCESS roomCode={currentRoomCode} IsHost={IsHost}");

                OnJoinResult?.Invoke(true);
                SceneManager.LoadScene("LobbyScene");
                break;

                Debug.Log("[RoomManager] IsHost = " + IsHost);

                OnJoinResult?.Invoke(true);
                SceneManager.LoadScene("LobbyScene");
                break;

            case "LOBBY_UPDATE":
                var update = JsonUtility.FromJson<LobbyUpdateEvent>(json);
                LobbyManager.Instance?.NotifyLobbyUpdated(update.room);                
                break;

            case "JOIN_FAILED":
                var failed = JsonUtility.FromJson<JoinFailedEvent>(json);
                // 메시지 표시
                UnityMainThreadDispatcher.EnqueueOnMainThread(() => {
                    RoomJoin.Instance.ShowMessage(failed.message);
                    // 필요하면 UI 전환도 여기서
                    RoomJoin.Instance.ShowMessage(failed.message);  // RoomJoin 화면 열기
                });
                OnJoinResult?.Invoke(false);
                break;

            case "LEAVE_SUCCESS":
                var leave = JsonUtility.FromJson<LeaveSuccessEvent>(json);

                Debug.Log("▶ 방 나가기 성공 (서버 응답): " + leave.message);

                // 씬 전환은 이미 ExitRoom에서 처리하므로 여기서는 생략
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
    public class LeaveSuccessEvent : BaseEvent
    {
        public string message;
    }

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
    public class PlayerInfo
    {
        public string id; public string nickname; public bool host;
    }

    public event Action<List<string>> OnRoomListUpdated;
    public event Action<bool> OnJoinResult;
}