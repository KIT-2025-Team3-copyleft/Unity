using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class RoomJoin : MonoBehaviour
{
    public TMP_InputField codeInput;
    public GameObject messageObject;
    public float messageDuration = 1f;

    private void OnEnable()
    {
        // RoomManager 이벤트 구독
        if (RoomManager.Instance != null)
            RoomManager.Instance.OnJoinResult += HandleJoinResult;
    }

    private void OnDisable()
    {
        if (RoomManager.Instance != null)
            RoomManager.Instance.OnJoinResult -= HandleJoinResult;
    }

    void Start()
    {
        if (messageObject != null)
            messageObject.SetActive(false);
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
    }

    // RoomManager에서 이벤트로 호출됨
    private void HandleJoinResult(bool success)
    {
        if (success)
        {
            Debug.Log("[RoomJoin] 방 입장 성공!");
            // Scene 이동 등
            // SceneManager.LoadScene("RoomScene");
        }
        else
        {
            // 실패 메시지 표시
            Show("방을 찾을 수 없습니다."); // 여기서 JOIN_FAILED 메시지 표시
        }
    }

    void Show(string msg)
    {
        messageObject.SetActive(true);
        Debug.Log(msg);
        CancelInvoke(nameof(Hide));
        Invoke(nameof(Hide), messageDuration);
    }
    void Hide()
    {
        if (messageObject != null)
            messageObject.SetActive(false);
    }
}
