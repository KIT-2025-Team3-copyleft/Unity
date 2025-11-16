/*
 * GPT 님이 
 */



using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;

    private WebSocket websocket;

    [Header("Server Settings")]
    public string serverUrl = "ws://localhost:8080/ws";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private async void Start()
    {
        websocket = new WebSocket(serverUrl);

        websocket.OnOpen += () => Debug.Log("WebSocket 연결 성공!");
        websocket.OnError += (e) => Debug.LogError("WebSocket 에러: " + e);
        websocket.OnClose += (e) => Debug.Log("WebSocket 연결 종료");

        websocket.OnMessage += (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("서버 수신: " + message);
            GameManager.Instance.OnServerMessage(message);
        };

        await websocket.Connect();
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif
    }

    private async void OnApplicationQuit()
    {
        if (websocket != null)
            await websocket.Close();
    }

    public async void SendCardSelection(string playerId, string card)
    {
        CardSelectionMessage msg = new CardSelectionMessage { playerId = playerId, selectedCard = card };
        string json = JsonUtility.ToJson(msg);
        await SendToServer(json);
    }

    public async void SendChatMessage(string playerId, string message)
    {
        ChatMessage msg = new ChatMessage { playerId = playerId, message = message };
        string json = JsonUtility.ToJson(msg);
        await SendToServer(json);
    }

    public async void SendRoundReady(string playerId)
    {
        RoundReadyMessage msg = new RoundReadyMessage { playerId = playerId };
        string json = JsonUtility.ToJson(msg);
        await SendToServer(json);
    }

    private async System.Threading.Tasks.Task SendToServer(string json)
    {
        if (websocket.State == WebSocketState.Open)
        {
            await websocket.SendText(json);
            Debug.Log("서버 전송: " + json);
        }
        else Debug.LogWarning("WebSocket이 연결되어 있지 않습니다.");
    }
}

[Serializable]
public class CardSelectionMessage { public string playerId; public string selectedCard; }

[Serializable]
public class ChatMessage { public string playerId; public string message; }

[Serializable]
public class RoundReadyMessage { public string playerId; }