using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class VoteMessageData
{
    public List<string> playerNicknames;
    public int agreeCount;
    public int totalCount;
    public Dictionary<string, int> voteCounts;
}

[Serializable]
public class VoteMessageWrapper
{
    public string @event;
    public string message;
    public VoteMessageData data;
}

public class VoteManager : MonoBehaviour
{
    public static VoteManager Instance;
    [HideInInspector]
    public VoteUIManager voteUIManager;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public void LinkUIManager(VoteUIManager ui)
    {
        if (voteUIManager == null && ui != null) voteUIManager = ui;
    }

    // ------------------- 서버 이벤트 수신 -------------------
    public void OnVoteEvent(VoteMessageWrapper wrapper)
    {
        switch (wrapper.@event)
        {
            case "VOTE_PROPOSAL_START":
                StartVoteProposal(wrapper);
                break;
            case "VOTE_PROPOSAL_FAILED":
                FinishVote(wrapper);
                break;
            case "TRIAL_START":
                StartStep2Vote(wrapper);
                break;
            case "TRIAL_RESULT":
                FinishVote(wrapper);
                break;
        }
    }

    // Step1: 찬반 투표 시작
    private void StartVoteProposal(VoteMessageWrapper wrapper)
    {
        int timer = TryParseOrDefault(wrapper.message, 30);
        voteUIManager?.ShowStep1();
        voteUIManager?.StartStep1Timer(timer); 
    }


    // Step1 & Step3: 결과
    private void FinishVote(VoteMessageWrapper wrapper)
    {
        voteUIManager?.ShowResult(wrapper.message, 5f);
    }

    // Step2: 심문 시작
    private void StartStep2Vote(VoteMessageWrapper wrapper)
    {
        int timer = TryParseOrDefault(wrapper.message, 20);

        var orderedPlayers = GameManager.Instance.GetOrderedPlayers();
        List<string> nicknames = orderedPlayers.Select(p => p.nickname).ToList();

        voteUIManager?.ShowStep2(nicknames);
        voteUIManager?.UpdateStep2Timer(timer);
    }

    // ------------------- 클라이언트 → 서버 -------------------
    public void SendStep1Vote(bool agree)
    {
        WebSocketManager.Instance?.SendProposeVote(agree);
    }

    public void SendStep2Vote(int index)
    {
        var orderedPlayers = GameManager.Instance.GetOrderedPlayers();
        if (index < 0 || index >= orderedPlayers.Count) return;

        string sessionId = orderedPlayers[index].sessionId;
        WebSocketManager.Instance?.SendCastVote(sessionId);
    }

    private int TryParseOrDefault(string s, int def)
    {
        if (int.TryParse(s, out int v)) return v;
        return def;
    }
}
