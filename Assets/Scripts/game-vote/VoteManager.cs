using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class TempWrapper
{
    public string @event;
    public string message;
    // string data; // data가 객체일 수 있으므로 파싱되지 않게 하거나 생략합니다.
}


[System.Serializable]
public class Step1EventWrapper
{
    public string @event;
    public string message;
    public Step1UpdateData data; 
}

[System.Serializable]
public class TrialEventWrapper
{
    public string @event;
    public string message;
    public TrialVoteStatus data; 
}

[System.Serializable]
public class Step1UpdateData
{
    public int count;
    public int totalPlayers;
}

[System.Serializable]
public class TrialVoteStatus
{
    public List<VoteItem> voteStatus;

    [System.Serializable]
    public class VoteItem
    {
        public string targetId;
        public int count;
    }
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
    public void OnVoteEvent(string json)
    {
        TempWrapper tempWrapper = JsonUtility.FromJson<TempWrapper>(json);

        if (string.IsNullOrEmpty(tempWrapper?.@event))
        {
            Debug.LogError("Received JSON does not contain a valid event type.");
            return;
        }

        switch (tempWrapper.@event)
        {
            case "VOTE_PROPOSAL_START":
                StartVoteProposal(tempWrapper.message);
                break;

            case "VOTE_PROPOSAL_FAILED":
            case "TRIAL_RESULT":
                FinishVote(tempWrapper.message); 
                break;

            case "VOTE_PROPOSAL_UPDATE":
                Step1EventWrapper step1Wrapper = JsonUtility.FromJson<Step1EventWrapper>(json);
                if (step1Wrapper?.data != null)
                {
                    UpdateStep1Vote(step1Wrapper.data);
                }
                break;

            case "TRIAL_START":
                StartStep2Vote(tempWrapper.message);
                break;

            case "TRIAL_VOTE_UPDATE":
                TrialEventWrapper trialWrapper = JsonUtility.FromJson<TrialEventWrapper>(json);
                if (trialWrapper?.data != null)
                {
                    UpdateStep2Vote(trialWrapper.data);
                }
                break;
        }
    }

    // Step1: 찬반 투표 시작
    private void StartVoteProposal(string message)
    {
        int timer = TryParseOrDefault(message, 30);
        voteUIManager?.ShowStep1();
        voteUIManager?.StartStep1Timer(timer);
    }

    private void UpdateStep1Vote(Step1UpdateData info)
    {
        voteUIManager?.UpdateStep1Count(info.count, info.totalPlayers);
    }

    // Step1 & Step3: 결과
    private void FinishVote(string message)
    {
        voteUIManager?.ShowResult(message, 5f);
    }

    // Step2: 심문 시작
    private void StartStep2Vote(string message)
    {
        int timer = TryParseOrDefault(message, 20); // message 변수를 바로 사용

        var orderedPlayers = GameManager.Instance.GetOrderedPlayers();
        voteUIManager?.ShowStep2(orderedPlayers); // 닉네임+색상을 모두 포함

        voteUIManager?.StartStep2Timer(timer);
    }

    private void UpdateStep2Vote(TrialVoteStatus info)
    {
        if (info == null || info.voteStatus == null) return;

        var orderedPlayers = GameManager.Instance.GetOrderedPlayers();

        foreach (var item in info.voteStatus)
        {
            // 1) targetId 기반으로 플레이어 인덱스를 찾는다
            int index = orderedPlayers.FindIndex(p => p.sessionId == item.targetId);
            if (index == -1) continue;

            // 2) UI에 표시
            voteUIManager?.UpdateVMark(index, item.count);
        }
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
