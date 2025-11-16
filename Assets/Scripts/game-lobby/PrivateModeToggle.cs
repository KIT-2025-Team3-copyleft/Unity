using UnityEngine;
using UnityEngine.UI;

public class PrivateModeToggle : MonoBehaviour
{
    public Toggle privateToggle; // Inspector에서 연결

    void Start()
    {
        if (privateToggle != null)
        {
            // 시작 시 체크 해제
            privateToggle.isOn = false;

            // UI 표시용 로그
            privateToggle.onValueChanged.AddListener(OnToggleChanged);
        }
    }

    // 토글 상태 변경 시 UI용 로그만 출력
    void OnToggleChanged(bool value)
    {
        Debug.Log("비공개 모드 토글 상태: " + (value ? "ON" : "OFF"));
    }

    // 외부에서 토글 상태 가져오기
    public bool GetPrivateState()
    {
        return privateToggle != null && privateToggle.isOn;
    }


}
