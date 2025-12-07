using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance;

    public int currentRound = 0;
    private string currentMission = "";

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
        currentRound++;

        currentMission = msg.mission;

        // 🌟 mySlot 업데이트 (첫 라운드 및 후속 라운드 모두 여기서 할당됨)
        GameManager.Instance.mySlot = msg.mySlot;

        // 🌟🌟🌟 상태 초기화
        if (GameManager.Instance != null)
        {
            GameManager.Instance.cardSelectedCompleted = false;
        }

        // 🌟🌟🌟 [디버그 추가] 카드 목록 확인 🌟🌟🌟
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
            // 🌟🌟🌟 수정: slotColors를 받지 않고, PlayerManager 기반으로 UI 색상 업데이트 요청 🌟🌟🌟
            UIManager.Instance.UpdateSlotColorsFromPlayers();
        }
        // SHOW_ORACLE, SHOW_ROLE 이벤트는 GameManager에서 이미 별도로 처리되었습니다.

        // 채팅창 상호작용 가능 여부 설정 (현재 주석 처리)
        // if (GameManager.Instance.chatInput != null) GameManager.Instance.chatInput.interactable = msg.chatEnabled; 

        // 🌟🌟🌟 코루틴 시작 안정성 보장 🌟🌟🌟
        if (GameManager.Instance != null && GameManager.Instance.isActiveAndEnabled)
        {
            // RoundManager 오브젝트의 활성 상태와 무관하게 GameManager를 통해 코루틴 실행
            GameManager.Instance.StartCoroutine(StartCardSelection(msg.cards, msg.timeLimit));
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

    // 🌟🌟🌟 PrepareNextRound 함수 추가 (NEXT_ROUND_START 처리) 🌟🌟🌟
    public void PrepareNextRound(int nextRoundNumber)
    {
        currentRound = nextRoundNumber;
        Debug.Log($"[RoundManager] New round prepared: Round {currentRound}");
    }
    // -------------------------------------------------------------

    // 카드 선택 시작
    private IEnumerator StartCardSelection(List<string> cards, int selectionTime)
    {
        // SHOW_ORACLE/SHOW_ROLE 메시지의 팝업 시간(약 4초)을 기다립니다.
        Debug.Log("[DEBUG 4] 카드 선택 코루틴 시작, 6초 대기.");
        // 🌟🌟🌟 시간 조정: SHOW_ROLE 대기 시간 증가 (4s -> 6s) 🌟🌟🌟
        yield return new WaitForSeconds(6.0f);

        // 카드 선택 UI 활성화
        UIManager.Instance.SetupCardButtons(cards);
        Debug.Log($"[DEBUG 5] SetupCardButtons 호출 완료. Cards Count: {cards?.Count ?? 0}");

        // 타이머 시작 (시간 종료 시 임의 카드 자동 선택)
        StartCoroutine(
            UIManager.Instance.StartTimer(selectionTime, () => UIManager.Instance.AutoSelectRandomCard())
        );
        Debug.Log($"[DEBUG 6] UIManager.StartTimer 호출 완료. Time: {selectionTime}");
    }

    // 카드 선택 완료(개인) - 서버로부터 CARD_SELECTION_CONFIRMED 수신 시 호출
    public void HandleCardSelectionConfirmed()
    {
        // 카드 선택 UI 비활성화
        UIManager.Instance.DisableMyCards();
        GameManager.Instance.systemMessageText.text = "카드 선택이 확인되었습니다.";
    }

    // 다른 플레이어 행동 완료 업데이트
    public void HandlePlayerActionUpdate(PlayerActionUpdate msg)
    {
        GameManager.Instance.systemMessageText.text = $"{msg.playerId}가 행동을 완료했습니다.";
    }

    // 카드 선택 완료(전체) - 서버로부터 ALL_CARDS_SELECTED 수신 시 호출
    public void HandleInterpretationEnd(InterpretationEnd msg)
    {
        // if (GameManager.Instance.chatInput != null) GameManager.Instance.chatInput.interactable = msg.chatEnabled; // 채팅 활성화/비활성화
        GameManager.Instance.systemMessageText.text = msg.message;
    }

    // 라운드 종료 - 서버로부터 ROUND_RESULT 수신 시 호출
    public void HandleRoundResult(RoundResult msg)
    {
        GameManager.Instance.systemMessageText.text = $"신의 심판: {msg.sentence} (HP {msg.score})";

        // 심판 연출 시작 (카메라 이동, UI 표시 등)
        GameManager.Instance.StartJudgmentSequence(msg);

        // 마을 HP 업데이트
        GameManager.Instance.UpdateVillageHP(msg.score);

        // 🌟🌟🌟 수정: 히스토리 패널에 기록할 때, 현재 PlayerManager 데이터에서 슬롯/색상 정보를 가져오기 위해
        // UIManager에서 사용할 Dictionary<string, string> (슬롯 역할: 색상)을 직접 생성합니다.

        Dictionary<string, string> currentSlotColors = new Dictionary<string, string>();
        foreach (var playerEntry in GameManager.Instance.GetPlayers())
        {
            PlayerManager pm = playerEntry.Value;
            if (!string.IsNullOrEmpty(pm.slot) && !string.IsNullOrEmpty(pm.colorName))
            {
                currentSlotColors[pm.slot] = pm.colorName;
            }
        }

        UIManager.Instance.AddHistoryItem(
           msg,
           currentRound,
           currentMission, // 🌟 신탁 전달
           currentSlotColors, // 🌟 생성된 슬롯 색상 딕셔너리 전달
           msg.finalWords
        );
    }
}