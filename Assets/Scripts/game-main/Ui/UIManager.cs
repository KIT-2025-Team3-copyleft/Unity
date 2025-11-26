using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        SetupHistoryPanel();
    }

    private Dictionary<int, Color> defaultSlotColors = new Dictionary<int, Color>();

    [Header("Oracle & Role")]
    public GameObject oraclePanel;
    public TextMeshProUGUI oracleText;
    public TextMeshProUGUI roleText;
    public GameObject persistentOraclePanel;
    public TextMeshProUGUI persistentOracleText;

    [Header("Traitor Info")]
    public TextMeshProUGUI traitorText;

    [Header("History Panel")]
    public RectTransform historyPanel;
    public Button historyToggleButton;
    public TextMeshProUGUI toggleButtonText;
    public List<HistoryItem> HistoryItems;

    private bool isHistoryOpen = false;
    private float closedYPosition;
    public float targetOpenedYPosition; 
    private float slideDuration = 0.3f;

    [Header("Card UI")]
    public GameObject cardSelectionPanel;
    public List<Button> cardButtons;
    public List<TextMeshProUGUI> cardTexts;

    [Header("Sentence Slots")]
    public List<Image> playerSlotImages;
    public List<TextMeshProUGUI> playerSlotTexts;

    [Header("Judgment Scroll UI")]
    public GameObject judgmentScroll;
    public TextMeshProUGUI judgmentText;

    [Header("Visual Cue")]
    public Animator visualCueAnimator;

    [Header("Timer")]
    public TextMeshProUGUI countdownText;
    public Image timerCircle;


    // 히스토리 패널 초기 설정
    private void SetupHistoryPanel()
    {
        if (historyPanel != null)
        {
            closedYPosition = historyPanel.anchoredPosition.y;

            isHistoryOpen = false; 
        }

        if (historyToggleButton != null)
        {
            historyToggleButton.onClick.AddListener(ToggleHistoryPanel);
            UpdateToggleButtonText();
        }

        if (cardSelectionPanel != null)
        {
            cardSelectionPanel.SetActive(false);
        }
    }

    // 히스토리 패널 열기/닫기 토글 함수
    public void ToggleHistoryPanel()
    {
        isHistoryOpen = !isHistoryOpen;
        float targetY = isHistoryOpen ? targetOpenedYPosition : closedYPosition;

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
            // 열린 상태일 때는 닫으라는 의미의 ▼를 표시
            toggleButtonText.text = isHistoryOpen ? "▼" : "▲";
        }
    }

    public void AddHistoryItem(RoundResult msg, int roundNumber, Dictionary<string, string> slotColors, List<string> finalWords)
    {
        // 0. 리스트 유효성 및 라운드 번호 확인
        if (HistoryItems == null || HistoryItems.Count < roundNumber || roundNumber < 1)
        {
            Debug.LogError($"HistoryItem list is invalid or round number ({roundNumber}) is out of bounds.");
            return;
        }

        int targetIndex = roundNumber - 1;

        HistoryItem historyItem = HistoryItems[targetIndex];

        if (historyItem != null)
        {
            historyItem.gameObject.SetActive(true);

            historyItem.SetData(msg, slotColors, roundNumber, finalWords);
        }
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

        // 영구 신탁 텍스트 표시
        if (persistentOracleText != null)
        {
            persistentOracleText.text = $"신탁: {oracle}";
            persistentOracleText.gameObject.SetActive(true);
        }

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


    public void SetupCardButtons(List<string> cards)
    {
        if (cardSelectionPanel != null)
        {
            cardSelectionPanel.SetActive(true);
        }

        for (int i = 0; i < cardButtons.Count; i++)
        {
            Button button = cardButtons[i];
            TextMeshProUGUI textComponent = cardTexts[i];

            if (i < cards.Count)
            {
                string cardCopy = cards[i];

                button.gameObject.SetActive(true); 
                textComponent.text = cardCopy;    

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => GameManager.Instance.OnCardSelected(cardCopy));

                CardHoverHandler hoverHandler = button.GetComponent<CardHoverHandler>();
                if (hoverHandler == null) hoverHandler = button.gameObject.AddComponent<CardHoverHandler>();

                hoverHandler.targetSlotId = GameManager.Instance.mySlot;
            }
            else
            {
                button.gameObject.SetActive(false);
            }
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

        TextMeshProUGUI tmpText = cardButtons[index].GetComponentInChildren<TextMeshProUGUI>();
        string card = tmpText != null ? tmpText.text : "";

        GameManager.Instance.OnCardSelected(card);
    }

    public void HighlightSlot(string slotId, bool highlight, string hoveredWord) 
    {
        if (slotId.StartsWith("slot") && int.TryParse(slotId.Substring(4), out int slotIndex))
        {
            int index = slotIndex - 1;

            if (index >= 0 && index < playerSlotTexts.Count)
            {
                TextMeshProUGUI slotText = playerSlotTexts[index];

                if (!defaultSlotColors.ContainsKey(index))
                {
                    defaultSlotColors[index] = slotText.color;
                }

                
                if (highlight)
                {
                    slotText.color = Color.black; 
                    slotText.text = hoveredWord;  
                }
                else
                {
                    if (defaultSlotColors.ContainsKey(index))
                    {
                        slotText.color = defaultSlotColors[index];
                    }

                    slotText.text = "";
                }
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


    public void SetGameUIActive(bool isActive)
    {
        // 상시 신탁
        if (persistentOraclePanel != null)
        {
            persistentOraclePanel.SetActive(isActive);
        }

        // 히스토리 패널 
        if (historyPanel != null)
        {
            historyPanel.gameObject.SetActive(isActive);
        }
        if (historyToggleButton != null)
        {
            historyToggleButton.gameObject.SetActive(isActive);
        }

        // 채팅창 
        //if (chatWindowObject != null)
        //{
        //    chatWindowObject.SetActive(isActive);
        //}

        // 단어 선택 창 띄우는 버튼 

        // 심판 제안 (여기로 옮겨야함)
        //if (trialButton != null)
        //{
        //    trialButton.gameObject.SetActive(isActive);
        //}
    }


    // 완성된 문장 출력 
    public void DisplaySentence(string sentence)
    {
        if (judgmentText != null)
        {
            string resultMessage = $"--- 완성된 문장 ---\n\n";
            resultMessage += $"{sentence}";
            judgmentText.text = resultMessage;
        }
    }

    // 심판 이유 출력 
    public void DisplayJudgmentReason(string reason)
    {
        if (judgmentText != null)
        {
            string resultMessage = $"{reason}";
            judgmentText.text = resultMessage;
        }
    }


    public void PlayVisualCue(VisualCue cue)
    {
        if (visualCueAnimator != null)
        {
            visualCueAnimator.SetTrigger(cue.effect);
        }
        else
        {
            Debug.LogWarning($"VisualCue Animator is not connected in UIManager.");
        }
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