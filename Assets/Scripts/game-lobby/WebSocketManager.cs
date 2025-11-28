using UnityEngine;
using WebSocketSharp;
using System;
using System.Collections;

public class WebSocketManager : MonoBehaviour
{
    public static WebSocketManager Instance { get; private set; }

    public string ClientSessionId { get; private set; }   // 🔥 세션ID 저장

    private WebSocket ws;
    private bool isConnecting = false;

    public bool IsConnected => ws != null && ws.ReadyState == WebSocketState.Open;

    public event Action<string> OnServerMessage;
    public event Action OnConnected;

    [SerializeField] private string serverUrl = "ws://168.107.19.253/ws/";

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

        ws = new WebSocket(serverUrl);
        isConnecting = true;

        ws.OnOpen += (s, e) =>
        {
            Debug.Log("[WS] 연결 성공");
            isConnecting = false;
            OnConnected?.Invoke();
        };

        ws.OnMessage += (s, e) =>
        {
            string msg = e.Data;

            // ---------------------------
            // 🔥 세션 ID 자동 추출 처리
            // ---------------------------
            TryExtractSessionId(msg);

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
    }

    IEnumerator Reconnect()
    {
        yield return new WaitForSeconds(3f);
        Connect();
    }

    public void Send(string json)
    {
        if (!IsConnected)
        {
            Debug.LogWarning("[WS] 연결 안됨");
            return;
        }

        ws.Send(json);
        Debug.Log("[WS] 클라 → 서버: " + json);
    }

    private void OnApplicationQuit()
    {
        if (ws != null && ws.IsAlive) ws.Close();
    }

    // ============================================================
    // 🔥 서버 메시지에서 sessionId 자동 추출하는 함수
    // ============================================================
    private void TryExtractSessionId(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        // 1) CONNECTED 같은 메시지에서 sessionId 바로 오는 경우
        if (json.Contains("\"sessionId\"") && json.Contains("\"event\":\"CONNECTED\""))
        {
            try
            {
                SessionDirect direct = JsonUtility.FromJson<SessionDirect>(json);
                if (!string.IsNullOrEmpty(direct.sessionId))
                {
                    ClientSessionId = direct.sessionId;
                    Debug.Log("[WS] sessionId 저장됨 (CONNECTED): " + ClientSessionId);
                    return;
                }
            }
            catch { }
        }

        // 2) JOIN_SUCCESS 또는 LOBBY_UPDATE 안 players[] 에서 sessionId 찾기
        if (json.Contains("\"players\""))
        {
            try
            {
                JoinSessionExtractor wrapper = JsonUtility.FromJson<JoinSessionExtractor>(json);
                if (wrapper != null && wrapper.data != null && wrapper.data.players != null)
                {
                    foreach (var p in wrapper.data.players)
                    {
                        if (!string.IsNullOrEmpty(p.sessionId))
                        {
                            ClientSessionId = p.sessionId;
                            Debug.Log("[WS] sessionId 저장됨 (players 배열): " + ClientSessionId);
                            return;
                        }
                    }
                }
            }
            catch { }
        }
    }

    [Serializable]
    private class SessionDirect
    {
        public string sessionId;
    }

    [Serializable]
    private class JoinSessionExtractor
    {
        public JoinData data;

        [Serializable]
        public class JoinData
        {
            public Player[] players;
        }

        [Serializable]
        public class Player
        {
            public string sessionId;
        }
    }

}
