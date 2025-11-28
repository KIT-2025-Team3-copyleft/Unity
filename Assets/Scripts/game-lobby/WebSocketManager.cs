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

        // ★ 서버가 CONNECTED 이벤트로 내려주는 경우 처리
        if (json.Contains("\"sessionId\""))
        {
            // JSON 안에 "sessionId":"xxxx" 있으면 파싱
            try
            {
                SessionWrapper wrapper =
                    JsonUtility.FromJson<SessionWrapper>(json);

                if (!string.IsNullOrEmpty(wrapper.sessionId))
                {
                    ClientSessionId = wrapper.sessionId;
                    Debug.Log("[WS] 세션 ID 저장됨 : " + ClientSessionId);
                }
            }
            catch
            {
                // 무시 (JsonUtility는 배열/중첩구조에서 실패할 수 있음)
            }
        }
    }

    [Serializable]
    private class SessionWrapper
    {
        public string sessionId;
    }
}
