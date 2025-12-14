using TMPro;
using UnityEngine;


public class RoomJoin : MonoBehaviour
{
    public static RoomJoin Instance; // 싱글톤

    public TMP_InputField codeInput;
    public GameObject messageObject;
    public float messageDuration = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
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
            ShowMessage("RoomManager 없음!");
            return;
        }

        string code = codeInput.text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            ShowMessage("코드를 입력하세요!");
            return;
        }

        // 서버에 Join 요청
        RoomManager.Instance.JoinRoom(code);
    }

    // RoomManager 이벤트에서 호출
    private void HandleJoinResult(bool success)
    {
        if (success)
        {
            Debug.Log("[RoomJoin] 방 입장 성공!");
        }
        else
        {
            ShowMessage("존재하지 않는 방입니다."); // 실패 메시지 표시
        }
    }

    public void ShowMessage(string msg)
    {
        if (messageObject != null)
        {
            messageObject.SetActive(true);
            var tmp = messageObject.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmp != null)
                tmp.text = msg;

            CancelInvoke(nameof(HideMessage));
            Invoke(nameof(HideMessage), messageDuration);
        }
    }

    void HideMessage()
    {
        if (messageObject != null)
            messageObject.SetActive(false);
    }
}
