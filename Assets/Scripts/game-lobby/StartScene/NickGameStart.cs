using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections;

public class NickNameStart : MonoBehaviour
{
    public TMP_InputField nicknameInput;
    public TMP_Text serverMessageText;
    public UnityEngine.UI.Button startButton;
    public string sceneName = "RoomSerachScene";
    public float loadDelayTime = 1.0f;

    [Header("메시지 자동 숨김 시간")]
    public float messageHideDelay = 1.0f;   // ← 몇 초 뒤에 숨길지

    private bool isProcessing = false;
    private Coroutine messageCoroutine;      // ← 현재 돌고 있는 코루틴 저장용

    void Start()
    {
        if (NickNameManager.Instance != null)
        {
            NickNameManager.Instance.OnNicknameSuccess += OnSuccess;
            NickNameManager.Instance.OnNicknameFail += OnFail;
        }
        else
        {
            Debug.LogError("[NickNameStart] NickNameManager 인스턴스를 찾을 수 없습니다. 씬에 NickNameManager 오브젝트가 있는지 확인하세요.");
        }

        if (serverMessageText != null)
        {
            serverMessageText.text = "";
            serverMessageText.gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (NickNameManager.Instance != null)
        {
            NickNameManager.Instance.OnNicknameSuccess -= OnSuccess;
            NickNameManager.Instance.OnNicknameFail -= OnFail;
            Debug.Log("[NickNameStart] 닉네임 이벤트 구독 해지 완료.");
        }
    }

    public void OnClick_Start()
    {
        if (isProcessing) return;

        string nick = nicknameInput.text.Trim();

        if (string.IsNullOrEmpty(nick))
        {
            ShowServerMessage("닉네임을 입력하세요!", Color.red, true);
            return;
        }

        isProcessing = true;
        nicknameInput.interactable = false;
        if (startButton != null) startButton.interactable = false;

        ShowServerMessage("닉네임 확인 중...", new Color(0.45f, 0.30f, 0.15f), false); // 진행중은 안 숨김
        NickNameManager.Instance.SendNickname(nick);
    }

    private void OnSuccess(string nickname)
    {
        // 성공 메시지는 씬 전환 전에 짧게 보여주고 싶으면 true로 바꿔도 됨
        //ShowServerMessage($"닉네임 설정 성공: {nickname}", Color.green, false);
        StartCoroutine(LoadSceneDelay());
    }

    private void OnFail(string message)
    {
        isProcessing = false;
        nicknameInput.interactable = true;
        if (startButton != null) startButton.interactable = true;

        // 중복/실패 메시지는 몇 초 뒤에 자동 숨김
        ShowServerMessage(message, Color.red, true);

        // nicknameInput.text = "";
        // nicknameInput.ActivateInputField();
    }

    /// <summary>
    /// 서버 메시지 TMP 텍스트를 표시하고, 필요하면 일정 시간 후 자동으로 숨김
    /// </summary>
    private void ShowServerMessage(string msg, Color color, bool autoHide)
    {
        if (serverMessageText == null) return;

        serverMessageText.text = msg;
        serverMessageText.color = color;
        serverMessageText.gameObject.SetActive(true);

        // 이전 코루틴 돌고 있으면 중지
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
            messageCoroutine = null;
        }

        // autoHide이면 몇 초 후 숨김
        if (autoHide)
        {
            messageCoroutine = StartCoroutine(HideMessageAfterDelay(messageHideDelay));
        }
    }

    private IEnumerator HideMessageAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        if (serverMessageText != null)
        {
            serverMessageText.gameObject.SetActive(false);
        }

        messageCoroutine = null;
    }

    private IEnumerator LoadSceneDelay()
    {
        yield return new WaitForSeconds(loadDelayTime);
        SceneManager.LoadScene(sceneName);
    }
}
