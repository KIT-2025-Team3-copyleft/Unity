using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Camera")]
    public Camera firstPersonCamera;
    public Camera observerCamera;
    public Camera topDownCamera;
    public Vector3 topDownStartPos;
    public Quaternion topDownStartRot;

    [Header("UI References")]
    public TextMeshProUGUI systemMessageText; // 디버그용
    public Button trialButton;
    public TMP_InputField chatInput;

    [Header("Judgment Animation Positions")]
    public Transform judgmentZoomPosition;
    public Transform judgmentFinalPosition;
    public float zoomDuration = 2.0f; 
    public float settleDuration = 2.0f; 

    [Header("Village State")]
    public int currentHP = 100;

    public string PlayerName;
    public int CurrentRoomId;


    // 로컬 플레이어 식별용 ID
    public string MySessionId { get; private set; } = "SESSION_ID_PLACEHOLDER";

    [Header("Player Info")]
    public string myRole;
    public string mySlot;

    public string currentRole;
    public string currentOracle;
    public bool cardSelectedCompleted = false;

    public GameObject localPlayerObject { get; private set; }

    private List<string> availableColors = new List<string> { "red", "blue", "green", "yellow", "pink" };
    private List<string> usedColors = new List<string>();
    private Dictionary<string, PlayerManager> players = new Dictionary<string, PlayerManager>();



    [System.Serializable]
    public class GameOverData
    {
        public string winnerRole;
    }

    [System.Serializable]
    public class GameOverMessage : MessageWrapper
    {
        public string message;
        public GameOverData data;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("[GM] Awake 호출됨");
    }

    public List<PlayerManager> GetOrderedPlayers()
    {
        return players.Values
         .Where(player => player != null && player.isConnected) 
         .ToList();
    }


    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }


    public void SetMySessionId(string sessionId)
    {
        if (!string.IsNullOrEmpty(sessionId) && MySessionId != sessionId)
        {
            MySessionId = sessionId;
            //Debug.Log($"[GM] 로컬 세션 ID가 서버 ID로 설정됨: {MySessionId}");
        }
    }


    private void Start()
    {
        SetupWebSocket();
        SetupMySessionId();
        //Debug.Log("[GM] Start 호출됨");
    }

    private void SetupWebSocket()
    {
        if (WebSocketManager.Instance != null)
        {
            WebSocketManager.Instance.OnServerMessage += OnServerMessage;
            WebSocketManager.Instance.OnConnected += OnWebSocketConnected;
        }
        else
        {
            Debug.LogError("WebSocketManager.Instance is null. 이벤트 구독 실패.");
        }
    }

    private void SetupMySessionId()
    {
        if (string.IsNullOrEmpty(MySessionId) || MySessionId == "SESSION_ID_PLACEHOLDER")
        {
            //Debug.LogWarning("MySessionId가 로비에서 설정되지 않았습니다. 임시 ID 사용.");
            MySessionId = "TEMP_LOCAL_PLAYER_ID";
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GamePlay")
        {
            AutoLinkSceneObjects();

            if (firstPersonCamera != null)
            {
                SwitchCamera(firstPersonCamera);
            }

            StartCoroutine(WaitForVoteUIManagerAndLink());
        }

        if (chatInput != null)
        {
            chatInput.onSubmit.RemoveAllListeners();
            chatInput.onSubmit.AddListener(SendChat);
        }
    }

    public void LinkLocalPlayerUI(GameObject localPlayer)
    {
        Camera cam = localPlayer.GetComponentInChildren<Camera>(true);
        if (cam != null)
        {
            firstPersonCamera = cam;
            cam.enabled = true;
        }

        localPlayerObject = localPlayer;

        //Debug.Log($"[GM] 로컬 플레이어 오브젝트 및 카메라 참조 저장 완료.");

    }



    private void AutoLinkSceneObjects()
    {
        if (SceneManager.GetActiveScene().name == "GamePlay")
        {
            observerCamera = GameObject.Find("ObserverCamera")?.GetComponent<Camera>();
            topDownCamera = GameObject.Find("TopDownCamera")?.GetComponent<Camera>();

            judgmentZoomPosition = GameObject.Find("JudgmentZoomPosition")?.transform;
            judgmentFinalPosition = GameObject.Find("JudgmentFinalPosition")?.transform;

            if (topDownCamera != null)
            {
                topDownStartPos = topDownCamera.transform.position;
                topDownCamera.transform.rotation = topDownStartRot;
            }
        }

        Debug.Log("[GM] 씬 오브젝트 자동 연결 복구 완료");
    }

    public IEnumerator WaitForUIManagerAndLink()
    {
        //Debug.Log("🔄 [GM 코루틴] UIManager 준비 대기 및 UI 연결 코루틴 시작.");

        while (UIManager.Instance == null)
        {
            yield return null;
        }

        //Debug.Log("✅ [GM 코루틴] UIManager 준비 완료. UI 연결을 시도합니다.");
        if (localPlayerObject != null)
        {
            UIManager.Instance.LinkLocalPlayerUIElements(localPlayerObject);
            Debug.Log("✔ UIManager에 로컬 플레이어 UI 요소 연결 완료.");
        }

        //Debug.Log("✅ [GM 코루틴] UIManager 준비 완료. 메시지 처리를 기다립니다.");

        if (localPlayerObject != null && systemMessageText != null)
        {
            systemMessageText.text = "게임 플레이 씬 로드 및 UI 연결 대기 완료!";
        }
        else
        {
            Debug.LogWarning("❌ [GM 코루틴] localPlayerObject가 null이어서 UI 연결을 할 수 없습니다.");
        }
    }

    public void OnWebSocketConnected()
    {
        if (systemMessageText != null)
            systemMessageText.text = "서버 연결 성공. 게임 시작 대기 중...";
    }

    public void OnServerMessage(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        MessageWrapper wrapper = JsonUtility.FromJson<MessageWrapper>(json);
        string eventType = wrapper.@event;

        if (string.IsNullOrEmpty(eventType))
        {
            Debug.LogWarning($"[GM] 수신된 메시지에 event 필드가 없습니다: {json}");
            return;
        }

        if (systemMessageText != null)
            systemMessageText.text = $"[서버 수신] 이벤트: {eventType}";

        switch (eventType)
        {
            case "VOTE_PROPOSAL_START":
                if (UIManager.Instance != null)
                    UIManager.Instance.StopGameOverCountdown();
                if (AudioManager.I != null)
                    AudioManager.I.StopJudgmentSfx();
                goto case "TRIAL_RESULT";

            case "VOTE_PROPOSAL_UPDATE":
            case "VOTE_PROPOSAL_FAILED":
            case "TRIAL_START":
            case "TRIAL_VOTE_UPDATE":
            case "TRIAL_RESULT":
                if (VoteManager.Instance != null)
                {
                    Debug.Log($"[GM] Vote 이벤트 전달: {eventType}");
                    VoteManager.Instance.OnVoteEvent(json);
                }
                else
                {
                    Debug.LogError("[GM] VoteManager.Instance가 null이어서 이벤트 전달 실패!");
                }
                break;

            case "GAME_START_TIMER":
                StartCoroutine(CountdownAndLoadGameScene());
                break;

            case "LOAD_GAME_SCENE":
                WebSocketManager.Instance?.SendGameReady();
                break;

            case "SHOW_ORACLE":
                ShowOracleMessage oracleMsg = JsonUtility.FromJson<ShowOracleMessage>(json);
                currentOracle = oracleMsg.data.oracle;
                StartCoroutine(ShowUIAfterLinking(json, "SHOW_ORACLE"));
                break;

            case "SHOW_ROLE":
                ShowRoleMessage roleMsg = JsonUtility.FromJson<ShowRoleMessage>(json);
                myRole = roleMsg.data.role;
                StartCoroutine(ShowUIAfterLinking(json, "SHOW_ROLE"));
                break;

            case "NEXT_ROUND_START":
                break;

            case "RECEIVE_CARDS":
                ReceiveCardsMessage rcMsg = JsonUtility.FromJson<ReceiveCardsMessage>(json);

                if (UIManager.Instance != null && rcMsg.data.slotOwners != null)
                {
                    Dictionary<string, string> slotColorMap = new Dictionary<string, string>();
                    foreach (var owner in rcMsg.data.slotOwners)
                    {
                        slotColorMap[owner.slotType] = owner.playerColor;
                    }
                    UIManager.Instance.UpdateSlotColorsFromRawData(slotColorMap);
                }

                if (RoundManager.Instance != null)
                {
                    RoundManager.Instance.HandleReceiveCards(rcMsg);
                }
                break;

            case "PLAYER_SLOT_ASSIGNMENT":
                PlayerSlotAssignmentMessage slotMsg = JsonUtility.FromJson<PlayerSlotAssignmentMessage>(json);
                HandlePlayerSlotAssignments(slotMsg.data.assignments);
                break;

            case "CARD_SELECTION_CONFIRMED":
                RoundManager.Instance.HandleCardSelectionConfirmed();
                break;

            case "PLAYER_ACTION_UPDATE":
                PlayerActionUpdate msg = JsonUtility.FromJson<PlayerActionUpdate>(json);
                if (players.ContainsKey(msg.playerId))
                    players[msg.playerId].MarkActionCompleted();
                if (systemMessageText != null)
                    systemMessageText.text = $"{msg.playerId}가 행동을 완료했습니다.";
                break;

            case "ALL_CARDS_SELECTED":
                RoundManager.Instance.HandleInterpretationEnd(JsonUtility.FromJson<InterpretationEnd>(json));
                if (RoundManager.Instance != null)
                {
                    RoundManager.Instance.HandleCardSelectionConfirmed();
                }
                break;

            case "ROUND_RESULT":
                Debug.Log($"[DEBUG ROUND_RESULT] FULL JSON: {json}");
                RoundResultResponse response = JsonUtility.FromJson<RoundResultResponse>(json);

                if (response != null && response.data != null)
                {
                    RoundManager.Instance.HandleRoundResult(response.data);
                }
                else
                {
                    Debug.LogError("❌ ROUND_RESULT 파싱 실패: response 또는 data가 null입니다.");
                }
                break;

            case "GAME_OVER":
                Debug.Log($"[GM] GAME_OVER 이벤트 수신. JSON: {json}");
                GameOverMessage gameOverMsg = JsonUtility.FromJson<GameOverMessage>(json);
                if (gameOverMsg != null)
                {
                    HandleGameOver(gameOverMsg.message, gameOverMsg.data.winnerRole);
                }
                else
                {
                    Debug.LogError("❌ GAME_OVER 메시지 파싱 실패.");
                }
                break;


            default:
                Debug.Log($"[GM] 알 수 없는 서버 이벤트 수신: {eventType}");
                break;
        }
    }


    public void HandlePlayerSlotAssignments(List<SlotAssignment> assignments)
    {
        foreach (var assignment in assignments)
        {
            if (players.ContainsKey(assignment.sessionId))
            {
                PlayerManager pm = players[assignment.sessionId];

                pm.slot = assignment.slot;

                Debug.Log($"[GM Assign] Player {assignment.sessionId} assigned slot: {assignment.slot}");
            }
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateSlotColorsFromPlayers();
            Debug.Log("✔ [GM Assign] PLAYER_SLOT_ASSIGNMENT 처리 후 UIManager.UpdateSlotColorsFromPlayers() 호출 완료.");
        }
    }

    public IEnumerator WaitForVoteUIManagerAndLink()
    {

        while (VoteUIManager.Instance == null || VoteUIManager.Instance.gameObject == null)
            yield return null;

        while (VoteManager.Instance == null)
            yield return null;

        VoteManager.Instance.LinkUIManager(VoteUIManager.Instance);

        if (localPlayerObject != null)
            VoteUIManager.Instance.LinkVoteUI(localPlayerObject);

    }


    private IEnumerator ShowUIAfterLinking(string json, string eventType, string mySlotOverride = null, List<string> cardsOverride = null)
    {
        while (UIManager.Instance == null)
        {
            yield return null;
        }

        if (UIManager.Instance.IsUILinked == false && localPlayerObject != null)
        {
            UIManager.Instance.LinkLocalPlayerUIElements(localPlayerObject);
            Debug.Log("✔ UIManager에 로컬 플레이어 UI 요소 연결 완료.");
        }

        ShowRoleMessage roleMsg = null;
        ShowOracleMessage oracleMsg = null;

        PlayerManager localPm = null;
        if (players.ContainsKey(MySessionId))
        {
            localPm = players[MySessionId];
        }

        if (UIManager.Instance != null)
        {
            switch (eventType)
            {
                case "SHOW_ORACLE":
                    oracleMsg = JsonUtility.FromJson<ShowOracleMessage>(json);
                    currentOracle = oracleMsg.data.oracle;
                    UIManager.Instance.ShowOracleAndRole(currentOracle, currentRole, 1);
                    break;

                case "SHOW_ROLE":
                    roleMsg = JsonUtility.FromJson<ShowRoleMessage>(json);
                    currentRole = roleMsg.data.role;    
                    if (localPm != null)
                    {
                        localPm.role = myRole;
                        localPm.godPersonality = roleMsg.data.godPersonality;
                    }

                    UIManager.Instance.ShowOracleAndRole(currentOracle, currentRole, 1);

                    if (roleMsg.data.role.ToLower() == "traitor")
                        UIManager.Instance.ShowTraitorInfo(roleMsg.data.godPersonality);

                    break;
            }
        }
    }


    private IEnumerator CountdownAndLoadGameScene()
    {
        int count = 3;
        while (count > 0)
        {
            if (systemMessageText != null)
                systemMessageText.text = $"게임 시작까지 {count}초";
            yield return new WaitForSeconds(1f);
            count--;
        }

        SceneManager.LoadScene("GamePlay");
    }

    public void AddPlayer(string playerId, PlayerManager player)
    {
        if (!players.ContainsKey(playerId))
            players.Add(playerId, player);
        else
            players[playerId] = player;

        if (player != null)
            player.SetSessionId(playerId);

    }

    public Dictionary<string, PlayerManager> GetPlayers()
    {
        return players;
    }

    public void OnCardSelected(string card)
    {
        if (string.IsNullOrEmpty(MySessionId) || !players.ContainsKey(MySessionId))
        {
            Debug.LogError("[GM] 로컬 플레이어의 Session ID를 찾을 수 없습니다. 카드 전송 실패.");
            return;
        }

        if (cardSelectedCompleted)
        {
            Debug.LogWarning("[GM] 이미 카드를 선택했습니다. 중복 전송 방지.");
            return;
        }

        cardSelectedCompleted = true;

        if (UIManager.Instance != null)
        {
            string slotId = UIManager.Instance.GetSlotIdFromRole(mySlot);
            UIManager.Instance.UpdateMySentenceSlot(slotId, card);
        }

        Debug.Log($"[DEBUG F_1] WebSocket SendCardSelection 호출 시도: {card}");

        WebSocketManager.Instance?.SendCardSelection(card);

        UIManager.Instance.DisableMyCards();
    }

    public void SendChat(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            WebSocketManager.Instance?.SendChat(message);
            if (chatInput != null) chatInput.text = "";
        }
    }

    public void RequestProposeVote(bool agree)
    {
        WebSocketManager.Instance?.SendProposeVote(agree);
    }

    public void RequestCastVote(string targetSessionId)
    {
        WebSocketManager.Instance?.SendCastVote(targetSessionId);
    }

    public void StartJudgmentSequence(RoundResult msg)
    {
        Debug.Log($"[DEBUG F_2] 심판 시퀀스 시작. Displaying Sentence: {msg.fullSentence}");
        StartCoroutine(JudgmentSequence(msg));
    }

    private IEnumerator AnimateCameraTransform(Camera cameraToMove, Transform targetTransform, float duration)
    {
        if (cameraToMove == null || !cameraToMove.enabled || targetTransform == null) yield break;

        Transform camTransform = cameraToMove.transform;
        Vector3 startPos = camTransform.position;
        Quaternion startRot = camTransform.rotation;
        Vector3 targetPos = targetTransform.position;
        Quaternion targetRot = targetTransform.rotation;
        float elapsedTime = 0f;

        while (elapsedTime < duration - 0.001f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            float smoothT = t * t * (3f - 2f * t);

            camTransform.position = Vector3.Lerp(startPos, targetPos, smoothT);
            camTransform.rotation = Quaternion.Slerp(startRot, targetRot, smoothT);
            yield return null;
        }

        camTransform.position = targetPos;
        camTransform.rotation = targetRot;

        Debug.Log($"[DEBUG F_8] Camera Animation completed. Duration: {duration}s. Actual Time: {elapsedTime:F2}s");
    }

    private IEnumerator JudgmentSequence(RoundResult msg)
    {
        // 🌟 서버 요청: ROUND_RESULT 
        const float TotalJudgmentTime = 40.0f;

        // --- 1. 클라이언트 내부 타이밍 설정 (총 45초에 맞춰 조정) ---
        // 카메라 전환 시간 (4.0s)
        float cameraTime = zoomDuration + settleDuration; // 2.0s + 2.0s = 4.0s

        // UI 표시 시간
        float preSentenceWait = 4.0f; 
        float displaySentenceTime = 12.0f; 
        float displayReasonTime = 12.0f; 
        float visualCueTime = 8.0f; 

        // 총 고정 시간 계산
        float totalFixedTime = cameraTime + preSentenceWait + displaySentenceTime + displayReasonTime + visualCueTime; // 4.0 + 4.0 + 12.0 + 12.0 + 10.0 = 42.0s
        float remainingTime = TotalJudgmentTime - totalFixedTime; 

        if (remainingTime < 0)
        {
            Debug.LogError($"[GM] 심판 시퀀스 할당 시간이 {TotalJudgmentTime}초를 초과했습니다! 초과 시간: {Mathf.Abs(remainingTime)}초");
            remainingTime = 0;
        }
        // --- End 타이밍 설정 ---

        if (topDownCamera != null)
        {
            topDownCamera.transform.position = topDownStartPos;
            topDownCamera.transform.rotation = topDownStartRot;
        }

        // 1. 카메라 이동 및 심판 UI 켜기
        SwitchCamera(topDownCamera);

        if (UIManager.Instance.cardSelectionPanel != null)
            UIManager.Instance.cardSelectionPanel.SetActive(false);
        if (UIManager.Instance.toggleCardButton != null)
            UIManager.Instance.toggleCardButton.gameObject.SetActive(false);

        // 1.1 카메라 줌 (2.0s)
        yield return StartCoroutine(AnimateCameraTransform(topDownCamera, judgmentZoomPosition, zoomDuration));

        // 1.2 카메라 정착 (2.0s)
        yield return StartCoroutine(AnimateCameraTransform(topDownCamera, judgmentFinalPosition, settleDuration));

        // 🌟 문장 표시 전 텀 (4.0s)
        yield return new WaitForSeconds(preSentenceWait);

        // 2. 완성된 문장 표시 (1.0s)
        UIManager.Instance.DisplaySentence(msg.fullSentence);
        if (UIManager.Instance.judgmentScroll != null)
            UIManager.Instance.judgmentScroll.SetActive(true);
        yield return new WaitForSeconds(displaySentenceTime);

        // 3. 심판 이유 표시 (8.0s)
        UIManager.Instance.DisplayJudgmentReason(msg.reason);
        yield return new WaitForSeconds(displayReasonTime);

        // 4. 이펙트 및 사운드 재생 (8.0s)
        SwitchCamera(observerCamera);

        VisualCue customCue = new VisualCue();

        if (msg.score < 0)
        {
            customCue.effect = "LIGHTNING";
        }
        else
        {
            customCue.effect = "FLOWER";
        }

        UIManager.Instance.PlayVisualCue(customCue);

        // 이펙트/사운드 재생 시간
        yield return new WaitForSeconds(visualCueTime);


        if (remainingTime > 0)
        {
            Debug.Log($"[GM] 심판 시퀀스 남은 시간 {remainingTime:F2}초 동안 관찰 카메라 유지.");
            yield return new WaitForSeconds(remainingTime);
        }

        // 6. 심판 시퀀스 끝, UI ON
        SwitchCamera(firstPersonCamera);
        if (UIManager.Instance.judgmentScroll != null)
            UIManager.Instance.judgmentScroll.SetActive(false);

        
    }


    public void SwitchCamera(Camera targetCamera)
    {
        bool isFirstPerson = (targetCamera == firstPersonCamera);

        if (firstPersonCamera != null) firstPersonCamera.enabled = isFirstPerson;
        if (observerCamera != null) observerCamera.enabled = (targetCamera == observerCamera);
        if (topDownCamera != null) topDownCamera.enabled = (targetCamera == topDownCamera);

        if (UIManager.Instance != null)
            UIManager.Instance.SetGameUIActive(isFirstPerson);
    }

    public void UpdateVillageHP(int scoreChange)
    {
        currentHP = Mathf.Clamp(currentHP + scoreChange, int.MinValue, 1000);
        Debug.Log($"마을 HP가 {scoreChange}만큼 변경되었습니다. 현재 HP: {currentHP}");
    }


    public void UpdatePlayerConnections(RoomManager.PlayerData[] newPlayers)
    {
        if (newPlayers == null)
        {
            return;
        }

        HashSet<string> connectedSessionIds = new HashSet<string>(
            newPlayers.Select(p => p.sessionId)
        );

        foreach (var playerEntry in players)
        {
            string sessionId = playerEntry.Key;
            PlayerManager playerManager = playerEntry.Value;

            if (!connectedSessionIds.Contains(sessionId))
            {
                if (playerManager != null && playerManager.isConnected)
                {
                    playerManager.MarkDisconnected(); 
                }
            }
        }

    }

    // ============================ GAME OVER LOGIC ===============================

    public void HandleGameOver(string serverMessage, string winnerRole)
    {
        StopAllCoroutines();

        Debug.Log($"[GM] 게임 종료! 서버 메시지: {serverMessage}, 승리 역할: {winnerRole}");
        if (AudioManager.I != null)
        {
            AudioManager.I.PlaySfx(AudioManager.I.gameOverSfx);
        }
        string finalMessage = serverMessage;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOverResult(
                finalMessage,
                SendBackToRoomAction,
                GoToRoomSearchScene
            );
        }
        else
        {
            Debug.LogError("❌ UIManager가 null입니다. 게임 종료 UI를 표시할 수 없습니다.");
            GoToRoomSearchScene();
        }
    }


    public void SendBackToRoomAction()
    {
        Debug.Log("[GM] '현재 방 로비' 복귀 요청 (RoomManager에 BACK_TO_ROOM 액션 위임)");
        if (WebSocketManager.Instance != null && WebSocketManager.Instance.IsConnected)
        {
            WebSocketManager.Instance.SendBackToRoom();
        }
        UIManager.Instance.DisableGameOverButtons();
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OverwriteResultTextToWaitingMessage();
        }
    }

    public void GoToRoomSearchScene()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.StopGameOverCountdown(); // 실행 중인 카운트다운 중지
            UIManager.Instance.HideGameOverUI();       // 게임 오버 패널 숨기기
        }
        Debug.Log("[GM] 룸 서치 씬(RoomSearchScene)으로 이동 시작. 모든 데이터 클리어.");
        if (WebSocketManager.Instance != null && WebSocketManager.Instance.IsConnected)
        {
            WebSocketManager.Instance.SendLeaveRoom();
        }

        // 플레이어 데이터 클리어 (필수)
        players.Clear();
        usedColors.Clear();

        SceneManager.LoadScene("RoomSerachScene");
    }
}