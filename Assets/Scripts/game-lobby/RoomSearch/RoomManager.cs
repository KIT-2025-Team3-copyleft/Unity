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

    private bool isHost = false;
    public bool IsHost => isHost;

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
        {
            WebSocketManager.Instance.OnServerMessage += Handle;
        }
    }
    void OnDisable()
    {
        if (WebSocketManager.Instance != null)
        {
            WebSocketManager.Instance.OnServerMessage -= Handle;
        }
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
            // 방 생성 (정상 작동 중)
            case "roomCreated":
                RoomCreated created = JsonUtility.FromJson<RoomCreated>(json);
                currentRoomId = created.room_id;
                currentRoomPrivate = created.isPrivate;

                isHost = true;

                PlayerPrefs.SetString("RoomID", currentRoomId);
                PlayerPrefs.SetInt("IsPrivate", currentRoomPrivate ? 1 : 0);
                PlayerPrefs.Save();

                SceneManager.LoadScene("LobbyScene");
                break;

            // 방 목록
            case "roomList":
                RoomList list = JsonUtility.FromJson<RoomList>(json);
                roomList = new List<string>(list.rooms);
                OnRoomListUpdated?.Invoke(roomList);
                break;

            // 핵심 수정 — joinSuccess 시 RoomID 저장해야 함!
            case "joinSuccess":
                RoomJoinSuccess join = JsonUtility.FromJson<RoomJoinSuccess>(json);

                // 여기가 추가된 부분
                currentRoomId = join.room_id;

                isHost = false;

                PlayerPrefs.SetString("RoomID", currentRoomId);
                PlayerPrefs.Save();

                NotifyRoomJoinResult(true);

                SceneManager.LoadScene("LobbyScene");
                break;

            case "joinFail":
                NotifyRoomJoinResult(false);
                break;

            // 빠른 시작에서 방 없음
            case "noRoom":
                OnFastStartNoRoom?.Invoke();
                break;

            // 핵심 수정 — fastStartRoom 도착 시 RoomID 저장하고 JoinRoom
            case "fastStartRoom":
                FastStartRoom fs = JsonUtility.FromJson<FastStartRoom>(json);

                isHost = false;

                currentRoomId = fs.room_id;
                PlayerPrefs.SetString("RoomID", currentRoomId);
                PlayerPrefs.Save();

                // UI 알림
                OnFastStartFoundRoom?.Invoke(fs.room_id);

                // 실제 입장 요청
                JoinRoom(fs.room_id);

                break;
        }
    }

    [Serializable]
    public class RoomJoinSuccess : BaseMsg
    {
        public string room_id;
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
