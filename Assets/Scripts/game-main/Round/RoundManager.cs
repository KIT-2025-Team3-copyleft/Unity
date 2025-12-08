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

    // 라운드 시작 (카드리스트 및 타이머 정보 수신)
    public void HandleRoundStart(RoundStartMessage msg)
    {
        currentRound = msg.currentRound;

        currentMission = msg.mission;

        // 🌟 mySlot 업데이트 (첫 라운드 및 후속 라운드 모두 여기서 할당됨)
        GameManager.Instance.mySlot = msg.mySlot;

        // 🌟🌟🌟 상태 초기화
        if (GameManager.Instance != null)
        {
            GameManager.Instance.cardSelectedCompleted = false;
        }

        // 🌟🌟🌟 (1) 이전 타이머가 있다면 취소 (다음 라운드로 넘어갈 때) 🌟🌟🌟
        if (cardSelectionTimerCoroutine != null)
        {
            StopCoroutine(cardSelectionTimerCoroutine);
            cardSelectionTimerCoroutine = null;
        }
        UIManager.Instance.HideTimerUI();

        if (msg.cards != null)
        {
            Debug.Log($"[RoundManager] Received Cards Count: {msg.cards.Count}");
            Debug.Log($"[RoundManager] First Card: {(msg.cards.Count > 0 ? msg.cards[0] : "N/A")}");
        }
        else
        {
            Debug.LogWarning("[RoundManager] Received Cards list is NULL!");
        }

        if (UIManager.Instance != null)
        {
            // 🌟🌟🌟 CRITICAL FIX: UIManager에서 PlayerManager 데이터를 기반으로 색상 업데이트 🌟🌟🌟
            UIManager.Instance.UpdateSlotColorsFromPlayers();
        }

        if (GameManager.Instance != null && GameManager.Instance.isActiveAndEnabled)
        {
            StartCoroutine(StartCardSelection(msg.cards, msg.timeLimit));
        }
        else if (this.isActiveAndEnabled)
        {
            StartCoroutine(StartCardSelection(msg.cards, msg.timeLimit));
        }
        else
        {
            Debug.LogError("❌ FATAL: RoundManager와 GameManager 모두 코루틴을 시작할 수 없는 상태입니다.");
        }
    }

    public void PrepareNextRound(int nextRoundNumber)
    {
        currentRound = nextRoundNumber;
        Debug.Log($"[RoundManager] New round prepared: Round {currentRound}");
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

        Debug.Log("[DEBUG 4] 카드 선택 코루틴 시작, 6초 대기.");
        // SHOW_ROLE/SHOW_ORACLE 대기 시간
        yield return new WaitForSeconds(6.0f);

        // 카드 선택 UI 활성화
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
        UIManager.Instance.ShowSystemMessage($"신의 심판: {msg.sentence} (Score {msg.score})");

        // 심판 연출 시작 (카메라 이동, UI 표시 등)
        GameManager.Instance.StartJudgmentSequence(msg);

        // 마을 HP 업데이트
        GameManager.Instance.UpdateVillageHP(msg.score);

        // 🌟🌟🌟 FIX: PlayerManager 데이터를 기반으로 슬롯-색상 딕셔너리 생성 🌟🌟🌟
        Dictionary<string, string> currentSlotColors = new Dictionary<string, string>();
        foreach (var playerEntry in GameManager.Instance.GetPlayers())
        {
            PlayerManager pm = playerEntry.Value;
            if (!string.IsNullOrEmpty(pm.slot) && !string.IsNullOrEmpty(pm.colorName))
            {
                currentSlotColors[pm.slot] = pm.colorName;
            }
        }

        // 히스토리 패널에 기록
        UIManager.Instance.AddHistoryItem(
           msg,
           currentRound,
           currentMission,
           currentSlotColors,
           msg.finalWords
        );
    }
}