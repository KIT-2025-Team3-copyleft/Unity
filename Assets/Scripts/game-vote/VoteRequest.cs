using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class VoteRequest : MonoBehaviour
{
    [Header("UI 연결")]
    public TMP_Text timerText;
    public TMP_Text voteCountText;
    public Button agreeButton;
    public Button rejectButton;

    private float timeLeft = 60f;
    private bool voteInProgress = false;

    private int agreeCount = 0;
    private int totalResponded = 0;
    private List<Player> players;

    public delegate void VoteEndDelegate(bool passed);
    public event VoteEndDelegate OnVoteEnd;

    public void StartVote(List<Player> playerList)
    {
        players = playerList;
        voteInProgress = true;
        timeLeft = 60f;
        agreeCount = 0;
        totalResponded = 0;

        foreach (var p in players) p.hasVoted = false;

        gameObject.SetActive(true);

        // 버튼 이벤트 코드에서 연결 (Inspector 필요 없음)
        agreeButton.onClick.RemoveAllListeners();
        agreeButton.onClick.AddListener(() => OnPlayerVote(true));

        rejectButton.onClick.RemoveAllListeners();
        rejectButton.onClick.AddListener(() => OnPlayerVote(false));
    }

    void Update()
    {
        if (!voteInProgress) return;

        timeLeft -= Time.deltaTime;
        timerText.text = $"{Mathf.CeilToInt(timeLeft)}s";
        voteCountText.text = $"{totalResponded}/{players.Count}";

        if (timeLeft <= 0)
        {
            foreach (var p in players)
                if (!p.hasVoted) OnPlayerVote(false);
        }
    }

    private void OnPlayerVote(bool agree)
    {
        if (!voteInProgress) return;

        totalResponded++;
        if (agree) agreeCount++;

        // 임시 테스트용: 한 명 기준으로 모두 처리
        if (totalResponded >= players.Count || timeLeft <= 0)
        {
            voteInProgress = false;
            gameObject.SetActive(false);

            bool passed = agreeCount >= 2;
            OnVoteEnd?.Invoke(passed);
        }
    }
}
