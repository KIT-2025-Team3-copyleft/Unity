using UnityEngine;
using WebSocketSharp; // WebSocketSharp 라이브러리 사용
using System;
using System.Collections;
using System.Collections.Generic;

public class WebSocketManager : MonoBehaviour
{
    public static WebSocketManager Instance { get; private set; }
    public string ClientSessionId { get; private set; }
    // 🌟 웹소켓 연결 객체 (WebSocketManager에서 가져옴)
    private WebSocket ws;
    private bool isConnecting = false;

    public bool IsConnected => ws != null && ws.ReadyState == WebSocketState.Open;

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

    // 🌟 웹소켓 연결 로직 (WebSocketManager에서 통합)
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

    // 🌟 메시지 전송 로직 (기존 NetworkManager 유지)
    public void Send(string json)
    {
        if (!IsConnected)
        {
            Debug.LogError("[WS] 연결되지 않았습니다. 메시지 전송 실패: " + json);
            return;
        }

        try
        {
            ws.Send(json);
            Debug.Log("[WS] 클라 → 서버: " + json);
        }
        catch (Exception ex)
        {
            Debug.LogError("[WS] 전송 에러: " + ex.Message);
        }
    }

    // ----------------------------------------------------
    // 🔸 API 명세 기반 구조화된 요청 전송 함수들
    // ----------------------------------------------------

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

    public void SendGameReady()
    {
        SendRequest("GAME_READY", null);
    }

    public void SendCardSelection(string card)
    {
        var payload = new CardSelectionPayload { card = card };
        SendRequest("SELECT_CARD", payload);
    }

    public void SendProposeVote(bool agree)
    {
        var payload = new ProposeVotePayload { agree = agree };
        SendRequest("PROPOSE_VOTE", payload);
    }

    public void SendCastVote(string targetSessionId)
    {
        var payload = new CastVotePayload { targetSessionId = targetSessionId };
        SendRequest("CAST_VOTE", payload);
    }

    public void SendChat(string messageContent)
    {
        var payload = new ChatMessagePayload { message = messageContent };
        SendRequest("SEND_CHAT", payload);
    }
    public void SendLeaveRoom()
    {
        SendRequest("LEAVE_ROOM", null);
    }
    public void SendBackToRoom()
    {
        SendRequest("BACK_TO_ROOM", null);
    }
}