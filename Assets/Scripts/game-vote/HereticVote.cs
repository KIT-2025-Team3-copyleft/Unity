using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HereticVote : MonoBehaviour
{
    [Header("UI 연결")]
    public TMP_Text timerText;           // 타이머 텍스트
    public Button[] playerButtons;       // 투표 버튼 4개
    public TMP_Text[] playerNameTexts;   // 닉네임 Text 4개
    public TMP_Text[] checkMarks;        // V 표시 Text 4개

    public Player localPlayer;           // 로컬 플레이어 (테스트용)

    private float timeLeft = 60f;
    private bool voteInProgress = false;

    public List<Player> players;         // 투표 대상 플레이어
    private Dictionary<int, int> votes = new Dictionary<int, int>();
    private int voteCount = 0;           // 테스트용: 내가 누른 투표 횟수

    public delegate void HeresyVoteEndDelegate(List<Player> result);
    public event HeresyVoteEndDelegate OnHeresyVoteEnd;

    // 투표 시작
    public void StartVote(List<Player> playerList)
    {
        players = playerList;
        voteInProgress = true;
        timeLeft = 60f;
        votes.Clear();
        voteCount = 0;

        gameObject.SetActive(true);

        for (int i = 0; i < players.Count; i++)
        {
            int index = i;
            int targetId = players[i].id;

            // 닉네임 UI 표시
            playerNameTexts[i].text = players[i].nickname;

            // 체크 표시 초기화
            checkMarks[i].gameObject.SetActive(false);

            // 버튼 색상
            playerButtons[i].GetComponent<Image>().color = players[i].color;

            // 버튼 클릭 이벤트 연결
            playerButtons[i].onClick.RemoveAllListeners();
            playerButtons[i].onClick.AddListener(() =>
            {
                OnPlayerVote(localPlayer, targetId, index);
            });
        }
    }

    private void Update()
    {
        if (!voteInProgress) return;

        timeLeft -= Time.deltaTime;
        timerText.text = $"{Mathf.CeilToInt(timeLeft)}s";

        if (timeLeft <= 0)
        {
            voteInProgress = false;
            EndVote();
        }
    }

    // 버튼 클릭 시
    private void OnPlayerVote(Player voter, int targetId, int buttonIndex)
    {
        if (voteCount >= 4) return; // 테스트용: 4번까지만 투표 가능

        voteCount++;

        if (!votes.ContainsKey(targetId))
            votes[targetId] = 0;

        votes[targetId]++;

        ShowMyVoteCheck(buttonIndex);

        if (voteCount >= 4)
        {
            voteInProgress = false;
            EndVote();
        }
    }

    // V 표시
    private void ShowMyVoteCheck(int index)
    {
        for (int i = 0; i < checkMarks.Length; i++)
            checkMarks[i].gameObject.SetActive(false);

        checkMarks[index].gameObject.SetActive(true);
    }

    // 투표 종료
    private void EndVote()
    {
        int maxVotes = 0;
        foreach (var v in votes.Values)
            if (v > maxVotes) maxVotes = v;

        List<Player> result = new List<Player>();
        foreach (var p in players)
        {
            if (votes.ContainsKey(p.id) && votes[p.id] == maxVotes)
            {
                result.Add(p);

                // 최고 득표자 Light 켜기
                if (p.highlightLight != null)
                    p.highlightLight.enabled = true;
            }
            else
            {
                // 나머지 Light 끄기
                if (p.highlightLight != null)
                    p.highlightLight.enabled = false;
            }
        }

        gameObject.SetActive(false);
        OnHeresyVoteEnd?.Invoke(result);
    }
}
