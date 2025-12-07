using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoteManager : MonoBehaviour
{
    public static VoteManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        WebSocketManager.Instance.OnServerMessage += HandleServerMessage;
    }

    private void OnDestroy()
    {
        if (WebSocketManager.Instance != null)
            WebSocketManager.Instance.OnServerMessage -= HandleServerMessage;
    }

    // =====================================================
    // 서버 메시지 처리
    // =====================================================

    private void HandleServerMessage(string rawJson)
    {
        string eventType = ExtractEventType(rawJson);

        switch (eventType)
        {
            case "VOTE_PROPOSAL_START":
                HandleStep1Start(rawJson);
                break;

            case "VOTE_PROPOSAL_UPDATE":
                HandleStep1Update(rawJson);
                break;

            case "VOTE_PROPOSAL_FINISH":
                HandleStep1Finish(rawJson);
                break;

            case "TRIAL_START":
                HandleStep2Start(rawJson);
                break;

            case "TRIAL_UPDATE":
                HandleStep2Update(rawJson);
                break;

            case "TRIAL_RESULT":
                HandleStep3Result(rawJson);
                break;

            default:
                Debug.Log("[VoteManager] Unknown event: " + eventType);
                break;
        }
    }

    // =====================================================
    // JSON event 추출
    // =====================================================
    [Serializable]
    private class EventTypeExtractor { public string eventField; }

    private string ExtractEventType(string json)
    {
        string mod = json.Replace("\"event\"", "\"eventField\"");
        EventTypeExtractor e = JsonUtility.FromJson<EventTypeExtractor>(mod);
        return e.eventField;
    }

    // =====================================================
    // STEP 1 (찬반 투표)
    // =====================================================

    [Serializable]
    private class Step1StartWrapper
    {
        public string eventField;
        public string message;
        public Step1StartData data;
    }

    [Serializable]
    private class Step1StartData
    {
        public int totalPlayers;
    }

    private Coroutine step1TimerCo;

    private void HandleStep1Start(string json)
    {
        var msg = JsonUtility.FromJson<Step1StartWrapper>(json);

        int time = int.Parse(msg.message);

        VoteUIManager.Instance.ShowStep1();
        VoteUIManager.Instance.SetStep1Buttons();

        if (step1TimerCo != null) StopCoroutine(step1TimerCo);
        step1TimerCo = StartCoroutine(Step1Timer(time));
    }

    private IEnumerator Step1Timer(int t)
    {
        while (t >= 0)
        {
            VoteUIManager.Instance.UpdateStep1Timer(t);
            yield return new WaitForSeconds(1);
            t--;
        }
    }

    // 중간 업데이트
    [Serializable]
    private class Step1UpdateWrapper
    {
        public string eventField;
        public Step1UpdateData data;
    }

    [Serializable]
    private class Step1UpdateData
    {
        public int agreeCount;
        public int totalPlayers;
    }

    private void HandleStep1Update(string json)
    {
        var msg = JsonUtility.FromJson<Step1UpdateWrapper>(json);
        VoteUIManager.Instance.UpdateStep1Count(msg.data.agreeCount, msg.data.totalPlayers);
    }

    // 종료
    [Serializable]
    private class Step1FinishWrapper
    {
        public string eventField;
        public string message;
    }

    private void HandleStep1Finish(string json)
    {
        var msg = JsonUtility.FromJson<Step1FinishWrapper>(json);
        VoteUIManager.Instance.ShowResult(msg.message);
    }

    // 버튼 누르면 서버로 Step1 투표 전송
    public void SendStep1Vote(bool agree)
    {
        WebSocketManager.Instance.SendProposeVote(agree);
    }

    // =====================================================
    // STEP 2 (지목 투표)
    // =====================================================

    [Serializable]
    private class Step2StartWrapper
    {
        public string eventField;
        public string message;
        public Step2StartData data;
    }

    [Serializable]
    private class Step2StartData
    {
        public List<PlayerInfo> players;
    }

    [Serializable]
    private class PlayerInfo
    {
        public string sessionId;
        public string nickname;
    }

    private Coroutine step2TimerCo;
    private List<PlayerInfo> currentPlayers;

    private void HandleStep2Start(string json)
    {
        var msg = JsonUtility.FromJson<Step2StartWrapper>(json);

        currentPlayers = msg.data.players;

        List<string> nicks = new List<string>();
        foreach (var p in currentPlayers)
            nicks.Add(p.nickname);

        VoteUIManager.Instance.ShowStep2(nicks);

        int time = int.Parse(msg.message);

        if (step2TimerCo != null) StopCoroutine(step2TimerCo);
        step2TimerCo = StartCoroutine(Step2Timer(time));
    }

    private IEnumerator Step2Timer(int t)
    {
        while (t >= 0)
        {
            VoteUIManager.Instance.UpdateStep2Timer(t);
            yield return new WaitForSeconds(1);
            t--;
        }
    }

    // 중간 투표 업데이트
    [Serializable]
    private class Step2UpdateWrapper
    {
        public string eventField;
        public Step2UpdateData data;
    }

    [Serializable]
    private class Step2UpdateData
    {
        public List<int> voteCounts;
    }

    private void HandleStep2Update(string json)
    {
        var msg = JsonUtility.FromJson<Step2UpdateWrapper>(json);

        for (int i = 0; i < msg.data.voteCounts.Count; i++)
        {
            VoteUIManager.Instance.UpdateVMark(i, msg.data.voteCounts[i]);
        }
    }

    // 실제 투표 전송
    public void SendStep2Vote(int index)
    {
        string targetSessionId = currentPlayers[index].sessionId;
        WebSocketManager.Instance.SendCastVote(targetSessionId);
    }

    // =====================================================
    // STEP 3 결과
    // =====================================================

    [Serializable]
    private class Step3ResultWrapper
    {
        public string eventField;
        public string message;
    }

    private void HandleStep3Result(string json)
    {
        var msg = JsonUtility.FromJson<Step3ResultWrapper>(json);
        VoteUIManager.Instance.ShowResult(msg.message);
    }
}

