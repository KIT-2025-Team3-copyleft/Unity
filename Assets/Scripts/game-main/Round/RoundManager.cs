/*
 * 현재 시연 중, 오류 해결을 위해 채팅창 관련 코드는 주석처리됨
 * 34, 63 LINE
 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance;

    public int currentRound = 0;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // 라운드 시작
    public void HandleRoundStart(RoundStartMessage msg)
    {
        currentRound++;
        GameManager.Instance.myRole = msg.myRole;
        GameManager.Instance.mySlot = msg.mySlot;
        UIManager.Instance.ShowOracleAndRole(msg.mission, msg.myRole, currentRound);

        if (msg.myRole == "traitor") UIManager.Instance.ShowTraitorInfo(msg.godPersonality);
        //GameManager.Instance.chatInput.interactable = msg.chatEnabled; 오류 해결을 위한 주석처리

        StartCoroutine(StartCardSelection(msg.cards, msg.timeLimit));
    }

    // 카드 선택 시작
    private IEnumerator StartCardSelection(List<string> cards, int selectionTime)
    {
        yield return new WaitForSeconds(4f);
        UIManager.Instance.SetupCardButtons(cards);

        StartCoroutine(
            UIManager.Instance.StartTimer(selectionTime, () => UIManager.Instance.AutoSelectRandomCard())
        );
    }
    // 카드 선택 완료(개인)
    public void HandleCardSelectionConfirmed()
    {
        UIManager.Instance.DisableMyCards();
        GameManager.Instance.systemMessageText.text = "카드 선택이 확인되었습니다.";
    }

    public void HandlePlayerActionUpdate(PlayerActionUpdate msg)
    {
        GameManager.Instance.systemMessageText.text = $"{msg.playerId}가 행동을 완료했습니다.";
    }

    // 카드 선택 완료(전체)
    public void HandleInterpretationEnd(InterpretationEnd msg)
    {
        //GameManager.Instance.chatInput.interactable = msg.chatEnabled; 오류 해결을 위한 임시 주석처리
        GameManager.Instance.systemMessageText.text = msg.message;
    }

    // 라운드 종료
    public void HandleRoundResult(RoundResult msg)
    {
        GameManager.Instance.systemMessageText.text = $"신의 심판: {msg.finalSentence} (HP {msg.scoreChange})";

        GameManager.Instance.StartJudgmentSequence(msg);

        GameManager.Instance.UpdateVillageHP(msg.scoreChange);

        UIManager.Instance.AddHistoryItem(
           msg,
           currentRound,
           msg.slotColors,
           msg.finalWords
        );
    }
}