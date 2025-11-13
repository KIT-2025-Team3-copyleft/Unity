using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [Header("Card UI")]
    public GameObject cardButtonPrefab;
    public Transform cardContainer;
    private List<GameObject> cardButtons = new List<GameObject>();

    [Header("Oracle & Role")]
    public GameObject oraclePanel;
    public Text oracleText;
    public Text roleText;

    [Header("Traitor Info")]
    public GameObject traitorPanel;
    public Text traitorText;

    [Header("Visual Cue")]
    public Animator visualCueAnimator;

    [Header("Card Selection Timer")]
    public Text countdownText;
    public Image timerCircle;


    // 신탁 및 역할 공개(역할은 1라운드에만)
    public void ShowOracleAndRole(string oracle, string role, int round)
    {

        if (round == 1)
            roleText.text = role;
        else
            roleText.text = "";


        oraclePanel.SetActive(true);
        oracleText.text = oracle;
        

        StartCoroutine(HideOraclePanelAfterSeconds(3f));
    }

    private IEnumerator HideOraclePanelAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        oraclePanel.SetActive(false);
        
    }

    public void ShowTraitorInfo(string godPersonality)
    {
        //traitorPanel.SetActive(true);
        traitorText.text = $"신의 페르소나: {godPersonality}";
    }

    // 카드 선택 (카드 생성)
    public void SetupCardButtons(List<string> cards)
    {
        foreach (var btn in cardButtons) Destroy(btn);
        cardButtons.Clear();

        foreach (string card in cards)
        {
            string cardCopy = card;
            GameObject newBtn = Instantiate(cardButtonPrefab, cardContainer);
            newBtn.GetComponentInChildren<Text>().text = cardCopy;
            cardButtons.Add(newBtn);

            newBtn.GetComponent<Button>().onClick.AddListener(() => GameManager.Instance.OnCardSelected(cardCopy));
        }
    }

    public void DisableMyCards()
    {
        foreach (var btn in cardButtons) btn.GetComponent<Button>().interactable = false;
    }


    // 제한 시간내 카드 미선택시 랜덤 선택
    public void AutoSelectRandomCard()
    {
        if (cardButtons.Count == 0) return;
        int index = UnityEngine.Random.Range(0, cardButtons.Count);
        string card = cardButtons[index].GetComponentInChildren<Text>().text;
        GameManager.Instance.OnCardSelected(card); 
    }

    public void PlayVisualCue(VisualCue cue)
    {
        if (visualCueAnimator != null) visualCueAnimator.SetTrigger(cue.effect);
    }

    // 타이머 (카드 선택, 배신자 투표)
    public IEnumerator StartTimer(float totalTime, Action onTimerEnd)
    {
        float timer = totalTime;

        if (countdownText != null) countdownText.gameObject.SetActive(true);
        if (timerCircle != null) timerCircle.gameObject.SetActive(true);

        while (timer > 0)
        {
            timer -= Time.deltaTime;
            if (countdownText != null)
                countdownText.text = Mathf.Ceil(timer).ToString();
            if (timerCircle != null)
                timerCircle.fillAmount = timer / totalTime;
            yield return null;
        }

        if (countdownText != null)
            countdownText.text = "0";
        if (timerCircle != null)
            timerCircle.fillAmount = 0;

        // 타이머 끝나면 UI 숨기기
        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (timerCircle != null) timerCircle.gameObject.SetActive(false);

        onTimerEnd?.Invoke();
    }

}