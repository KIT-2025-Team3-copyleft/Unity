/*using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// GameManager, RoundManager, UIManager의 기능 테스트를 위한 드라이버 스크립트
public class TestDriver : MonoBehaviour
{
    // 테스트 시작 버튼 (Inspector에서 연결)
    public Button startTestButton;
    public PlayerSpawner playerSpawner; // PlayerSpawner 스크립트 연결

    private void Start()
    {
        // 싱글톤 인스턴스 확인 (테스트에 필수)
        if (GameManager.Instance == null || RoundManager.Instance == null || UIManager.Instance == null || playerSpawner == null)
        {
            Debug.LogError("GameManager, RoundManager, UIManager 또는 PlayerSpawner 싱글톤/컴포넌트가 Scene에 없거나 연결되지 않았습니다. 테스트를 시작할 수 없습니다.");
            return;
        }

        // 테스트 시작 버튼 리스너 추가
        if (startTestButton != null)
        {
            startTestButton.onClick.AddListener(StartFullTestSequence);
            GameManager.Instance.systemMessageText.text = "테스트 준비 완료. 'Start Test' 버튼을 누르세요.";
        }
        else
        {
            Debug.LogWarning("Start Test Button이 연결되지 않았습니다. 5초 후 자동 테스트 시작.");
            StartCoroutine(AutoStartTest(5f));
        }

        // 플레이어 스포너 설정 확인 및 더미 플레이어 생성
        SetupDummyPlayers();

    }

    // 버튼 연결이 안된 경우 자동 시작 코루틴
    private IEnumerator AutoStartTest(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 🔥 UIManager가 UI 연결 완료할 때까지 기다림
        while (!UIManager.Instance.IsUILinked)
            yield return null;

        StartFullTestSequence();
    }


    // 테스트 시작 시 호출되는 메인 시퀀스
    public void StartFullTestSequence()
    {


        Debug.Log("--- ✨ 테스트 시퀀스 시작 ✨ ---");
        if (startTestButton != null)
        {
            startTestButton.interactable = false;
        }
        StartCoroutine(TestSequenceCoroutine());
    }

    // 더미 플레이어 생성 (PlayerSpawner 활용)
    private void SetupDummyPlayers()
    {
        if (playerSpawner.spawnPoints == null || playerSpawner.spawnPoints.Length < 4)
        {
            Debug.LogError("PlayerSpawner에 spawnPoints 배열이 4개 미만입니다. 더미 플레이어 생성을 건너뜁니다.");
            return;
        }

        // 클라이언트 본인 플레이어 정보 설정 (slot2)
        GameManager.Instance.mySlot = "slot2";
        GameManager.Instance.PlayerName = "테스터";

        // 더미 플레이어 데이터
        var playersToSpawn = new Dictionary<string, (string nickname, int spawnIndex)>
        {
            { "slot1", ("김철수", 0) },
            { "slot2", ("테스터", 1) },
            { "slot3", ("박영희", 2) },
            { "slot4", ("이민지", 3) }
        };

        foreach (var kvp in playersToSpawn)
        {
            PlayerManager pm = playerSpawner.SpawnPlayer(kvp.Key, kvp.Value.nickname, kvp.Value.spawnIndex);

            // 본인 플레이어의 경우 actionCompleted를 false로 설정하여 카드 선택 가능하게 함
            if (kvp.Key == GameManager.Instance.mySlot)
            {
                pm.actionCompleted = false;
                Debug.Log($"자신({pm.nickname}) 플레이어(ID: {pm.playerId}) 생성 완료. 색상 할당 대기 중.");
            }
            else
            {
                // 다른 플레이어는 이미 행동 완료했다고 가정
                pm.actionCompleted = true;
                Debug.Log($"더미({pm.nickname}) 플레이어(ID: {pm.playerId}) 생성 완료.");
            }

   
        }

        // 나의 슬롯 이미지 색상 테스트를 위해 텍스트 업데이트 (GameManager에는 UpdateMySentenceSlot 함수가 없음)
        UIManager.Instance.UpdateMySentenceSlot("slot1", "슬롯 1");
        UIManager.Instance.UpdateMySentenceSlot("slot2", "슬롯 2");
        UIManager.Instance.UpdateMySentenceSlot("slot3", "슬롯 3");
        UIManager.Instance.UpdateMySentenceSlot("slot4", "슬롯 4");
    }


    // 테스트 코루틴
    private IEnumerator TestSequenceCoroutine()
    {
        // 1. 라운드 1 시작 (ROUND_START)
        Debug.Log("1. 라운드 1 시작 (RoundManager.HandleRoundStart)");
        GameManager.Instance.systemMessageText.text = "1. 라운드 1 시작 메시지 수신...";

        RoundStartMessage round1Msg = new RoundStartMessage
        {
            roundNumber = 1,
            timeLimit = 10,
            mission = "숲의 평화",
            myRole = "traitor",
            mySlot = GameManager.Instance.mySlot,
            cards = new List<string> { "평화", "파괴", "자유", "억압", "바나나", "딸기", "키위", "계란" },
            
            chatEnabled = true,
            godPersonality = "심술쟁이"
        };

        RoundManager.Instance.HandleRoundStart(round1Msg);
        yield return new WaitForSeconds(3f);

        // 2. 카드 선택 시간 경과 -> 자동 선택 시뮬레이션
        // UIManager.StartTimer에서 AutoSelectRandomCard를 호출할 때까지 기다려야 함
        GameManager.Instance.systemMessageText.text = "2. 카드 선택 시간(10초) 경과 시뮬레이션 중...";
        yield return new WaitForSeconds(10f); // UIManager.StartTimer와 동일하게 작동한다고 가정

        Debug.Log("2. 자동 카드 선택 시뮬레이션 완료 (UIManager.AutoSelectRandomCard 호출됨)");

        // 3. 나의 카드 선택 확인 (CARD_SELECTION_CONFIRMED)
        Debug.Log("3. 나의 카드 선택 확인 (RoundManager.HandleCardSelectionConfirmed)");
        RoundManager.Instance.HandleCardSelectionConfirmed();
        yield return new WaitForSeconds(2f);

        // 4. 다른 플레이어 행동 완료 (PLAYER_ACTION_UPDATE)
        Debug.Log("4. 다른 플레이어 행동 업데이트 (GameManager.OnServerMessage -> PLAYER_ACTION_UPDATE)");
        PlayerActionUpdate pActionMsg = new PlayerActionUpdate { playerId = "slot1", actionStatus = "completed" };
        // GameManager.OnServerMessage(JsonUtility.ToJson(pActionMsg)); 대신 직접 호출하여 테스트
        GameManager.Instance.systemMessageText.text = $"{pActionMsg.playerId}가 행동을 완료했습니다. (강제 업데이트)";
        // players["slot1"].MarkActionCompleted()는 이미 SetupDummyPlayers에서 이루어졌다고 가정
        yield return new WaitForSeconds(2f);


        // 5. 해석 단계 종료 (INTERPRETATION_END)
        Debug.Log("5. 해석 단계 종료 (RoundManager.HandleInterpretationEnd)");
        InterpretationEnd intEndMsg = new InterpretationEnd
        {
            message = "모두의 선택이 완료되었습니다. 신의 심판을 기다리십시오.",
            chatEnabled = false
        };
        RoundManager.Instance.HandleInterpretationEnd(intEndMsg);
        yield return new WaitForSeconds(2f);

        // 6. 라운드 1 심판 결과 (ROUND_RESULT)
        Debug.Log("6. 라운드 1 심판 결과 (RoundManager.HandleRoundResult & JudgmentSequence)");
        RoundResult round1ResultMsg = new RoundResult
        {
            finalSentence = "테스터는 신의 뜻을 거역했다.",
            scoreChange = -10,
            visualCue = new VisualCue { effect = "thunder" },
            reason = "테스터가 배신자의 카드를 내어 숲의 평화를 깨뜨렸기 때문입니다.",
            finalWords = new List<string> { "테스터는", "신의", "뜻을", "거역했다" },
            slotColors = new Dictionary<string, string> {
                { "slot1", "blue" }, { "slot2", "red" }, { "slot3", "yellow" }, { "slot4", "green" }
            }
        };

        // RoundManager가 JudgmentSequence를 시작하고 UIManager.AddHistoryItem 호출
        RoundManager.Instance.HandleRoundResult(round1ResultMsg);

        // 심판 시퀀스가 끝날 때까지 기다림 (약 0.8s + 0.7s + 3s + 1s + 2s + 1s = 8.5초)
        float totalJudgmentDuration = GameManager.Instance.zoomDuration + GameManager.Instance.settleDuration + 7f;
        GameManager.Instance.systemMessageText.text = $"6. 심판 시퀀스 시작. HP 변화: {round1ResultMsg.scoreChange}. (총 {totalJudgmentDuration}초 대기)";

        yield return new WaitForSeconds(totalJudgmentDuration);

        // 7. 히스토리 패널 토글 테스트
        Debug.Log("7. 히스토리 패널 토글 테스트 (UIManager.ToggleHistoryPanel)");
        yield return new WaitForSeconds(8f);
        UIManager.Instance.ToggleHistoryPanel();
        yield return new WaitForSeconds(4f);
        UIManager.Instance.ToggleHistoryPanel();
        yield return new WaitForSeconds(4f);

        Debug.Log("--- ✅ 테스트 시퀀스 종료. 현재 마을 HP: " + GameManager.Instance.currentHP + " ---");
        GameManager.Instance.systemMessageText.text = "테스트 시퀀스 종료. 콘솔 로그 확인.";

        if (startTestButton != null)
        {
            startTestButton.interactable = true;
        }
    }
}*/