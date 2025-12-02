using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoteManager : MonoBehaviour
{
    public static VoteManager Instance;

    public bool isLocalTestMode = true; // 로컬 테스트 모드

    private void Awake()
    {
        Instance = this;
    }

    private List<PlayerVoteState> players = new List<PlayerVoteState>();
    private int totalPlayers = 4;

    private void Start()
    {
        for (int i = 0; i < totalPlayers; i++)
            players.Add(new PlayerVoteState { playerId = $"P{i + 1}" });

        StartStep1();
    }

    // ======================================================
    #region STEP 1: 동의/거부 투표

    private Coroutine timerRoutine;

    public void StartStep1()
    {
        Debug.Log("Step1 시작");

        VoteUIManager.Instance.ShowStep1();
        VoteUIManager.Instance.UpdateStep1Count(0);

        VoteUIManager.Instance.agreeButton.onClick.RemoveAllListeners();
        VoteUIManager.Instance.disagreeButton.onClick.RemoveAllListeners();

        VoteUIManager.Instance.agreeButton.onClick.AddListener(() => OnVoteStep1(true));
        VoteUIManager.Instance.disagreeButton.onClick.AddListener(() => OnVoteStep1(false));

        if (timerRoutine != null) StopCoroutine(timerRoutine);
        timerRoutine = StartCoroutine(Step1Timer());
    }

    private IEnumerator Step1Timer()
    {
        int t = 60;
        while (t > 0)
        {
            VoteUIManager.Instance.UpdateStep1Timer(t);
            yield return new WaitForSeconds(1);
            t--;
        }

        VoteUIManager.Instance.UpdateStep1Timer(0);

        foreach (var p in players)
        {
            if (!p.votedStep1)
            {
                p.votedStep1 = true;
                p.agree = false;
            }
        }

        CheckStep1Finish();
    }

    public void OnVoteStep1(bool agree)
    {
        var p1 = players[0];
        if (p1.votedStep1) return;

        p1.votedStep1 = true;
        p1.agree = agree;

        VoteUIManager.Instance.UpdateStep1Count(CountVotedStep1());

        //  로컬 테스트 → 가상의 플레이어 투표를 순차적으로 진행
        if (isLocalTestMode)
        {
            StartCoroutine(SimulateStep1OtherPlayers());
        }
        else
        {
            CheckStep1Finish();
        }
    }

    /// <summary>
    ///  Step1: 가상의 플레이어 2~4가 1초 간격으로 자동 동의
    /// </summary>
    private IEnumerator SimulateStep1OtherPlayers()
    {
        for (int i = 1; i < players.Count; i++)
        {
            yield return new WaitForSeconds(1f);

            players[i].votedStep1 = true;
            players[i].agree = true;

            VoteUIManager.Instance.UpdateStep1Count(CountVotedStep1());
        }

        CheckStep1Finish();
    }

    private int CountVotedStep1()
    {
        int c = 0;
        foreach (var p in players)
            if (p.votedStep1) c++;

        return c;
    }

    private void CheckStep1Finish()
    {
        int agreeCount = 0;
        foreach (var p in players)
            if (p.agree) agreeCount++;

        if (agreeCount >= 2)
            StartStep2();
        else
            VoteUIManager.Instance.ShowResult("동의한 사람이 부족하여 투표 종료");
    }

    #endregion

    // ======================================================
    #region STEP 2: 심판 후보 투표

    private Coroutine step2TimerRoutine;
    private int step2TimeLimit = 60; // 원하는 시간으로 조절 가능

    public void StartStep2()
    {
        Debug.Log("Step2 시작");

        List<string> nicknames = new List<string>() { "A", "B", "C", "D" };
        VoteUIManager.Instance.ShowStep2(nicknames);

        // Step2 UI 타이머 시작
        if (step2TimerRoutine != null) StopCoroutine(step2TimerRoutine);
        step2TimerRoutine = StartCoroutine(Step2Timer());
    }

    private IEnumerator Step2Timer()
    {
        int t = step2TimeLimit;

        while (t > 0)
        {
            VoteUIManager.Instance.UpdateStep2Timer(t);
            yield return new WaitForSeconds(1f);
            t--;
        }

        VoteUIManager.Instance.UpdateStep2Timer(0);

        Debug.Log("Step2 타이머 종료 → 남은 플레이어 자동 처리");

        // 타이머 종료 시 투표 안 한 플레이어는 자동 투표
        for (int i = 0; i < players.Count; i++)
        {
            if (!players[i].votedStep2)
            {
                players[i].votedStep2 = true;
                players[i].selectedPlayerIndex = i; // 자기 자신 선택
            }
        }

        CountStep2Votes();

        // 결과 확인
        StartCoroutine(DelayedCheckStep2Finish());
    }

    /// <summary>
    /// Step2 버튼 클릭 → Player1 투표
    /// </summary>
    public void OnVoteStep2(int voterIndex, int targetIndex)
    {
        var voter = players[voterIndex];
        if (voter.votedStep2) return;

        voter.votedStep2 = true;
        voter.selectedPlayerIndex = targetIndex;

        CountStep2Votes();

        // 로컬 테스트 모드 → 가상의 플레이어들 1초 간격으로 투표
        if (isLocalTestMode)
        {
            StartCoroutine(SimulateStep2OtherPlayers());
        }
        else
        {
            if (AllStep2Voted())
                StartCoroutine(DelayedCheckStep2Finish());
        }
    }

    /// <summary>
    /// 가상의 플레이어 2~4가 순차적으로 자동 투표
    /// </summary>
    private IEnumerator SimulateStep2OtherPlayers()
    {
        for (int i = 1; i < players.Count; i++)
        {
            yield return new WaitForSeconds(1f);

            players[i].votedStep2 = true;
            players[i].selectedPlayerIndex = i; // 자기 자신 선택

            CountStep2Votes();
        }

        yield return new WaitForSeconds(0.8f);

        StartCoroutine(DelayedCheckStep2Finish());
    }

    private void CountStep2Votes()
    {
        int[] voteCounts = new int[players.Count];

        foreach (var p in players)
        {
            if (p.selectedPlayerIndex != -1)
                voteCounts[p.selectedPlayerIndex]++;
        }

        for (int i = 0; i < voteCounts.Length; i++)
            VoteUIManager.Instance.UpdateVMark(i, voteCounts[i]);
    }

    private bool AllStep2Voted()
    {
        foreach (var p in players)
            if (!p.votedStep2) return false;
        return true;
    }

    /// <summary>
    /// Step2가 끝난 뒤 잠시 대기한 후 결과 확인 → Step3로 진행
    /// </summary>
    private IEnumerator DelayedCheckStep2Finish()
    {
        yield return new WaitForSeconds(1.5f); // UI 감상을 위한 지연

        CheckStep2Finish();
    }

    private void CheckStep2Finish()
    {
        int[] voteCounts = new int[players.Count];

        foreach (var p in players)
        {
            if (p.selectedPlayerIndex != -1)
                voteCounts[p.selectedPlayerIndex]++;
        }

        int maxVal = -1;
        int maxIdx = -1;
        int countMax = 0;

        for (int i = 0; i < voteCounts.Length; i++)
        {
            if (voteCounts[i] > maxVal)
            {
                maxVal = voteCounts[i];
                maxIdx = i;
                countMax = 1;
            }
            else if (voteCounts[i] == maxVal)
            {
                countMax++;
            }
        }

        if (countMax != 1)
        {
            VoteUIManager.Instance.ShowResult("최다 득표자가 1명이 아니므로 투표 종료");
            return;
        }

        StartStep3(maxIdx);
    }

    #endregion

    // ======================================================
    #region STEP 3: 배신자 판정

    public void StartStep3(int targetPlayerIndex)
    {
        Debug.Log("Step3 시작");

        bool isTraitor = (targetPlayerIndex == 1); // 예: 플레이어2가 배신자

        if (isTraitor)
            VoteUIManager.Instance.ShowResult("배신자가 맞습니다! HP 상승!");
        else
            VoteUIManager.Instance.ShowResult("배신자가 아닙니다. HP 감소!");
    }

    #endregion
}
