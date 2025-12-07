using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ResultUIManager : MonoBehaviour
{
    public static ResultUIManager Instance;

    [Header("UI Components")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public Button backToRoomButton;
    public Button goLobbyButton;
    public TextMeshProUGUI countdownText;

    private Coroutine autoExitCo;

    private void Awake()
    {
        Instance = this;
    }

    public void ShowResult(string message, float autoExitSeconds = 10f)
    {
        resultPanel.SetActive(true);
        resultText.text = message;
        countdownText.text = autoExitSeconds.ToString("F0");

        backToRoomButton.onClick.RemoveAllListeners();
        backToRoomButton.onClick.AddListener(() =>
        {
            SendBackToRoom();
            HideResult();
        });

        goLobbyButton.onClick.RemoveAllListeners();
        goLobbyButton.onClick.AddListener(() =>
        {
            GoLobby();
            HideResult();
        });

        if (autoExitCo != null) StopCoroutine(autoExitCo);
        autoExitCo = StartCoroutine(AutoExitTimer(autoExitSeconds));
    }

    private IEnumerator AutoExitTimer(float time)
    {
        float t = time;
        while (t > 0f)
        {
            countdownText.text = Mathf.Ceil(t).ToString();
            yield return new WaitForSeconds(1f);
            t -= 1f;
        }
        GoLobby();
        HideResult();
    }

    private void SendBackToRoom()
    {
        var msg = new
        {
            action = "BACK_TO_ROOM",
            payload = (object)null
        };
        string json = JsonUtility.ToJson(msg);

        WebSocketManager.Instance.Send(json);
    }


    private void GoLobby()
    {
        // 여기서 씬 전환
        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    }

    private void HideResult()
    {
        resultPanel.SetActive(false);
    }
}
