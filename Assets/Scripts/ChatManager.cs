using UnityEngine;

public class ChatManager : MonoBehaviour
{
    public static ChatManager Instance { get; private set; }
    public Chathandler chathandler;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        WebSocketManager.Instance.OnServerMessage += HandleServerMessage;
    }

    private void OnDestroy()
    {
        if (WebSocketManager.Instance != null)
            WebSocketManager.Instance.OnServerMessage -= HandleServerMessage;
    }

    // =====================================================
    // 서버 메시지 처리
    // =====================================================
    private void HandleServerMessage(string rawJson)
    {
        string eventType = ExtractEventType(rawJson);

        switch (eventType)
        {
            case "CHAT_MESSAGE":
                HandleChatMessage(rawJson);
                break;

            case "ERROR_MESSAGE":
                HandleErrorMessage(rawJson);
                break;

            default:
                Debug.Log($"[ChatManager] Unknown event: {eventType}");
                break;
        }
    }

    // =====================================================
    // "event" 값을 추출하기 위한 임시 구조체
    // =====================================================
    [System.Serializable]
    private class EventTypeExtractor
    {
        public string eventField;
    }

    private string ExtractEventType(string json)
    {
        string mod = json.Replace("\"event\"", "\"eventField\"");
        EventTypeExtractor e = JsonUtility.FromJson<EventTypeExtractor>(mod);
        return e.eventField;
    }

    // =====================================================
    // CHAT_MESSAGE 처리
    // =====================================================
    private void HandleChatMessage(string rawJson)
    {
        string mod = rawJson.Replace("\"event\"", "\"eventType\"");
        ChatMessageWrapper wrapper = JsonUtility.FromJson<ChatMessageWrapper>(mod);

        chathandler.AddChatMessage(wrapper.data.sender, wrapper.data.color, wrapper.data.content);
    }

    // =====================================================
    // ERROR_MESSAGE 처리
    // =====================================================
    private void HandleErrorMessage(string rawJson)
    {
        string mod = rawJson.Replace("\"event\"", "\"eventType\"");
        ErrorMessageWrapper wrapper = JsonUtility.FromJson<ErrorMessageWrapper>(mod);

        chathandler.AddSystemMessage($"{wrapper.message} ({wrapper.code})");
    }

    // =====================================================
    // 메시지 서버 전송
    // =====================================================
    public void SendChat(string message)
    {
        WebSocketManager.Instance.SendChat(message);
    }


    // =====================================================
    // ====== JSON 구조용 내부 클래스들 =====================
    // =====================================================

    [System.Serializable]
    private class ChatMessageData
    {
        public string sender;
        public string color;
        public string content;
        public string formattedMessage;
    }

    [System.Serializable]
    private class ChatMessageWrapper
    {
        public string eventType;
        public ChatMessageData data;
    }

    [System.Serializable]
    private class ErrorMessageWrapper
    {
        public string eventType;
        public string message;
        public string code;
    }
}
