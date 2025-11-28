using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using Room = RoomManager.Room;
public class GameStartManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text countdownText;
    public TMP_Text warningText;
    public Button startButton;

    private Coroutine countdownCoroutine;

    private void Start()
    {
        // UI 초기화
        countdownText.gameObject.SetActive(false);
        if (warningText != null)
            warningText.gameObject.SetActive(false);

        startButton.gameObject.SetActive(false);

        // 방장이면 버튼 보이기
        if (RoomManager.Instance.IsHost)
        {
            startButton.gameObject.SetActive(true);
        }

        // 서버 이벤트 구독
        LobbyManager.Instance.OnGameStartTimer += OnGameStartTimer;
        LobbyManager.Instance.OnTimerCancelled += OnTimerCancelled;
        LobbyManager.Instance.OnLoadGameScene += OnLoadGameScene;
    }

    private void OnDestroy()
    {
        // 이벤트 해제
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnGameStartTimer -= OnGameStartTimer;
            LobbyManager.Instance.OnTimerCancelled -= OnTimerCancelled;
            LobbyManager.Instance.OnLoadGameScene -= OnLoadGameScene;
        }
    }

    // -----------------------
    // 1. 방장 버튼 클릭 → 서버 요청
    // -----------------------
    public void OnStartButtonPressed()
    {
        if (!RoomManager.Instance.IsHost) return;

        // 서버에 START_GAME 요청
        LobbyManager.Instance.StartGame();
    }

    // -----------------------
    // 2. 서버 → GAME_START_TIMER
    // -----------------------
    private void OnGameStartTimer(int seconds)
    {
        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);

        countdownCoroutine = StartCoroutine(ShowCountdown(seconds));
    }

    private IEnumerator ShowCountdown(int seconds)
    {
        countdownText.gameObject.SetActive(true);

        while (seconds > 0)
        {
            countdownText.text = seconds.ToString();
            yield return new WaitForSeconds(1f);
            seconds--;
        }

        countdownText.text = "";
    }

    // -----------------------
    // 3. 서버 → TIMER_CANCELLED
    // -----------------------
    private void OnTimerCancelled()
    {
        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);

        countdownText.gameObject.SetActive(false);
        countdownText.text = "";
    }

    // -----------------------
    // 4. 서버 → LOAD_GAME_SCENE
    // -----------------------
    private void OnLoadGameScene(Room room)
    {
        countdownText.gameObject.SetActive(false);
        SceneManager.LoadScene("GameScene");
    }
}
