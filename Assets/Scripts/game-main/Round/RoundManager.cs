using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance;

    public int currentRound = 0;
    private string currentMission = "";

    // 🌟 추가: 카드 선택 타이머 코루틴을 저장할 변수
    private Coroutine cardSelectionTimerCoroutine;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // 🌟🌟🌟 서버로부터 RECEIVE_CARDS 이벤트를 받았을 때 호출됩니다.
    public void HandleReceiveCards(ReceiveCardsMessage msg)
    {
        // 🌟🌟🌟 클라이언트에서 라운드 번호 증가
        currentRound++;
        Debug.Log($"[RoundManager] New Round Started (Cards Received): Round {currentRound}");

        // currentMission은 GameManager에 저장된 currentOracle을 사용
        currentMission = GameManager.Instance.currentOracle;

        // 🌟🌟🌟 FIX: 이전 라운드에 선택된 단어 슬롯 초기화 🌟🌟🌟
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ResetSentenceSlots();
        }

        // 🌟 mySlot 업데이트
        GameManager.Instance.mySlot = msg.data.slotType;

        // 🌟🌟🌟 상태 초기화
        if (GameManager.Instance != null)
        {
            GameManager.Instance.cardSelectedCompleted = false;
        }

        // 🌟🌟🌟 (1) 이전 타이머가 있다면 취소
        if (cardSelectionTimerCoroutine != null)
        {
            StopCoroutine(cardSelectionTimerCoroutine);
            cardSelectionTimerCoroutine = null;
        }
        UIManager.Instance.HideTimerUI();

        // 🌟🌟🌟 (2) 카드 리스트 확인
        if (msg.data.cards != null)
        {
            Debug.Log($"[RoundManager] Received Cards Count: {msg.data.cards.Count}");
            Debug.Log($"[RoundManager] First Card: {(msg.data.cards.Count > 0 ? msg.data.cards[0] : "N/A")}");
        }
        else
        {
            Debug.LogWarning("[RoundManager] Received Cards list is NULL!");
        }

        // 🌟🌟🌟 (3) 타이머 시작 (서버 요청에 따라 120초로 고정)
        int selectionTime = 120;

        if (this.isActiveAndEnabled)
        {
            StartCoroutine(StartCardSelection(msg.data.cards, selectionTime));
        }
        else
        {
            Debug.LogError("❌ FATAL: RoundManager가 코루틴을 시작할 수 없는 상태입니다.");
        }
    }


    // 카드 선택 시작
    private IEnumerator StartCardSelection(List<string> cards, int selectionTime)
    {
        if (cardSelectionTimerCoroutine != null)
        {
            StopCoroutine(cardSelectionTimerCoroutine);
            cardSelectionTimerCoroutine = null;
        }
        UIManager.Instance.HideTimerUI();

        // UI 활성화 이전에 1프레임 대기하여 모든 UI 컴포넌트가 활성화되도록 보장
        yield return null;

        // 🌟🌟🌟 FIX: 카드 선택 관련 UI 활성화 🌟🌟🌟
        if (UIManager.Instance != null)
        {
            if (UIManager.Instance.toggleCardButton != null)
                UIManager.Instance.toggleCardButton.gameObject.SetActive(true); // 카드 토글 버튼 ON
            if (UIManager.Instance.historyPanel != null)
                UIManager.Instance.historyPanel.gameObject.SetActive(true); // 히스토리 패널 ON
            if (UIManager.Instance.chatRoot != null)
                UIManager.Instance.chatRoot.gameObject.SetActive(true); // 채팅 ON
        }

        // 카드 선택 UI 활성화 (SetupCardButtons 내부에서 cardSelectionPanel이 true가 됨)
        UIManager.Instance.SetupCardButtons(cards);
        Debug.Log($"[DEBUG 5] SetupCardButtons 호출 완료. Cards Count: {cards?.Count ?? 0}");

        cardSelectionTimerCoroutine = StartCoroutine(
            UIManager.Instance.StartTimer(selectionTime, () =>
            {
                UIManager.Instance.AutoSelectRandomCard();
                cardSelectionTimerCoroutine = null; // 자동 선택 완료 후 참조 해제
            })
        );
        Debug.Log($"[DEBUG 6] UIManager.StartTimer 호출 완료. Time: {selectionTime}");
    }

    // 카드 선택 완료(개인) - 서버로부터 CARD_SELECTION_CONFIRMED 수신 시 호출
    public void HandleCardSelectionConfirmed()
    {
        // 🌟🌟🌟 (3) 플레이어 수동 선택 시 타이머 취소 🌟🌟🌟
        if (cardSelectionTimerCoroutine != null)
        {
            StopCoroutine(cardSelectionTimerCoroutine);
            cardSelectionTimerCoroutine = null;
        }

        // 🌟🌟🌟 (4) UI에서도 타이머 숨기기 🌟🌟🌟
        UIManager.Instance.HideTimerUI();

        // 카드 선택 UI 비활성화
        UIManager.Instance.DisableMyCards();
        UIManager.Instance.ShowSystemMessage("카드 선택이 확인되었습니다.");
    }

    // 다른 플레이어 행동 완료 업데이트
    public void HandlePlayerActionUpdate(PlayerActionUpdate msg)
    {
        UIManager.Instance.ShowSystemMessage($"{msg.playerId}가 행동을 완료했습니다.");
    }

    // 카드 선택 완료(전체) - 서버로부터 ALL_CARDS_SELECTED 수신 시 호출
    public void HandleInterpretationEnd(InterpretationEnd msg)
    {
        UIManager.Instance.ShowSystemMessage(msg.message);
    }

    // 라운드 종료 - 서버로부터 ROUND_RESULT 수신 시 호출
    public void HandleRoundResult(RoundResult msg)
    {
        int roundNumberToRecord = currentRound;

        UIManager.Instance.ShowSystemMessage($"신의 심판: {(string.IsNullOrEmpty(msg.fullSentence) ? msg.sentence : msg.fullSentence)} (Score {msg.score})");

        GameManager.Instance.StartJudgmentSequence(msg);

        GameManager.Instance.UpdateVillageHP(msg.score);

        Dictionary<string, string> currentSlotColors = new Dictionary<string, string>();

        if (msg.players != null)
        {
            foreach (var player in msg.players)
            {
                if (!string.IsNullOrEmpty(player.slot) && !string.IsNullOrEmpty(player.color))
                {
                    currentSlotColors[player.slot] = player.color;
                    Debug.Log($"[History Color FIX] Slot {player.slot} mapped to Color {player.color} for player {player.nickname}");
                }
            }
        }
        else
        {
            foreach (var playerEntry in GameManager.Instance.GetPlayers())
            {
                PlayerManager pm = playerEntry.Value;
                if (!string.IsNullOrEmpty(pm.slot) && !string.IsNullOrEmpty(pm.colorName))
                {
                    currentSlotColors[pm.slot] = pm.colorName;
                }
            }
            Debug.LogWarning("[History Color FIX] msg.players가 null이어서 로컬 PlayerManager 데이터를 사용했습니다.");
        }


        // 히스토리 패널에 기록
        UIManager.Instance.AddHistoryItem(
           msg,
           roundNumberToRecord,
           currentMission,
           currentSlotColors
        );
    }
}