using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
public class RoomJoin : MonoBehaviour
{
    public TMP_InputField codeInput;
    public GameObject messageObject;
    public float messageDuration = 1;

    void Start()
    {
        if (messageObject != null) messageObject.SetActive(false);
    }

    public void OnClickJoin()
    {
        if (RoomManager.Instance == null)
        {
            Show("RoomManager 없음!");
            return;
        }

        string code = codeInput.text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            Show("코드를 입력하세요.");
            return;
        }

        // 서버에 Join 요청
        RoomManager.Instance.JoinRoom(code);
        // 서버 응답을 RoomManager.Handle에서 받아서 처리

    }

    // 서버 응답에 따라 호출할 함수 예시
    public void OnJoinResult(bool success)
    {
        if (success)
        {
            Debug.Log("방 입장!!");
            
        }
        else
        {
            Show("방을 찾을 수 없습니다.");
        }
    }

    void Show(string msg)
    {
        if (messageObject != null)
        {
            messageObject.SetActive(true);
        }
        Debug.Log(msg);
        CancelInvoke(nameof(Hide));
        Invoke(nameof(Hide), messageDuration);
    }

    void Hide()
    {
        if (messageObject != null) messageObject.SetActive(false);
    }
}
