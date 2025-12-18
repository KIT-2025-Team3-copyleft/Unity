using UnityEngine;
using System;
using System.Collections;

#if !UNITY_WEBGL || UNITY_EDITOR
using WebSocketSharp; // 데스크탑에서만 사용
#else
using System.Runtime.InteropServices; // WebGL에서 jslib 사용
#endif

public class WebSocketManager : MonoBehaviour
{
    public static WebSocketManager Instance { get; private set; }
    public string ClientSessionId { get; private set; }

#if !UNITY_WEBGL || UNITY_EDITOR
    private WebSocket ws; // 데스크탑용
#else
    // WebGL용 jslib 함수 선언
    [DllImport("__Internal")]
    private static extern void WebSocketConnect(string url);
    
    [DllImport("__Internal")]
    private static extern void WebSocketSend(string message);
    
    [DllImport("__Internal")]
    private static extern void WebSocketClose();
    
    [DllImport("__Internal")]
    private static extern int WebSocketIsConnected();
#endif

    private bool isConnecting = false;

    public bool IsConnected
    {
        get
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            return ws != null && ws.ReadyState == WebSocketState.Open;
#else
            return WebSocketIsConnected() == 1;
#endif
        }
    }

    public event Action<string> OnServerMessage;
    public event Action OnConnected;

    [SerializeField] private string serverUrl = "wss://godschoice.kro.kr/ws";

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
        StartCoroutine(ConnectDelay());
    }

    IEnumerator ConnectDelay()
    {
        yield return new WaitForSeconds(0.3f);
        Connect();
    }

    void Connect()
    {
        if (isConnecting || IsConnected) return;

        isConnecting = true;

#if !UNITY_WEBGL || UNITY_EDITOR
        // ========== 데스크탑: WebSocketSharp 사용 ==========
        ws = new WebSocket(serverUrl);

        ws.OnOpen += (s, e) =>
        {
            Debug.Log("[WS] 연결 성공");
            isConnecting = false;
            OnConnected?.Invoke();
        };

        ws.OnMessage += (s, e) =>
        {
            string msg = e.Data;
            if (!string.IsNullOrEmpty(msg))
            {
                UnityMainThreadDispatcher.EnqueueOnMainThread(() =>
                {
                    OnServerMessage?.Invoke(msg);
                });
            }
            Debug.Log("[WS] 서버 → 클라: " + msg);
        };

        ws.OnClose += (s, e) =>
        {
            Debug.LogWarning("[WS] 연결 종료: " + e.Reason);
            isConnecting = false;
            StartCoroutine(Reconnect());
        };

        ws.OnError += (s, e) =>
        {
            Debug.LogError("[WS] 에러: " + e.Message);
            isConnecting = false;
        };

        ws.ConnectAsync();
#else
        // ========== WebGL: JavaScript WebSocket 사용 ==========
        WebSocketConnect(serverUrl);
        isConnecting = false;
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    // ========== JavaScript에서 호출되는 콜백 함수들 ==========
    void OnWebSocketOpen(string message)
    {
        Debug.Log("[WS] WebGL 연결 성공");
        isConnecting = false;
        OnConnected?.Invoke();
    }

    void OnWebSocketError(string error)
    {
        Debug.LogError("[WS] WebGL 에러: " + error);
        isConnecting = false;
    }

    void OnWebSocketMessage(string message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            OnServerMessage?.Invoke(message);
            Debug.Log("[WS] 서버 → 클라: " + message);
        }
    }

    void OnWebSocketClose(string message)
    {
        Debug.LogWarning("[WS] WebGL 연결 종료");
        isConnecting = false;
        StartCoroutine(Reconnect());
    }
#endif

    IEnumerator Reconnect()
    {
        yield return new WaitForSeconds(3f);
        Connect();
    }

    public void Send(string json)
    {
        if (!IsConnected)
        {
            Debug.LogError("[WS] 연결되지 않았습니다. 메시지 전송 실패: " + json);
            return;
        }

        try
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            ws.Send(json);
#else
            WebSocketSend(json);
#endif
            Debug.Log("[WS] 클라 → 서버: " + json);
        }
        catch (Exception ex)
        {
            Debug.LogError("[WS] 전송 에러: " + ex.Message);
        }
    }

    // 기존 SendRequest, SendGameReady 등은 그대로 유지
    private void SendRequest(string action, object payload)
    {
        string json;
        try
        {
            if (payload != null)
            {
                string payloadJson = JsonUtility.ToJson(payload);
                json = $"{{\"action\":\"{action}\",\"payload\":{payloadJson}}}";
            }
            else
            {
                json = $"{{\"action\":\"{action}\",\"payload\":null}}";
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WS] JSON 직렬화 에러 ({action}): {ex.Message}");
            return;
        }

        Send(json);
    }

    public void SendGameReady() => SendRequest("GAME_READY", null);
    public void SendCardSelection(string card) => SendRequest("SELECT_CARD", new CardSelectionPayload { card = card });
    public void SendProposeVote(bool agree) => SendRequest("PROPOSE_VOTE", new ProposeVotePayload { agree = agree });
    public void SendCastVote(string targetSessionId) => SendRequest("CAST_VOTE", new CastVotePayload { targetSessionId = targetSessionId });
    public void SendChat(string messageContent) => SendRequest("SEND_CHAT", new ChatMessagePayload { message = messageContent });
    public void SendLeaveRoom() => SendRequest("LEAVE_ROOM", null);
    public void SendBackToRoom() => SendRequest("BACK_TO_ROOM", null);
}