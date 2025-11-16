using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class RoomJoin : MonoBehaviour
{
    public TMP_InputField codeInput;   // 방 코드 입력 필드
    public GameObject messageObject;   // 메시지 TMP_Text가 들어있는 오브젝트
    public TMP_Text messageText;       // 메시지 표시용 TMP_Text
    public string correctCode = "1234";
    public string lobbySceneName = "LobbyScene";
    public float messageDuration = 1.0f; // 메시지 표시 시간

    void Start()
    {
        if (messageObject != null)
            messageObject.SetActive(false); // 시작 시 숨김
    }

    public void OnClickJoin()
    {
        string enteredCode = codeInput.text.Trim();

        if (enteredCode == correctCode)
        {
            SceneManager.LoadScene(lobbySceneName); // 성공 시 바로 입장
        }
        else
        {
            messageObject.SetActive(true);           // 틀리면 메시지 보여주기
            CancelInvoke(nameof(HideMessage));
            Invoke(nameof(HideMessage), 1.0f);      // 1초 후 숨기기
        }
    }

    void HideMessage()
    {
        if (messageObject != null)
            messageObject.SetActive(false);
    }
}
