using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;


    [Header("Oracle & Role")]
    public GameObject oraclePanel;
    public Text oracleText;
    public Text roleText;
    public GameObject persistentOraclePanel; 
    public Text persistentOracleText;

    [Header("Traitor Info")]
    public GameObject traitorPanel;
    public Text traitorText;

    [Header("History Panel")]
    public RectTransform historyPanel; 
    public Button historyToggleButton;    
    public Text toggleButtonText;        
    public Transform historyContentParent; 

    public GameObject historyItemPrefab;             
    private bool isHistoryOpen = false;
    private float closedYPosition;
    private float openedYPosition;
    private float slideDuration = 0.3f;

    [Header("Card UI")]
    public GameObject cardSelectionPanel;
    public GameObject cardButtonPrefab;
    public Transform cardContainer;
    private List<GameObject> cardButtons = new List<GameObject>();

    [Header("Sentence Slots")]
    public List<Image> playerSlotImages; 
    public List<Text> playerSlotTexts;

    [Header("Visual Cue")]
    public Animator visualCueAnimator;

    [Header("Timer")]
    public Text countdownText;
    public Image timerCircle;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        SetupHistoryPanel();
    }

    // 히스토리 패널
    private void SetupHistoryPanel()
    {
        if (historyPanel != null)
        {
            closedYPosition = historyPanel.anchoredPosition.y;

            openedYPosition = closedYPosition + historyPanel.sizeDelta.y;

            isHistoryOpen = true;
            historyPanel.anchoredPosition = new Vector2(
                historyPanel.anchoredPosition.x,
                openedYPosition
            );
        }

        if (historyToggleButton != null)
        {
            historyToggleButton.onClick.AddListener(ToggleHistoryPanel);
            UpdateToggleButtonText();
        }
    }

    // 히스토리 패널 열기/닫기 토글 함수
    public void ToggleHistoryPanel()
    {
        isHistoryOpen = !isHistoryOpen;
        float targetY = isHistoryOpen ? openedYPosition : closedYPosition;

        StartCoroutine(SlidePanel(targetY));
        UpdateToggleButtonText();
    }

    // 패널 이동 코루틴
    private IEnumerator SlidePanel(float targetY)
    {
        float startTime = Time.time;
        float startY = historyPanel.anchoredPosition.y;
        float distance = targetY - startY;

        while (Time.time < startTime + slideDuration)
        {
            float elapsed = Time.time - startTime;
            float t = elapsed / slideDuration;

            historyPanel.anchoredPosition = new Vector2(historyPanel.anchoredPosition.x, startY + distance * t);
            yield return null;
        }
        historyPanel.anchoredPosition = new Vector2(historyPanel.anchoredPosition.x, targetY);
    }

    private void UpdateToggleButtonText()
    {
        if (toggleButtonText != null)
        {
            toggleButtonText.text = isHistoryOpen ? "▲" : "▼";
        }
    }

    public void AddHistoryEntry(string roundResultText, int roundNumber, int scoreChange)
    {
        if (historyItemPrefab == null || historyContentParent == null)
        {
            Debug.LogError("History Item Prefab 또는 Content Parent가 UIManager에 연결되지 않았습니다!");
            return;
        }

        GameObject newItem = Instantiate(historyItemPrefab, historyContentParent);

        Text itemText = newItem.GetComponentInChildren<Text>();

        
    }



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
        // 1. 카드 선택 UI 팝업 활성화
        if (cardSelectionPanel != null)
        {
            cardSelectionPanel.SetActive(true);
        }

        // 기존 버튼 제거 및 리스트 초기화
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


    // 카드 선택 완료 시 버튼 비활성화 
    public void DisableMyCards()
    {
        // 카드 선택이 완료되면 모든 버튼을 비활성화
        foreach (var btn in cardButtons) btn.GetComponent<Button>().interactable = false;

        if (cardSelectionPanel != null)
        {
            cardSelectionPanel.SetActive(false);
        }
    }

    public void AutoSelectRandomCard()
    {
        if (cardButtons.Count == 0) return;
        int index = UnityEngine.Random.Range(0, cardButtons.Count);
        string card = cardButtons[index].GetComponentInChildren<Text>().text;
        GameManager.Instance.OnCardSelected(card);
    }

    public void HighlightSlot(string slotId, bool highlight)
    {
        // 슬롯 ID에서 인덱스 추출
        if (slotId.StartsWith("slot") && int.TryParse(slotId.Substring(4), out int slotIndex))
        {
            int index = slotIndex - 1;

            if (index >= 0 && index < playerSlotImages.Count)
            {
                Image slotImage = playerSlotImages[index];

                // 강조 색상 설정 (예시: 강조 시 밝아짐, 해제 시 기본색으로 복귀)
                Color baseColor = slotImage.color;
                Color targetColor = baseColor;

                if (highlight)
                {
                    // 강조 색상 (원하는 색상으로 설정, 예: 알파 값을 높여 더 밝게)
                    targetColor = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
                }
                else
                {
                    // 기본 색상 (Unity Editor에서 설정된 기본 색상/투명도)
                    // 만약 기본적으로 반투명하게 설정했다면, 해당 알파 값으로 복귀
                    targetColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.5f);
                }

                slotImage.color = targetColor;
            }
        }
    }


    // 슬롯에 단어를 표시하는 함수 
    public void UpdateMySentenceSlot(string slotId, string selectedWord)
    {
        if (slotId.StartsWith("slot") && int.TryParse(slotId.Substring(4), out int slotIndex))
        {
            int index = slotIndex - 1;

            if (index >= 0 && index < playerSlotTexts.Count)
            {
                playerSlotTexts[index].text = selectedWord;
            }
        }
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