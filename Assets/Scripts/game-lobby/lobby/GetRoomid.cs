using UnityEngine;
using TMPro;

public class GetRoomId : MonoBehaviour
{
    public TMP_Text roomIdText;

    void Start()
    {
        if (RoomManager.Instance == null)
        {
            Debug.LogError("RoomManager ¾øÀ½!");
            return;
        }

        string id = RoomManager.Instance.CurrentRoomId;
        roomIdText.text = id;
    }
}
