using UnityEngine;
using UnityEngine.SceneManagement;  // 씬 관련 API

public class SceneChanger : MonoBehaviour
{
    // 씬 이름을 인스펙터에서 설정할 수 있게 public 변수로 만듦
    public string sceceName;

    // 버튼 OnClick 이벤트에 이 함수 연결
    public void ChangeScene()
    {
        // 씬 로드 (이름으로 씬 전환)
        SceneManager.LoadScene(sceceName);
    }
}