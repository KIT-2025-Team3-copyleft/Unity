using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameStartManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text countdownText;
    public TMP_Text warningText;
    public GameObject startButton;

    void Start()
    {
        // 처음에는 카운트다운 텍스트 숨기기
        countdownText.gameObject.SetActive(false);

        // 경고 메시지도 기본 비활성화
        if (warningText != null)
            warningText.gameObject.SetActive(false);
    }

    public void OnStartButtonPressed()
    {
        if (CounterManager.Instance.playerCount != 4)
        {
            StartCoroutine(ShowNotEnoughPlayers());
            return;
        }

        startButton.SetActive(false);
        StartCoroutine(StartCountdown());
    }

    private IEnumerator ShowNotEnoughPlayers()
    {
        warningText.gameObject.SetActive(true);
        warningText.text = "4명이 아닙니다!";
        yield return new WaitForSeconds(2f);
        warningText.gameObject.SetActive(false);
    }

    private IEnumerator StartCountdown()
    {
        countdownText.gameObject.SetActive(true);

        countdownText.text = "3";
        yield return new WaitForSeconds(1f);

        countdownText.text = "2";
        yield return new WaitForSeconds(1f);

        countdownText.text = "1";
        yield return new WaitForSeconds(1f);

        countdownText.gameObject.SetActive(false);

        SendPlayerCountToServer(CounterManager.Instance.playerCount);

        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene("GameScene");
    }

    private void SendPlayerCountToServer(int count)
    {
        if (WebSocketManager.Instance != null &&
            WebSocketManager.Instance.IsConnected)
        {
            string json = "{\"type\":\"playerCount\",\"count\":" + count + "}";
            WebSocketManager.Instance.Send(json);
        }
        else
        {
            Debug.LogWarning("웹소켓 연결 안됨!");
        }
    }
}
