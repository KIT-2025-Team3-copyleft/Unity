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

        // 🌟 [핵심] UIManager를 통해 슬롯 색상 업데이트
        // 서버 메시지에 slotColors 필드가 추가되었다고 가정합니다.
        if (UIManager.Instance != null && msg.slotColors != null)
        {
            // msg.slotColors는 RoundStartMessage에 포함되어 있지 않지만,
            // 이 기능을 위해 서버에서 보내준다고 가정하고 호출합니다.
            // GameMessages.cs에 slotColors가 추가되어야 합니다.
            UIManager.Instance.UpdateSlotColors(msg.slotColors);
        }
        else if (UIManager.Instance != null)
        {
            // 색상 정보가 없으면 기본 색상(그린)으로 초기화 (UpdateSlotColors 내부에서 처리됨)
            UIManager.Instance.UpdateSlotColors(new Dictionary<string, string>());
        }

        // SHOW_ORACLE, SHOW_ROLE 이벤트는 GameManager에서 이미 별도로 처리되었습니다.

        // 채팅창 상호작용 가능 여부 설정 (현재 주석 처리)
        // if (GameManager.Instance.chatInput != null) GameManager.Instance.chatInput.interactable = msg.chatEnabled; 

        // 카드 선택 코루틴 시작
        StartCoroutine(StartCardSelection(msg.cards, msg.timeLimit));
    }

    // 카드 선택 시작
    private IEnumerator StartCardSelection(List<string> cards, int selectionTime)
    {
        // SHOW_ORACLE/SHOW_ROLE 메시지의 팝업 시간(약 4초)을 기다립니다.
        yield return new WaitForSeconds(4f);

        // 카드 선택 UI 활성화
        UIManager.Instance.SetupCardButtons(cards);

        // 타이머 시작 (시간 종료 시 임의 카드 자동 선택)
        StartCoroutine(
            UIManager.Instance.StartTimer(selectionTime, () => UIManager.Instance.AutoSelectRandomCard())
        );
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
        GameManager.Instance.systemMessageText.text = $"신의 심판: {msg.finalSentence} (HP {msg.scoreChange})";

        // 심판 연출 시작 (카메라 이동, UI 표시 등)
        GameManager.Instance.StartJudgmentSequence(msg);

        // 마을 HP 업데이트
        GameManager.Instance.UpdateVillageHP(msg.scoreChange);

        // 히스토리 패널에 기록
        // 🌟 수정: currentRound와 currentMission을 UIManager에 전달
        UIManager.Instance.AddHistoryItem(
           msg,
           currentRound,
           currentMission, // 🌟 신탁 전달
           msg.slotColors,
           msg.finalWords
        );
    }
}