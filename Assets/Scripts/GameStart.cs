using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;

public class NickNameStart : MonoBehaviour
{
    public TMP_InputField nicknameInput;
    public TMP_Text serverMessageText; // 서버 메시지를 보여줄 TMP_Text
    public string sceneName;

    private const string NicknameKey = "PlayerNickname";
    private const string ServerUrl = "http://localhost/get.php"; // PHP 경로 확인!

    public void OnClick_Start()
    {
        string nick = nicknameInput.text.Trim();

        if (string.IsNullOrEmpty(nick))
        {
            Debug.LogWarning("닉네임이 비어있습니다.");
            if (serverMessageText != null)
                serverMessageText.text = "닉네임을 입력해주세요!";
            return;
        }

        PlayerPrefs.SetString(NicknameKey, nick);
        PlayerPrefs.Save();

        StartCoroutine(SendNicknameToServer(nick));
    }

    private IEnumerator SendNicknameToServer(string nickname)
    {
        WWWForm form = new WWWForm();
        form.AddField("nickname", nickname);

        using (UnityWebRequest www = UnityWebRequest.Post(ServerUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("서버 응답: " + www.downloadHandler.text);

                // JSON 파싱
                ServerResponse response = JsonUtility.FromJson<ServerResponse>(www.downloadHandler.text);

                if (response.success)
                {
                    if (serverMessageText != null)
                        serverMessageText.text = $"{response.message}\n닉네임: {response.nickname}";

                    // 씬 이동 전에 잠깐 보여주고 싶다면 딜레이
                    yield return new WaitForSeconds(1f);

                    SceneManager.LoadScene(sceneName);
                }
                else
                {
                    if (serverMessageText != null)
                        serverMessageText.text = response.message;
                }
            }
            else
            {
                Debug.LogError("서버 전송 실패: " + www.error);
                if (serverMessageText != null)
                    serverMessageText.text = "서버 연결 실패";
            }
        }
    }

    // PHP 응답 구조체
    [System.Serializable]
    private class ServerResponse
    {
        public bool success;
        public string message = "";   // 기본값 지정
        public string nickname = "";  // 기본값 지정
        public string created_at = ""; // GET 요청용 추가
    }

}
