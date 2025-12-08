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

    // 🌟 UI에 표시되는 순서에 따라 서버에서 받은 슬롯 역할 이름 정의 (SUBJECT, TARGET, HOW, ACTION 순서 가정)
    private readonly string[] SlotVisualOrder = { "SUBJECT", "TARGET", "HOW", "ACTION" };

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
    public List<string> slotRoleNames = new List<string> { "주체", "대상", "어떻게", "어쩐다" }; // 🌟🌟🌟 역할 텍스트 저장 🌟🌟🌟

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

        // --- A. 단일 컴포넌트 할당 ---
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

            if (oracleText == null) Debug.LogError("❌ UIManager: oracleText (신탁 텍스트) 참조 실패! 경로 확인 필요.");
        }

        // 2) Persistent Oracle
        if (persistentRoot != null)
        {
            persistentOraclePanel = persistentRoot.gameObject;
            persistentOracleText = persistentRoot.GetComponentInChildren<TextMeshProUGUI>(true);

            if (persistentOracleText == null) Debug.LogError("❌ UIManager: persistentOracleText 참조 실패!");
            else Debug.Log("✔ UIManager: persistentOracleText 참조 성공.");
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

            if (judgmentText == null) Debug.LogError("❌ UIManager: judgmentText 참조 실패!");
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
            // 🌟🌟🌟 초기 슬롯 텍스트 복원 (버튼 아래 텍스트가 안 보일 경우를 대비)
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
        }

        // 🌟 카드 토글 버튼 연결 및 리스너 추가
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

        IsUILinked = true;

        // 게임 시작 직후 UI 비활성화
        if (oraclePanel != null) oraclePanel.SetActive(false);
        if (cardSelectionPanel != null) cardSelectionPanel.SetActive(false);
        if (persistentOraclePanel != null) persistentOraclePanel.SetActive(false);
        if (judgmentScroll != null) judgmentScroll.SetActive(false);

        // UI 연결 직후 슬롯 색상을 기본값(그린)으로 초기화
        UpdateSlotColorsInternal(new Dictionary<string, string>());
        Debug.Log($"[DEBUG 8] UIManager UI Link 완료. CardTexts Count: {cardTexts.Count}");
    }

    // 🌟🌟🌟 [추가] 타이머 UI를 명시적으로 숨기는 함수 🌟🌟🌟
    public void HideTimerUI()
    {
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
        if (timerCircle != null)
            timerCircle.gameObject.SetActive(false);
    }
    // -----------------------------------------------------------


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

    // 히스토리 아이템 추가
    public void AddHistoryItem(RoundResult msg, int roundNumber, string mission, Dictionary<string, string> slotPlayerColors, List<string> finalWords)
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
            historyItem.SetData(msg, slotPlayerColors, roundNumber, mission, finalWords);
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
            persistentOraclePanel.SetActive(true);
            Debug.Log("✔ [Persistent UI] Persistent Oracle Panel 활성화 완료.");
        }

        if (persistentOracleText != null)
        {
            persistentOracleText.text = $"{oracle}";
        }

        Canvas.ForceUpdateCanvases();

        // 🌟🌟🌟 시간 조정: 신탁 표시 시간 증가 (3s -> 5s) 🌟🌟🌟
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
        cardSelectionPanel.SetActive(true);

        Image panelImage = cardSelectionPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.raycastTarget = false;
        }

        // 🌟🌟🌟 CRITICAL FIX: 플레이어의 역할 ID를 "slotX" 형식으로 가져옵니다. 🌟🌟🌟
        string playerRole = GameManager.Instance.mySlot;
        string playerSlotId = GetSlotIdFromRole(playerRole);
        // --------------------------------------------------------------------------

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

            // 🌟🌟🌟 CardHoverHandler 활성화 및 수정된 slotId 할당 🌟🌟🌟
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

    // UIManager.cs - 새로운 코루틴 추가
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

    // 🌟 역할 이름(SUBJECT)을 슬롯 ID(slot1)로 변환하는 핵심 함수
    public string GetSlotIdFromRole(string roleName)
    {
        for (int i = 0; i < SlotVisualOrder.Length; i++)
        {
            if (SlotVisualOrder[i].Equals(roleName, StringComparison.OrdinalIgnoreCase))
            {
                return $"slot{i + 1}";
            }
        }
        Debug.LogError($"❌ 역할 '{roleName}'이 SlotVisualOrder에 없습니다. 기본값 slot1로 처리.");
        return "slot1";
    }


    // 🌟 새로 추가된 카드 패널 토글 함수
    public void ToggleCardPanel()
    {
        if (cardSelectionPanel != null)
        {
            bool isActive = cardSelectionPanel.activeSelf;
            cardSelectionPanel.SetActive(!isActive);
            Debug.Log($"[UI] 카드 패널 활성화 상태 토글: {!isActive}");
        }
    }

    // 🌟 GameManager의 PlayerManager 데이터를 기반으로 슬롯 색상을 업데이트하는 공개 함수
    public void UpdateSlotColorsFromPlayers()
    {
        var players = GameManager.Instance.GetPlayers();
        Dictionary<string, string> slotRoleColors = new Dictionary<string, string>();

        foreach (var playerEntry in players)
        {
            PlayerManager pm = playerEntry.Value;
            if (!string.IsNullOrEmpty(pm.slot) && !string.IsNullOrEmpty(pm.colorName))
            {
                slotRoleColors[pm.slot] = pm.colorName;
            }
        }

        UpdateSlotColorsInternal(slotRoleColors);
    }

    // 🌟 슬롯 테두리 색상을 업데이트하는 내부 함수
    private void UpdateSlotColorsInternal(Dictionary<string, string> slotPlayerColors)
    {
        for (int i = 0; i < playerSlotImages.Count; i++)
        {
            if (i >= SlotVisualOrder.Length) continue;
            string slotRoleName = SlotVisualOrder[i];

            string colorName = "white";

            if (slotPlayerColors != null && slotPlayerColors.ContainsKey(slotRoleName))
            {
                colorName = slotPlayerColors[slotRoleName];
            }

            Debug.Log($"[Slot Color Debug] Slot {slotRoleName} assigned color: {colorName}");
            Debug.Log($"[Color Conversion] Attempting to convert color: {colorName}");

            playerSlotImages[i].color = GetUnityColor(colorName);
        }
        Debug.Log($"[DEBUG 13] 슬롯 색상 업데이트 감지 완료.");
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
                Debug.LogWarning($"Unknown color name: {colorName}. Defaulting to green.");
                return Color.green;
        }
    }

    // 카드 선택 완료 시 버튼 비활성화 
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

    // 🌟🌟🌟 수정된 HighlightSlot: 텍스트 복구 로직 적용 🌟🌟🌟
    public void HighlightSlot(string slotId, bool highlight, string hoveredWord)
    {
        if (slotId.StartsWith("slot") && int.TryParse(slotId.Substring(4), out int slotIndex))
        {
            int index = slotIndex - 1;

            if (index >= 0 && index < playerSlotImages.Count)
            {
                Image slotImage = playerSlotImages[index];
                TextMeshProUGUI slotText = playerSlotTexts[index];


                Debug.Log($"[DEBUG 15/F_5] SlotId: {slotId}, Index: {index}, Action: {(highlight ? "Show" : "Hide")}, Word: '{hoveredWord}', CurrentColor: {slotImage.color}");


                if (highlight)
                {
                    // 텍스트는 명확하게 보이도록 검은색으로 설정
                    if (slotText != null)
                    {
                        slotText.color = Color.black;
                        slotText.text = hoveredWord;
                        slotText.ForceMeshUpdate();
                    }

                }
                else
                {
                    // ❌❌❌ CRITICAL FIX: 호버 해제 시 원래의 역할 텍스트로 복원합니다. ❌❌❌
                    if (slotText != null && index < slotRoleNames.Count)
                    {
                        // 원래의 역할 텍스트 ("주체", "대상" 등)로 복원합니다.
                        slotText.text = slotRoleNames[index];
                        slotText.ForceMeshUpdate();
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning($"❌ HighlightSlot 호출 실패: ID 형식이 잘못되었습니다. 수신된 ID: '{slotId}'");
        }
    }


    public void UpdateMySentenceSlot(string slotId, string selectedWord)
    {
        if (slotId.StartsWith("slot") && int.TryParse(slotId.Substring(4), out int slotIndex))
        {
            int index = slotIndex - 1;

            if (index >= 0 && index < playerSlotTexts.Count)
            {
                // 🌟🌟🌟 텍스트를 영구적으로 업데이트 🌟🌟🌟
                playerSlotTexts[index].text = selectedWord;
                playerSlotTexts[index].ForceMeshUpdate();

                // 🌟🌟🌟 CRITICAL FIX: 호버 복원용 리스트도 업데이트하여 영구 고정 🌟🌟🌟
                if (index < slotRoleNames.Count)
                {
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
            string resultMessage = $"--- 완성된 문장 ---\n\n";
            resultMessage += $"{sentence}";

            // 🌟🌟🌟 FIX: 텍스트 할당 로직을 안전하게 감싸서 값 유지 보장 🌟🌟🌟
            // (1) 일단 할당
            judgmentText.text = resultMessage;
            judgmentText.ForceMeshUpdate();

            // (2) 다음 프레임 또는 다음 렌더링 루프에서 값이 지워지는 것을 방지하기 위해 강제 재할당
            StartCoroutine(VerifyAndMaintainText(judgmentText, resultMessage, 2)); // 2프레임 확인

            Debug.Log($"[DEBUG F_3] Judgment Sentence assigned: {sentence}. Length: {resultMessage.Length}");
        }
        else
        {
            Debug.LogError("❌ JudgmentText is null during DisplaySentence.");
        }
    }

    public void DisplayJudgmentReason(string reason)
    {
        if (judgmentText != null)
        {
            string resultMessage = $"{reason}";

            judgmentText.text = resultMessage;
            judgmentText.ForceMeshUpdate();

            StartCoroutine(VerifyAndMaintainText(judgmentText, resultMessage, 2)); // 2프레임 확인

            Debug.Log($"[DEBUG F_4] Judgment Reason assigned: {reason}. Length: {resultMessage.Length}");
        }
        else
        {
            Debug.LogError("❌ JudgmentText is null during DisplayJudgmentReason.");
        }
    }


    // 🌟🌟🌟 UIManager.cs - 새로운 텍스트 유지 코루틴 추가 🌟🌟🌟
    private IEnumerator VerifyAndMaintainText(TextMeshProUGUI textComponent, string expectedText, int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            yield return null; // 1프레임 대기

            if (textComponent.text != expectedText)
            {
                // 값이 지워졌다면 다시 할당하고 렌더링 강제
                textComponent.text = expectedText;
                textComponent.ForceMeshUpdate();
                Debug.LogWarning($"[CRITICAL FIX] Text was overwritten! Reverting to expected text. Frame: {i + 1}");
            }
        }
    }

    // 🌟🌟🌟 ShowSystemMessage 함수 추가 🌟🌟🌟
    public void ShowSystemMessage(string message)
    {
        if (GameManager.Instance != null && GameManager.Instance.systemMessageText != null)
        {
            GameManager.Instance.systemMessageText.text = message;
        }
    }

    // 누락된 함수들 (SetGameUIActive, PlayVisualCue, StartTimer)
    public void SetGameUIActive(bool isActive)
    {
        if (persistentOraclePanel != null)
        {
            persistentOraclePanel.SetActive(isActive);
        }
        if (historyPanel != null)
        {
            historyPanel.gameObject.SetActive(isActive);
        }
        if (historyToggleButton != null)
        {
            historyToggleButton.gameObject.SetActive(isActive);
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
}