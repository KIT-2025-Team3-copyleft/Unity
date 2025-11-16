using UnityEngine;
using UnityEngine.SceneManagement;

public class FastStartButton : MonoBehaviour
{
    public GameObject noRoomPopup;

    void Start()
    {
        // 시작할 때 팝업 꺼두기
        noRoomPopup.SetActive(false);
    }

    // "빠른 시작" 버튼 눌렀을 때 실행
    public void OnClickFastStart()
    {
        // 임시: 방이 있는지 체크
        bool roomExists = RoomManager.Instance.HasRoom();

        if (roomExists)
        {
            // 방 입장 처리
            RoomManager.Instance.JoinRoom(0); // 0번 방 입장
            SceneManager.LoadScene("LobbyScene");
        }
        else
        {
            ShowNoRoomPopup();
        }
    }

    // 팝업 열기
    public void ShowNoRoomPopup()
    {
        noRoomPopup.SetActive(true);
    }

    // 팝업 닫기
    public void HideNoRoomPopup()
    {
        noRoomPopup.SetActive(false);
    }
}
