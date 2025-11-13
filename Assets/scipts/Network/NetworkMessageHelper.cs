using UnityEngine;

public static class NetworkMessageHelper
{
    public static string GetEventType(string json)
    {
        var temp = JsonUtility.FromJson<EventTypeWrapper>(json);
        return temp.eventType;
    }

    [System.Serializable]
    private class EventTypeWrapper { public string eventType; }
}