using UnityEngine;

public class lookingfor : MonoBehaviour
{
    public GameObject RoomPopup;  // RoomPopup 연결

    void Start()
    {
        // 시작할 때 팝업 꺼두기
        RoomPopup.SetActive(false);
    }

    public void OnClickLookingfor()
    {
        ShowRoomPopup();
    }
    
    public void ShowRoomPopup()
    {
        if (RoomPopup != null)
            RoomPopup.SetActive(true);
    }

    public void HideRoomPopup()
    {
        if (RoomPopup != null)
            RoomPopup.SetActive(false);
    }
}
