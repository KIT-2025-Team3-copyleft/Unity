using UnityEngine;

public class ResultManager : MonoBehaviour
{
    public static ResultManager Instance;

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

    private void HandleServerMessage(string rawJson)
    {
        string eventType = ExtractEventType(rawJson);

        if (eventType == "GAME_OVER")
        {
            var msg = JsonUtility.FromJson<GameOverWrapper>(rawJson);
            ResultUIManager.Instance.ShowResult(msg.message, 10f);
        }
    }

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

    [System.Serializable]
    private class GameOverWrapper
    {
        public string eventField;
        public string message;
        public GameOverData data;
    }

    [System.Serializable]
    private class GameOverData
    {
        public RoomData room;
        public string winnerRole;
    }

    [System.Serializable]
    private class RoomData
    {
        public string roomId;
        public string roomCode;
        public string roomTitle;
        public string hostSessionId;
        public string status;
        public PlayerData[] players;
    }

    [System.Serializable]
    private class PlayerData
    {
        public string sessionId;
        public string nickname;
        public bool isHost;
        public string color;
        public string connectionStatus;
    }
}
