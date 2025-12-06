using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;


public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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
    public Button toggleCardButton; 
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


    public bool IsUILinked = false;
    public void LinkLocalPlayerUIElements(GameObject localPlayerRoot)
    {
        // 1. 초기화
        HistoryItems.Clear();
        playerSlotImages.Clear();
        playerSlotTexts.Clear();
        cardButtons.Clear();
        cardTexts.Clear();

        Transform canvasRoot = localPlayerRoot.transform.Find("Canvas");
        if (canvasRoot == null) return;
        canvasRoot.gameObject.SetActive(true);

        Transform oracleRoot = canvasRoot.Find("Role&OraclePanel");
        Transform persistentRoot = canvasRoot.Find("PersistentOraclePanel");
        Transform systemPanel = canvasRoot.Find("SystemPanel");
        Transform slotPanelRoot = canvasRoot.Find("SlotPanel");

        // 1) Oracle & Role
        if (oracleRoot != null)
        {
            oraclePanel = oracleRoot.gameObject;
            oracleText = oracleRoot.Find("oracleText")?.GetComponent<TextMeshProUGUI>();
            roleText = oracleRoot.Find("roleText")?.GetComponent<TextMeshProUGUI>();
            traitorText = oracleRoot.Find("traitorText")?.GetComponent<TextMeshProUGUI>();
        }

        // 2) Persistent Oracle
        if (persistentRoot != null)
        {
            persistentOraclePanel = persistentRoot.gameObject;
            persistentOracleText = persistentRoot.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        // 3) System Message
        if (systemPanel != null)
        {
            Transform sysText = systemPanel.Find("systemText");
            if (sysText != null) GameManager.Instance.systemMessageText = sysText.GetComponent<TextMeshProUGUI>();
        }

        // 4) Judgment Scroll
        Transform judgmentScrollTransform = canvasRoot.Find("JudgmentScroll");
        if (judgmentScrollTransform != null)
        {
            judgmentScroll = judgmentScrollTransform.gameObject;
            judgmentText = judgmentScrollTransform.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        // 5) Visual Cue Animator
        visualCueAnimator = localPlayerRoot.GetComponentInChildren<Animator>(true);


        // 6) History Items
        Transform historyPanelRoot = canvasRoot.Find("HistoryPanel");
        if (historyPanelRoot != null)
        {
            historyPanel = historyPanelRoot.GetComponent<RectTransform>();
            Transform toggleBtn = historyPanelRoot.Find("Button");
            if (toggleBtn != null)
            {
                historyToggleButton = toggleBtn.GetComponent<Button>();
                toggleButtonText = toggleBtn.GetComponentInChildren<TextMeshProUGUI>();
            }
            HistoryItems.AddRange(historyPanelRoot.GetComponentsInChildren<HistoryItem>(true));
        }

        historyPanel = historyPanelRoot.GetComponent<RectTransform>();

        closedYPosition = historyPanel.anchoredPosition.y;
        isHistoryOpen = false;

        if (historyToggleButton != null)
        {
            historyToggleButton.onClick.RemoveAllListeners();
            historyToggleButton.onClick.AddListener(ToggleHistoryPanel);
            UpdateToggleButtonText();
        }


        if (slotPanelRoot != null)
        {
            cardSelectionPanel = slotPanelRoot.gameObject;

            // Timer
            Transform timerRoot = slotPanelRoot.Find("Timer");
            if (timerRoot != null)
            {
                countdownText = timerRoot.GetComponentInChildren<TextMeshProUGUI>(true);
                timerCircle = timerRoot.Find("timerCircle")?.GetComponent<Image>();
            }

            // Sentence Slots 
            for (int i = 1; i <= 4; i++)
            {
                Transform container = slotPanelRoot.Find($"SlotsContainer_{i}");
                if (container != null)
                {
                    // Image 
                    Image img = container.GetComponentInChildren<Image>(true);
                    if (img != null) playerSlotImages.Add(img);

                    // Text 
                    TextMeshProUGUI txt = container.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null) playerSlotTexts.Add(txt);
                }
            }

            //  Card Buttons
            Transform buttonsRoot = slotPanelRoot.Find("Buttons");
            if (buttonsRoot != null)
            {
                Button[] foundButtons = buttonsRoot.GetComponentsInChildren<Button>(true);

                foreach (Button btn in foundButtons)
                {
                    cardButtons.Add(btn);

                    TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (btnText != null)
                    {
                        cardTexts.Add(btnText);
                    }
                }
            }
        }

        Transform toggleBtnRoot = canvasRoot.Find("toggleCardButton");
        if (toggleBtnRoot != null)
        {
            toggleCardButton = toggleBtnRoot.GetComponent<Button>();

            if (toggleCardButton != null)
            {
                toggleCardButton.onClick.RemoveAllListeners();
                toggleCardButton.onClick.AddListener(ToggleCardPanel);
                Debug.Log("✔ 카드 토글 버튼 리스너 연결 완료.");
            }
        }

        IsUILinked = true;

        // 게임 시작 직후 UI 비활성화
        oraclePanel.SetActive(false);
        cardSelectionPanel.SetActive(false);
        persistentOraclePanel.SetActive(false);
        systemPanel.gameObject.SetActive(false);
        judgmentScroll.SetActive(false);

        // UI 연결 직후 슬롯 색상을 기본값(그린)으로 초기화
        UpdateSlotColors(new Dictionary<string, string>());
    }


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

    // 🌟 AddHistoryItem 매개변수 수정: mission 추가
    public void AddHistoryItem(RoundResult msg, int roundNumber, string mission, Dictionary<string, string> slotColors, List<string> finalWords)
    {
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

            // 🌟 SetData 호출 시 mission 전달
            historyItem.SetData(msg, slotColors, roundNumber, mission, finalWords);
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


        persistentOracleText.text = $"{oracle}";
        persistentOracleText.gameObject.SetActive(true);


        StartCoroutine(HideOraclePanelAfterSeconds(3f));
    }

    private IEnumerator HideOraclePanelAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (oraclePanel != null)
        {
            oraclePanel.SetActive(false);
            traitorText.gameObject.SetActive(false);
        }

    }

    public void ShowTraitorInfo(string godPersonality)
    {

        traitorText.text = $"신의 페르소나: {godPersonality}";
        traitorText.gameObject.SetActive(true);

    }


    public void SetupCardButtons(List<string> cards)
    {
        if (cardSelectionPanel != null)
            cardSelectionPanel.SetActive(true);

        for (int i = 0; i < cardButtons.Count; i++)
        {
            Button button = cardButtons[i];
            TextMeshProUGUI textComponent = cardTexts[i];

            if (i < cards.Count)
            {
                string cardWord = cards[i];
                textComponent.text = cardWord;

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => GameManager.Instance.OnCardSelected(cardWord));
            }
            else
            {
                textComponent.text = "";
                button.onClick.RemoveAllListeners();
            }

            button.gameObject.SetActive(true);

            CardHoverHandler hoverHandler = button.GetComponent<CardHoverHandler>();
            if (hoverHandler == null) hoverHandler = button.gameObject.AddComponent<CardHoverHandler>();
            // 🌟 GameManager.Instance.mySlot 대신 MySessionId를 사용하여 targetSlotId를 설정할 수도 있지만, 
            // 현재 구조상 mySlot을 사용합니다.
            hoverHandler.targetSlotId = GameManager.Instance.mySlot;
        }
    }

    // 🌟 새로 추가된 카드 패널 토글 함수
    public void ToggleCardPanel()
    {
        if (cardSelectionPanel != null)
        {
            // 현재 상태의 반대(Not)로 설정합니다.
            bool isActive = cardSelectionPanel.activeSelf;
            cardSelectionPanel.SetActive(!isActive);
            Debug.Log($"[UI] 카드 패널 활성화 상태 토글: {!isActive}");
        }
    }

    // 🌟 슬롯 테두리 색상을 업데이트하는 함수
    public void UpdateSlotColors(Dictionary<string, string> slotPlayerColors)
    {
        // playerSlotImages 리스트는 slot1, slot2, ... 순서로 연결되어 있다고 가정합니다.
        for (int i = 0; i < playerSlotImages.Count; i++)
        {
            string slotId = $"slot{i + 1}"; // "slot1", "slot2", "slot3", "slot4"

            string colorName = "green"; // 기본 색상은 "green"

            if (slotPlayerColors.ContainsKey(slotId))
            {
                colorName = slotPlayerColors[slotId];
            }

            // 이미지 컴포넌트 색상 업데이트
            playerSlotImages[i].color = GetUnityColor(colorName);
        }
    }

    // 색상 문자열을 Unity Color 객체로 변환 (HistoryItem에서 가져옴)
    private Color GetUnityColor(string colorName)
    {
        switch (colorName.ToLower())
        {
            case "red":
                return Color.red;
            case "blue":
                return Color.blue;
            case "green":
                return Color.green;
            case "yellow":
                return Color.yellow;
            case "pink":
                return new Color(1f, 0.41f, 0.71f);
            default:
                // 매칭되는 색이 없을 경우 기본 색상인 초록을 반환
                Debug.LogWarning($"Unknown color name: {colorName}. Defaulting to green.");
                return Color.green;
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