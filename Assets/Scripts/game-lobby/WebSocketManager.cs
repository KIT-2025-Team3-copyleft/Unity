using UnityEngine;
using WebSocketSharp;
using System;
using System.Collections;

public class WebSocketManager : MonoBehaviour
{
    public static WebSocketManager Instance { get; private set; }

    private WebSocket ws;
    private bool isConnecting = false;

    public bool IsConnected => ws != null && ws.ReadyState == WebSocketState.Open;

    // 외부에서 메시지 수신 시 구독 가능
    public event Action<string> OnServerMessage;

    [SerializeField]
    private string serverUrl = "ws://localhost:7777/";
    public event Action OnConnected;
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

            // 초기 메시지 예: Unity 식별
            ws.Send("{\"action\":\"unity\"}");
        };

        ws.OnMessage += (sender, e) =>
        {
            try
            {
                string msg = e.Data;
                if (!string.IsNullOrEmpty(msg))
                {
                    // 메인 쓰레드에서 실행
                    UnityMainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        OnServerMessage?.Invoke(msg); // 여기서 PlayerPrefs, UI 등 안전하게 실행 가능
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[WS] OnMessage 예외: " + ex);
            }
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
    }

    private void OnApplicationQuit()
    {
        if (ws != null && ws.IsAlive) ws.Close();
    }
}
