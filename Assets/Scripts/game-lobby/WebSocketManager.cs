using UnityEngine;
using WebSocketSharp;
using System;
using System.Collections;

public class WebSocketManager : MonoBehaviour
{
    public static WebSocketManager Instance { get; private set; }

    // 지금은 안 쓰지만, 나중을 위해 남겨둠
    public string ClientSessionId { get; private set; }

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
}
