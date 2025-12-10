using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        if (lightningEffect != null) lightningEffect.SetActive(false);
        if (flowerEffect != null) flowerEffect.SetActive(false);
    }

    private readonly string[] SlotVisualOrder = { "SUBJECT", "TARGET", "HOW", "ACTION" };
    // 🌟 초기 상태의 슬롯 이름 저장 (Reset에 사용)
    private readonly List<string> InitialSlotRoleNames = new List<string> { "주체", "대상", "어떻게", "어쩐다" };

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
    public List<string> slotRoleNames = new List<string> { "주체", "대상", "어떻게", "어쩐다" };

    [Header("Judgment Scroll UI")]
    public GameObject judgmentScroll;
    public TextMeshProUGUI judgmentText;

    [Header("Visual Cue")]
    public GameObject judgmentCueObject; 
    public GameObject lightningEffect;
    public GameObject flowerEffect;

    [Header("Timer")]
    public TextMeshProUGUI countdownText;
    public Image timerCircle;

    [Header("Chat UI")]
    public GameObject chatRoot;       // Canvas/Chat
    public Chathandler chatHandler;   // ChatPanel에 붙어있는 스크립트

    [Header("GameOver")]
    public GameObject ResultPanel;
    public TextMeshProUGUI ResultText;
    public Button BackToRoomButton;
    public Button GoRoomSearchButton;
    public TextMeshProUGUI GameOverCountdownText; 

    private Coroutine gameOverCountdownCoroutine;

    public bool IsUILinked = false;
    public void LinkLocalPlayerUIElements(GameObject localPlayerRoot)
    {
        // 1. 초기화
        HistoryItems.Clear();
        playerSlotImages.Clear();
        playerSlotTexts.Clear();
        cardButtons.Clear();
        cardTexts.Clear();
        slotRoleNames = new List<string>(InitialSlotRoleNames);


        Transform canvasRoot = localPlayerRoot.transform.Find("Canvas");
        if (canvasRoot == null) return;
        canvasRoot.gameObject.SetActive(true);

        Transform oracleRoot = canvasRoot.Find("Role&OraclePanel");
        Transform persistentRoot = canvasRoot.Find("PersistentOraclePanel");
        Transform systemPanel = canvasRoot.Find("SystemPanel");
        Transform slotPanelRoot = canvasRoot.Find("SlotPanel");

        if (oracleRoot != null)
        {
            oraclePanel = oracleRoot.gameObject;
            oracleText = oracleRoot.Find("oracleText")?.GetComponent<TextMeshProUGUI>();
            roleText = oracleRoot.Find("roleText")?.GetComponent<TextMeshProUGUI>();
            traitorText = oracleRoot.Find("traitorText")?.GetComponent<TextMeshProUGUI>();

            if (oracleText == null) Debug.LogError("❌ UIManager: oracleText (신탁 텍스트) 참조 실패! 경로 확인 필요.");
        }

        if (persistentRoot != null)
        {
            persistentOraclePanel = persistentRoot.gameObject;
            persistentOracleText = persistentRoot.GetComponentInChildren<TextMeshProUGUI>(true);

            if (persistentOracleText == null) Debug.LogError("❌ UIManager: persistentOracleText 참조 실패!");
            else Debug.Log("✔ UIManager: persistentOracleText 참조 성공.");
        }

        if (systemPanel != null)
        {
            Transform sysText = systemPanel.Find("systemText");
            if (sysText != null && GameManager.Instance != null) GameManager.Instance.systemMessageText = sysText.GetComponent<TextMeshProUGUI>();
        }

        Transform judgmentScrollTransform = canvasRoot.Find("JudgmentScroll");
        if (judgmentScrollTransform != null)
        {
            judgmentScroll = judgmentScrollTransform.gameObject;
            judgmentText = judgmentScrollTransform.GetComponentInChildren<TextMeshProUGUI>(true);

            if (judgmentText == null) Debug.LogError("❌ UIManager: judgmentText 참조 실패!");
        }

     
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
            Debug.Log($"✔ HistoryItems Found: {HistoryItems.Count}");
        }

        historyPanel = historyPanelRoot.GetComponent<RectTransform>();

        closedYPosition = historyPanel.anchoredPosition.y;
        isHistoryOpen = false;

        if (historyToggleButton != null)
        {
            historyToggleButton.onClick.RemoveAllListeners();
            historyToggleButton.onClick.AddListener(ToggleHistoryPanel);
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

                if (countdownText == null) Debug.LogError("❌ [DEBUG 7] CountdownText 참조 실패!");
                if (timerCircle == null) Debug.LogError("❌ [DEBUG 7] TimerCircle 참조 실패!");
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
                    if (txt != null)
                    {
                        playerSlotTexts.Add(txt);
                        Debug.Log($"[DEBUG F_6] Slot {i} Text found on object: {txt.gameObject.name} (Parent: {container.name})");
                    }
                    else
                    {
                        Debug.LogError($"❌ UIManager: Slot {i} TextMeshProUGUI not found in {container.name}!");
                    }
                }
            }
            if (playerSlotTexts.Count == 4)
            {
                for (int i = 0; i < 4; i++)
                {
                    playerSlotTexts[i].text = slotRoleNames[i];
                    playerSlotTexts[i].ForceMeshUpdate();
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
                        Debug.Log($"[DEBUG F_7] Card Button Text found on object: {btnText.gameObject.name} (Parent: {btn.name})");
                    }
                    else
                    {
                        Debug.LogError($"❌ UIManager: Card button '{btn.name}' missing TextMeshProUGUI!");
                    }
                }
            }

            Transform chatRootTransform = canvasRoot.Find("ChatPanel");
            if (chatRootTransform != null)
            {
                chatRoot = chatRootTransform.gameObject;
                var chatHandlerComponent = chatRoot.GetComponentInChildren<Chathandler>(true);

                if (chatHandlerComponent != null)
                {
                    chatHandler = chatHandlerComponent;
                    if (ChatManager.Instance != null)
                    {
                        ChatManager.Instance.chathandler = chatHandler;
                        Debug.Log("✔ ChatHandler 연결 완료.");
                    }
                }
                else
                {
                    Debug.LogError("❌ UIManager: ChatPanel 아래에서 Chathandler 컴포넌트를 찾을 수 없습니다.");
                }
            }
            else
            {
                Debug.LogError("❌ UIManager: Canvas 아래에 'ChatPanel'이라는 이름의 오브젝트를 찾을 수 없습니다. 이름 및 계층 구조를 확인하세요.");
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
            else
            {
                Debug.LogError("❌ toggleCardButton 오브젝트에 Button 컴포넌트가 없습니다.");
            }
        }
        else
        {
            Debug.LogError("❌ Canvas 아래에 'toggleCardButton'이라는 이름의 오브젝트를 찾을 수 없습니다. 이름 및 계층 구조를 확인하세요.");
        }


        // 7) GameOver Panel 연결 🌟 [추가]
        Transform gameOverPanelRoot = canvasRoot.Find("GameOverPanel");
        if (gameOverPanelRoot != null)
        {
            ResultPanel = gameOverPanelRoot.gameObject;
            ResultText = gameOverPanelRoot.Find("ResultText")?.GetComponent<TextMeshProUGUI>();
            GameOverCountdownText = gameOverPanelRoot.Find("CountdownText")?.GetComponent<TextMeshProUGUI>();
            BackToRoomButton = gameOverPanelRoot.Find("BackToRoomButton")?.GetComponent<Button>();
            GoRoomSearchButton = gameOverPanelRoot.Find("GoRoomSearchButton")?.GetComponent<Button>();
        }
        
        IsUILinked = true;

        // 🌟 FIX: 게임 시작 직후 모든 UI 비활성화
        if (oraclePanel != null) oraclePanel.SetActive(false);
        if (cardSelectionPanel != null) cardSelectionPanel.SetActive(false);
        if (persistentOraclePanel != null) persistentOraclePanel.SetActive(false);
        if (judgmentScroll != null) judgmentScroll.SetActive(false);
        if (systemPanel != null) systemPanel.gameObject.SetActive(false);

        // 🌟 FIX: 카드 토글 버튼, 히스토리, 채팅도 기본적으로 비활성화
        if (toggleCardButton != null) toggleCardButton.gameObject.SetActive(false);
        if (historyPanel != null) historyPanel.gameObject.SetActive(false);
        if (chatRoot != null) chatRoot.gameObject.SetActive(false);
        if (ResultPanel != null) ResultPanel.SetActive(false);

        // 초기화 시 빈 딕셔너리로 색상 업데이트 (모두 white로 시작)
        UpdateSlotColorsFromRawData(new Dictionary<string, string>());
        Debug.Log($"[DEBUG 8] UIManager UI Link 완료. CardTexts Count: {cardTexts.Count}");


    }

    public void HideTimerUI()
    {
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
        if (timerCircle != null)
            timerCircle.gameObject.SetActive(false);
    }



    // 히스토리 패널 열기/닫기 토글 함수
    public void ToggleHistoryPanel()
    {
        isHistoryOpen = !isHistoryOpen;
        float targetY = isHistoryOpen ? targetOpenedYPosition : closedYPosition;

        StartCoroutine(SlidePanel(targetY));

    }


    private IEnumerator SlidePanel(float targetY)
    {
        if (historyPanel == null) yield break;

        float startTime = Time.time;
        float startY = historyPanel.anchoredPosition.y;
        float distance = targetY - startY;
        float slideDuration = 0.3f;

        while (Time.time < startTime + slideDuration)
        {
            float elapsed = Time.time - startTime;
            float t = elapsed / slideDuration;

            historyPanel.anchoredPosition = new Vector2(historyPanel.anchoredPosition.x, startY + distance * t);
            yield return null;
        }
        historyPanel.anchoredPosition = new Vector2(historyPanel.anchoredPosition.x, targetY);
    }

    
    public void AddHistoryItem(RoundResult msg, int roundNumber, string mission, Dictionary<string, string> slotPlayerColors)
    {
        if (HistoryItems == null || HistoryItems.Count == 0)
        {
            Debug.LogError($"❌ HistoryItem list is invalid or empty.");
            return;
        }

        if (roundNumber < 1)
        {
            Debug.LogError($"❌ Invalid round number: {roundNumber}. Must be 1 or greater.");
            return;
        }

        int targetIndex = roundNumber - 1;

        if (targetIndex >= 0 && targetIndex < HistoryItems.Count)
        {
            HistoryItem historyItem = HistoryItems[targetIndex];

            historyItem.gameObject.SetActive(true);

            historyItem.SetData(msg, slotPlayerColors, roundNumber, mission);
            Debug.Log($"✔ History Item for Round {roundNumber} (Index {targetIndex}) recorded and activated.");
        }
        else
        {
            Debug.LogWarning($"❌ History Item UI for round {roundNumber} (index {targetIndex}) is out of bounds. HistoryItems Count: {HistoryItems.Count}");
        }
    }


    // 신탁 및 역할 공개(역할은 1라운드에만)
    public void ShowOracleAndRole(string oracle, string role, int round)
    {

        if (roleText != null)
        {
            if (round == 1 && !string.IsNullOrEmpty(role))
                roleText.text = role;
        }

        if (oraclePanel != null) oraclePanel.SetActive(true);

        if (oracleText != null)
        {
            oracleText.text = "";
            oracleText.text = oracle;
            oracleText.SetAllDirty();

            Debug.Log($"✔ [Oracle Text] 신탁 값 할당 시도 완료: {oracle}");
        }

        if (persistentOraclePanel != null)
        {
            persistentOraclePanel.SetActive(true); // 🌟 영구 신탁 패널 활성화
            Debug.Log("✔ [Persistent UI] Persistent Oracle Panel 활성화 완료.");
        }

        if (persistentOracleText != null)
        {
            persistentOracleText.text = $"{oracle}";
        }

        Canvas.ForceUpdateCanvases();
        // 🌟 5초 후 oraclePanel만 숨김
        StartCoroutine(HideOraclePanelAfterSeconds(5.0f));
    }

    private IEnumerator HideOraclePanelAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (oraclePanel != null)
        {
            oraclePanel.SetActive(false);
            if (traitorText != null) traitorText.gameObject.SetActive(false);
        }

    }

    public void ShowTraitorInfo(string godPersonality)
    {
        if (traitorText != null)
        {
            traitorText.text = $"신의 페르소나: {godPersonality}";
            traitorText.gameObject.SetActive(true);
        }
    }


    public void SetupCardButtons(List<string> cards)
    {
        if (cardSelectionPanel == null)
        {
            Debug.LogError("❌ [DEBUG 9] cardSelectionPanel이 null입니다. 카드 UI 활성화 실패.");
            return;
        }
        // cardSelectionPanel은 StartCardSelection 코루틴 시작 시 켜짐.
        cardSelectionPanel.SetActive(true);

        Image panelImage = cardSelectionPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.raycastTarget = false;
        }

        string playerRole = GameManager.Instance.mySlot;
        string playerSlotId = GetSlotIdFromRole(playerRole);

        for (int i = 0; i < cardButtons.Count; i++)
        {
            Button button = cardButtons[i];
            TextMeshProUGUI textComponent = cardTexts[i];

            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.raycastTarget = true;
            }

            button.interactable = true;

            if (i < cards.Count)
            {
                string cardWord = cards[i];
                if (textComponent != null)
                {
                    Debug.Log($"[DEBUG 10A] Card Button {i}에 값 할당 시도: '{cardWord}'");

                    textComponent.enableAutoSizing = false;
                    textComponent.fontSize = 24;

                    textComponent.text = cardWord;
                    textComponent.enabled = false;
                    textComponent.enabled = true;
                    textComponent.ForceMeshUpdate();
                }
                else
                {
                    Debug.LogError($"❌ Card Text component is missing for button index {i}.");
                }

                button.onClick.RemoveAllListeners();
                string capturedCardWord = cards[i];
                button.onClick.AddListener(() => GameManager.Instance.OnCardSelected(capturedCardWord));
            }
            else
            {
                if (textComponent != null) textComponent.text = "";
                button.onClick.RemoveAllListeners();
            }

            button.gameObject.SetActive(true);

            CardHoverHandler hoverHandler = button.GetComponent<CardHoverHandler>();
            if (hoverHandler == null)
            {
                hoverHandler = button.gameObject.AddComponent<CardHoverHandler>();
            }

            if (hoverHandler != null)
            {
                hoverHandler.targetSlotId = playerSlotId;

                hoverHandler.Initialize(this, textComponent);

                hoverHandler.enabled = false;
                hoverHandler.enabled = true;
                Debug.Log($"[DEBUG 11] 호버 핸들러 초기화 완료. SlotId: {hoverHandler.targetSlotId}");
            }
        }
        Canvas.ForceUpdateCanvases();
        Debug.Log("[DEBUG 12] SetupCardButtons 실행 완료 및 Canvas 강제 갱신.");

        StartCoroutine(PostSetupTextUpdate());
    }

    private IEnumerator PostSetupTextUpdate()
    {
        yield return null;

        foreach (var textComp in cardTexts)
        {
            if (textComp != null)
            {
                textComp.ForceMeshUpdate();
            }
        }
        Canvas.ForceUpdateCanvases();
        Debug.Log("✔ 렌더링 후속 갱신 완료 (텍스트/호버 반영 기대).");
    }

    // 🌟 FIX: mySlot이 대소문자가 섞여있을 경우를 대비하여 OrdinalIgnoreCase 사용
    public string GetSlotIdFromRole(string roleName)
    {
        for (int i = 0; i < SlotVisualOrder.Length; i++)
        {
            // 🌟 대소문자 무시 비교 (OrdinalIgnoreCase)
            if (SlotVisualOrder[i].Equals(roleName, StringComparison.OrdinalIgnoreCase))
            {
                return $"slot{i + 1}";
            }
        }
        Debug.LogError($"❌ 역할 '{roleName}'이 SlotVisualOrder에 없습니다. 기본값 slot1로 처리.");
        return "slot1";
    }

    // 🌟 FIX: 라운드 시작 시 슬롯 텍스트를 초기화하는 함수 추가
    public void ResetSentenceSlots()
    {
        // 🌟 저장된 slotRoleNames를 초기 상태로 리셋
        slotRoleNames = new List<string>(InitialSlotRoleNames);

        for (int i = 0; i < playerSlotTexts.Count; i++)
        {
            if (i < InitialSlotRoleNames.Count)
            {
                playerSlotTexts[i].text = InitialSlotRoleNames[i];
                playerSlotTexts[i].ForceMeshUpdate();
            }
        }
        Debug.Log("✔ 문장 슬롯 텍스트 초기화 완료.");
    }


    public void ToggleCardPanel()
    {
        if (cardSelectionPanel != null)
        {
            bool isActive = cardSelectionPanel.activeSelf;
            cardSelectionPanel.SetActive(!isActive);
            Debug.Log($"[UI] 카드 패널 활성화 상태 토글: {!isActive}");
        }
    }

    public void UpdateSlotColorsFromRawData(Dictionary<string, string> slotRoleColors)
    {
        for (int i = 0; i < playerSlotImages.Count; i++)
        {
            if (i >= SlotVisualOrder.Length) continue;
            string slotRoleName = SlotVisualOrder[i];

            string colorName = "white";

            if (slotRoleColors != null && slotRoleColors.ContainsKey(slotRoleName))
            {
                colorName = slotRoleColors[slotRoleName];
            }

            Debug.Log($"[Raw Slot Color Debug] Slot {slotRoleName} assigned color: {colorName}");
            playerSlotImages[i].color = GetUnityColor(colorName);
        }
        Debug.Log($"[DEBUG 13] 슬롯 색상 업데이트 감지 완료 (Raw Data).");
    }

    // 기존 UpdateSlotColorsFromPlayers()는 RoundResult 기록 등 로컬 PlayerManager 데이터 기반 필요 시 사용
    public void UpdateSlotColorsFromPlayers()
    {
        if (GameManager.Instance == null) return;

        var players = GameManager.Instance.GetPlayers();
        Dictionary<string, string> slotRoleColors = new Dictionary<string, string>();

        foreach (var playerEntry in players)
        {
            PlayerManager pm = playerEntry.Value;
            if (!string.IsNullOrEmpty(pm.slot) && !string.IsNullOrEmpty(pm.colorName))
            {
                slotRoleColors[pm.slot] = pm.colorName;
                Debug.Log($"[Slot Color Prep] Player {pm.playerId} has Slot: {pm.slot}, Color: {pm.colorName}");
            }
            else
            {
                Debug.Log($"[Slot Color Prep] Skipping player {pm.playerId}: Slot={pm.slot}, Color={pm.colorName}");
            }
        }

        // 기존 UpdateSlotColorsInternal 로직을 RawData 함수로 대체
        UpdateSlotColorsFromRawData(slotRoleColors);
    }


    private Color GetUnityColor(string colorName)
    {
        // colorName이 null이거나 비어있으면 switch 문에서 default로 떨어져 white가 반환됨
        if (colorName == null) colorName = "unknown";

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
                Debug.LogWarning($"Unknown color name: {colorName}. Defaulting to white.");
                return Color.white;
        }
    }

    public void DisableMyCards()
    {
        foreach (var btn in cardButtons) btn.GetComponent<Button>().interactable = false;

        if (cardSelectionPanel != null)
        {
            cardSelectionPanel.SetActive(false);
        }
    }

    public void AutoSelectRandomCard()
    {
        if (GameManager.Instance.cardSelectedCompleted)
        {
            Debug.Log("[UI] 카드가 이미 선택되었습니다. 자동 선택 로직 건너뜀.");
            return;
        }

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

            if (index >= 0 && index < playerSlotImages.Count)
            {
                TextMeshProUGUI slotText = playerSlotTexts[index];

                if (highlight)
                {
                    if (slotText != null)
                    {
                        slotText.color = Color.black;
                        slotText.text = hoveredWord;
                        slotText.ForceMeshUpdate();
                    }

                }
                else
                {
                    if (slotText != null && index < slotRoleNames.Count)
                    {
                        slotText.text = slotRoleNames[index];
                        slotText.ForceMeshUpdate();
                    }
                }
            }
        }
    }


    public void UpdateMySentenceSlot(string slotId, string selectedWord)
    {
        if (slotId.StartsWith("slot") && int.TryParse(slotId.Substring(4), out int slotIndex))
        {
            int index = slotIndex - 1;

            if (index >= 0 && index < playerSlotTexts.Count)
            {
                playerSlotTexts[index].text = selectedWord;
                playerSlotTexts[index].ForceMeshUpdate();

                if (index < slotRoleNames.Count)
                {
                    // 🌟 선택된 단어를 slotRoleNames에 반영하여 HighlightSlot에서 해당 단어가 기본으로 유지되도록 함
                    slotRoleNames[index] = selectedWord;
                    Debug.Log($"[UI Fix] Slot {slotId} permanent text updated to: {selectedWord}");
                }
            }
        }
    }

    public void DisplaySentence(string sentence)
    {
        if (judgmentText != null)
        {
            string resultMessage = $"-완성된 문장-\n\n";
            resultMessage += $"{sentence}";
            judgmentText.text = resultMessage;
            judgmentText.ForceMeshUpdate();
            StartCoroutine(VerifyAndMaintainText(judgmentText, resultMessage, 2));
        }
    }

    public void DisplayJudgmentReason(string reason)
    {
        if (judgmentText != null)
        {
            string resultMessage = $"{reason}";
            judgmentText.text = resultMessage;
            judgmentText.ForceMeshUpdate();
            StartCoroutine(VerifyAndMaintainText(judgmentText, resultMessage, 2));
        }
    }


    private IEnumerator VerifyAndMaintainText(TextMeshProUGUI textComponent, string expectedText, int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            yield return null;

            if (textComponent.text != expectedText)
            {
                textComponent.text = expectedText;
                textComponent.ForceMeshUpdate();
                Debug.LogWarning($"[CRITICAL FIX] Text was overwritten! Reverting to expected text. Frame: {i + 1}");
            }
        }
    }

    public void ShowSystemMessage(string message)
    {
        if (GameManager.Instance != null && GameManager.Instance.systemMessageText != null)
        {
            GameManager.Instance.systemMessageText.text = message;
        }
    }


    public void SetGameUIActive(bool isActive)
    {
        bool isGameUIActive = isActive;

        if (persistentOraclePanel != null)
        {
            persistentOraclePanel.SetActive(isGameUIActive);
        }
        if (historyPanel != null)
        {
            historyPanel.gameObject.SetActive(isGameUIActive);
        }
        if (historyToggleButton != null)
        {
            historyToggleButton.gameObject.SetActive(isGameUIActive);
        }
        if (chatRoot != null)
        {
            chatRoot.gameObject.SetActive(isGameUIActive);
        }

        if (toggleCardButton != null)
        {
            toggleCardButton.gameObject.SetActive(isGameUIActive);
        }
    }

    public void PlayVisualCue(VisualCue cue)
    {
        if (lightningEffect != null) lightningEffect.SetActive(false);
        if (flowerEffect != null) flowerEffect.SetActive(false);

        if (cue.effect == "LIGHTNING" && lightningEffect != null)
        {
            StartCoroutine(ActivateAndDeactivateEffect(lightningEffect, 3.0f));
            Debug.Log("⚡ 번개 이펙트 재생 시작.");
        }
        else if (cue.effect == "FLOWER" && flowerEffect != null)
        {
            StartCoroutine(ActivateAndDeactivateEffect(flowerEffect, 3.0f));
            Debug.Log("🌸 꽃잎 이펙트 재생 시작.");
        }
        else
        {
            Debug.LogWarning($"VisualCue Effect '{cue.effect}'에 해당하는 오브젝트가 없거나 연결되지 않았습니다.");
        }
    }

    private IEnumerator ActivateAndDeactivateEffect(GameObject effectObject, float duration)
    {
        if (effectObject != null)
        {
            effectObject.SetActive(true);
            yield return new WaitForSeconds(duration);
            effectObject.SetActive(false);
        }
    }

    public IEnumerator StartTimer(float totalTime, Action onTimerEnd)
    {
        float timer = totalTime;

        if (countdownText != null) countdownText.gameObject.SetActive(true);
        if (timerCircle != null) timerCircle.gameObject.SetActive(true);

        if (countdownText == null || timerCircle == null)
        {
            Debug.LogError("❌ StartTimer: 필수 UI 컴포넌트(Text 또는 Circle)가 연결되지 않아 타이머를 시작할 수 없습니다.");
            yield break;
        }

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

    public void ShowGameOverResult(string resultMsg, Action onBackToRoom, Action onGoRoomSearch)
    {
        if (ResultPanel == null)
        {
            Debug.LogError("❌ GameOver Panel UI가 연결되지 않았습니다.");
            return;
        }

        // 게임 UI 비활성화 (기존 게임 요소 숨김)
        SetGameUIActive(false);

        // Timer UI도 숨김 (기존 라운드 타이머)
        HideTimerUI();

        // 결과 패널 활성화 및 텍스트 설정
        ResultText.text = resultMsg;
        ResultPanel.SetActive(true);

        // 버튼 리스너 초기화 및 추가
        BackToRoomButton.onClick.RemoveAllListeners();
        GoRoomSearchButton.onClick.RemoveAllListeners();

        // 1. 방으로 돌아가기 버튼 리스너
        BackToRoomButton.onClick.AddListener(() =>
        {
            StopGameOverCountdown();
            onBackToRoom?.Invoke(); // GameManager의 SendBackToRoomAction 호출
            LockResultButtons();
        });

        // 2. 룸서치로 이동 버튼 리스너
        GoRoomSearchButton.onClick.AddListener(() =>
        {
            StopGameOverCountdown();
            onGoRoomSearch?.Invoke(); // GameManager의 GoToRoomSearchScene 호출
            LockResultButtons();
        });

        // 카운트다운 시작
        StartGameOverCountdown(10, onGoRoomSearch);
    }

    // 카운트다운 타이머를 시작하는 코루틴
    public void StartGameOverCountdown(int seconds, Action onTimerEnd)
    {
        if (gameOverCountdownCoroutine != null) StopCoroutine(gameOverCountdownCoroutine);
        gameOverCountdownCoroutine = StartCoroutine(GameOverCountdownRoutine(seconds, onTimerEnd));
    }

    private IEnumerator GameOverCountdownRoutine(int remaining, Action onTimerEnd)
    {
        while (remaining >= 0)
        {
            if (GameOverCountdownText != null)
                GameOverCountdownText.text = $"{remaining}";

            yield return new WaitForSeconds(1f);
            remaining--;
        }

        // 타이머 종료 시 자동 씬 전환
        onTimerEnd?.Invoke();
        LockResultButtons();
    }

    // 카운트다운을 중지하는 함수
    public void StopGameOverCountdown()
    {
        if (gameOverCountdownCoroutine != null)
        {
            StopCoroutine(gameOverCountdownCoroutine);
            gameOverCountdownCoroutine = null;
        }
    }

    // 버튼 상호작용을 비활성화하는 함수
    public void LockResultButtons()
    {
        if (BackToRoomButton != null) BackToRoomButton.interactable = false;
        if (GoRoomSearchButton != null) GoRoomSearchButton.interactable = false;
    }
}