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
        GameManager.Instance.chatInput.interactable = msg.chatEnabled;

        StartCoroutine(StartCardSelection(msg.cards, msg.timeLimit));
    }

    // 카드 선택 시작
    private IEnumerator StartCardSelection(List<string> cards, int selectionTime)
    {
        yield return new WaitForSeconds(2f);
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
        GameManager.Instance.chatInput.interactable = msg.chatEnabled;
        GameManager.Instance.systemMessageText.text = msg.message;
    }

    // 라운드 종료
    public void HandleRoundResult(RoundResult msg)
    {
        GameManager.Instance.systemMessageText.text = $"신의 평가: {msg.finalSentence} (+{msg.scoreChange})";
        UIManager.Instance.PlayVisualCue(msg.visualCue);

        UIManager.Instance.AddHistoryEntry(
            msg.finalSentence,
            currentRound,
            msg.scoreChange
            ); // 이 부분은 상승, 하락에 따라 값으로 1,0?
    }
}