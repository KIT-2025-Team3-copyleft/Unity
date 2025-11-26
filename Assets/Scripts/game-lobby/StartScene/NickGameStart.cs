using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class NickNameStart : MonoBehaviour
{
    public TMP_InputField nicknameInput;
    public TMP_Text serverMessageText;
    public UnityEngine.UI.Button startButton; // ⭐ 버튼 컴포넌트 참조 추가 (UX 개선용)
    public string sceneName = "RoomSerach";
    public float loadDelayTime = 1.0f; // ⭐ 딜레이 시간을 인스펙터에서 조절 가능하게 변경

    private bool isProcessing = false; // ⭐ 상태 변수: 서버 통신 중인지 확인

    void Start()
    {
        // Null 체크 및 이벤트 구독
        if (NickNameManager.Instance != null)
        {
            NickNameManager.Instance.OnNicknameSuccess += OnSuccess;
            NickNameManager.Instance.OnNicknameFail += OnFail;
        }
        else
        {
            Debug.LogError("[NickNameStart] NickNameManager 인스턴스를 찾을 수 없습니다. 씬에 NickNameManager 오브젝트가 있는지 확인하세요.");
        }
    }

    // ⭐ 필수 추가: 컴포넌트 파괴 시 구독 해지
    void OnDestroy()
    {
        if (NickNameManager.Instance != null)
        {
            NickNameManager.Instance.OnNicknameSuccess -= OnSuccess;
            NickNameManager.Instance.OnNicknameFail -= OnFail;
            Debug.Log("[NickNameStart] 닉네임 이벤트 구독 해지 완료.");
        }
    }

    // ⭐ 버튼 클릭 시 유니티 이벤트 시스템에 연결할 메서드
    public void OnClick_Start()
    {
        // 1. 상태 확인 (이중 전송 방지)
        if (isProcessing) return;

        string nick = nicknameInput.text.Trim();

        if (string.IsNullOrEmpty(nick))
        {
            serverMessageText.text = "닉네임을 입력하세요!";
            return;
        }

        // 2. 전송 시작 및 UI/상태 잠금
        isProcessing = true;
        nicknameInput.interactable = false;
        if (startButton != null) startButton.interactable = false; // 버튼 비활성화

        serverMessageText.text = "닉네임 확인 중..."; // 사용자에게 피드백 제공

        // 3. 닉네임 전송
        NickNameManager.Instance.SendNickname(nick);
    }

    private void OnSuccess(string nickname)
    {
        // 성공했으므로 isProcessing 해제는 필요 없음 (씬 전환 예정)

        serverMessageText.text = $"닉네임 설정 성공: {nickname}";
        StartCoroutine(LoadSceneDelay());
    }

    private void OnFail(string message)
    {
        // ⭐ 실패 시 상태 해제 및 UI 복구
        isProcessing = false;
        nicknameInput.interactable = true;
        if (startButton != null) startButton.interactable = true; // 버튼 재활성화

        serverMessageText.text = message;
    }

    private IEnumerator LoadSceneDelay()
    {
        // ⭐ 인스펙터 변수 사용
        yield return new WaitForSeconds(loadDelayTime);
        SceneManager.LoadScene(sceneName);
    }
}