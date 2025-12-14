using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance;

    public int currentRound = 0;
    private string currentMission = "";

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

    public void HandleReceiveCards(ReceiveCardsMessage msg)
    {
        currentRound++;
        Debug.Log($"[RoundManager] New Round Started (Cards Received): Round {currentRound}");

        currentMission = GameManager.Instance.currentOracle;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ResetSentenceSlots();
        }

        GameManager.Instance.mySlot = msg.data.slotType;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.cardSelectedCompleted = false;
        }

        if (cardSelectionTimerCoroutine != null)
        {
            StopCoroutine(cardSelectionTimerCoroutine);
            cardSelectionTimerCoroutine = null;
        }
        UIManager.Instance.HideTimerUI();

        if (msg.data.cards != null)
        {
            Debug.Log($"[RoundManager] Received Cards Count: {msg.data.cards.Count}");
            Debug.Log($"[RoundManager] First Card: {(msg.data.cards.Count > 0 ? msg.data.cards[0] : "N/A")}");
        }
        else
        {
            Debug.LogWarning("[RoundManager] Received Cards list is NULL!");
        }

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


    private IEnumerator StartCardSelection(List<string> cards, int selectionTime)
    {
        if (cardSelectionTimerCoroutine != null)
        {
            StopCoroutine(cardSelectionTimerCoroutine);
            cardSelectionTimerCoroutine = null;
        }
        UIManager.Instance.HideTimerUI();

        yield return null;

        if (UIManager.Instance != null)
        {
            if (UIManager.Instance.toggleCardButton != null)
                UIManager.Instance.toggleCardButton.gameObject.SetActive(true); 
            if (UIManager.Instance.historyPanel != null)
                UIManager.Instance.historyPanel.gameObject.SetActive(true); 
            if (UIManager.Instance.chatRoot != null)
                UIManager.Instance.chatRoot.gameObject.SetActive(true);
        }

        UIManager.Instance.SetupCardButtons(cards);
        Debug.Log($"[DEBUG 5] SetupCardButtons 호출 완료. Cards Count: {cards?.Count ?? 0}");

        cardSelectionTimerCoroutine = StartCoroutine(
            UIManager.Instance.StartTimer(selectionTime, () =>
            {
                UIManager.Instance.AutoSelectRandomCard();
                cardSelectionTimerCoroutine = null; 
            })
        );
        Debug.Log($"[DEBUG 6] UIManager.StartTimer 호출 완료. Time: {selectionTime}");
    }

    public void HandleCardSelectionConfirmed()
    {
        if (cardSelectionTimerCoroutine != null)
        {
            StopCoroutine(cardSelectionTimerCoroutine);
            cardSelectionTimerCoroutine = null;
        }

        UIManager.Instance.HideTimerUI();

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