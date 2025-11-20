using UnityEngine;
using UnityEngine.UI;   // 버튼 사용하려면 필요!
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameStartManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text countdownText;
    public TMP_Text warningText;
    public Button startButton;   // GameObject → Button으로 변경

    void Start()
    {
        countdownText.gameObject.SetActive(false);

        if (warningText != null)
            warningText.gameObject.SetActive(false);

        startButton.gameObject.SetActive(false);
        // 방장이면 버튼 활성화
        if(startButton.interactable = RoomManager.Instance.IsHost)
        {
            startButton.gameObject.SetActive(true);
        }
    }

    public void OnStartButtonPressed()
    {
        if (CounterManager.Instance.playerCount != 4)
        {
            StartCoroutine(ShowNotEnoughPlayers());
            return;
        }

        startButton.gameObject.SetActive(false);
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
