using UnityEngine;

public class MakeRoomButton : MonoBehaviour
{
    public GameObject MakeRoomPopup;  

    void Start()
    {
        MakeRoomPopup.SetActive(false);
    }

    public void OnClickMakeRoom()
    {
        ShowMakePopup();
    }

    public void ShowMakePopup()
    {
        if (MakeRoomPopup != null)
            MakeRoomPopup.SetActive(true);
    }

    public void HideMakePopup()
    {
        if (MakeRoomPopup != null)
            MakeRoomPopup.SetActive(false);
    }
}
