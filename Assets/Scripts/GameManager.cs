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
    public float zoomDuration = 1.2f; // 시간 조정 유지
    public float settleDuration = 1.0f; // 시간 조정 유지

    [Header("Village State")]
    public int currentHP = 100;

    public string PlayerName;
    public int CurrentRoomId;


    // 로컬 플레이어 식별용 ID
    public string MySessionId { get; private set; } = "SESSION_ID_PLACEHOLDER";

    [Header("Player Info")]
    public string myRole;
    public string mySlot;

    public string currentOracle; // 👈 신탁 텍스트를 저장할 변수 추가
    public bool cardSelectedCompleted = false; // 👈 상태 플래그 추가

    public GameObject localPlayerObject { get; private set; }

    private List<string> availableColors = new List<string> { "red", "blue", "green", "yellow", "pink" };
    private List<string> usedColors = new List<string>();
    // Session ID를 키로 PlayerManager를 저장합니다.
    private Dictionary<string, PlayerManager> players = new Dictionary<string, PlayerManager>();

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
        return players.Values.ToList();
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
            Debug.Log($"[GM] 로컬 세션 ID가 서버 ID로 설정됨: {MySessionId}");
        }
    }


    private void Start()
    {
        SetupWebSocket();
        SetupMySessionId();
        Debug.Log("[GM] Start 호출됨");
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
            Debug.LogWarning("MySessionId가 로비에서 설정되지 않았습니다. 임시 ID 사용.");
            MySessionId = "TEMP_LOCAL_PLAYER_ID";
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GamePlay")
        {
            AutoLinkSceneObjects();

            // GamePlayScene 로드 후 1인칭 카메라로 전환
            if (firstPersonCamera != null)
            {
                SwitchCamera(firstPersonCamera);
            }

            StartCoroutine(WaitForVoteUIManagerAndLink());
        }

        // TMP_InputField listener 재연결
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

        Debug.Log($"[GM] 로컬 플레이어 오브젝트 및 카메라 참조 저장 완료.");
        
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
        // LobbyScene에서는 아무것도 찾지 않음.

        Debug.Log("[GM] 씬 오브젝트 자동 연결 복구 완료");
    }

    public IEnumerator WaitForUIManagerAndLink()
    {
        Debug.Log("🔄 [GM 코루틴] UIManager 준비 대기 및 UI 연결 코루틴 시작.");

        while (UIManager.Instance == null)
        {
            yield return null;
        }

        Debug.Log("✅ [GM 코魯틴] UIManager 준비 완료. UI 연결을 시도합니다.");
        while (VoteUIManager.Instance == null || VoteUIManager.Instance.gameObject == null)
        {
            Debug.Log("... VoteUIManager 대기 중 (yield return null) ...");
            yield return null; // 다음 프레임까지 대기
        }

        Debug.Log("[GM 코루틴] VoteUIManager 준비 완료");
        if (localPlayerObject != null)
        {
            UIManager.Instance.LinkLocalPlayerUIElements(localPlayerObject);
            Debug.Log("✔ UIManager에 로컬 플레이어 UI 요소 연결 완료.");
        }

        Debug.Log("✅ [GM 코루틴] UIManager 준비 완료. 메시지 처리를 기다립니다.");

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
            case "VOTE_PROPOSAL_FAILED":
            case "TRIAL_START":
            case "TRIAL_RESULT":
                if (VoteManager.Instance != null)
                {
                    var voteWrapper = JsonUtility.FromJson<VoteMessageWrapper>(json);
                    Debug.Log($"[GM] Vote 이벤트 전달: {voteWrapper.@event}");
                    VoteManager.Instance.OnVoteEvent(voteWrapper);
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
                StartCoroutine(ShowUIAfterLinking(json, "SHOW_ORACLE"));
                break;

            case "SHOW_ROLE":
                ShowRoleMessage roleMsg = JsonUtility.FromJson<ShowRoleMessage>(json);
                myRole = roleMsg.data.role;
                StartCoroutine(ShowUIAfterLinking(json, "SHOW_ROLE"));
                break;

            case "NEXT_ROUND_START":
                RoundStartMessage startMsg = JsonUtility.FromJson<RoundStartMessage>(json);
                StartCoroutine(ShowUIAfterLinking(json, "NEXT_ROUND_START", startMsg.mySlot, startMsg.cards));
                break;

            case "RECEIVE_CARDS":
                ReceiveCardsMessage rcMsg = JsonUtility.FromJson<ReceiveCardsMessage>(json);

                // 🌟 FIX: SlotOwner 정보를 UIManager로 직접 전달하여 캔버스 색상 업데이트
                if (UIManager.Instance != null && rcMsg.data.slotOwners != null)
                {
                    Dictionary<string, string> slotColorMap = new Dictionary<string, string>();
                    foreach (var owner in rcMsg.data.slotOwners)
                    {
                        slotColorMap[owner.slotType] = owner.playerColor;
                    }
                    UIManager.Instance.UpdateSlotColorsFromRawData(slotColorMap);
                }

                StartCoroutine(ShowUIAfterLinking(json, "RECEIVE_CARDS", rcMsg.data.slotType, rcMsg.data.cards));
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

                // 다른 플레이어의 슬롯도 업데이트합니다.
                pm.slot = assignment.slot;

                Debug.Log($"[GM Assign] Player {assignment.sessionId} assigned slot: {assignment.slot}");
            }
        }

        if (UIManager.Instance != null)
        {
            // 모든 플레이어의 슬롯이 업데이트된 후 색상을 업데이트합니다.
            UIManager.Instance.UpdateSlotColorsFromPlayers();
            Debug.Log("✔ [GM Assign] PLAYER_SLOT_ASSIGNMENT 처리 후 UIManager.UpdateSlotColorsFromPlayers() 호출 완료.");
        }
    }

    public IEnumerator WaitForVoteUIManagerAndLink()
    {
        Debug.Log("[GM 코루틴] VoteUIManager 준비 대기 및 UI 연결 시작.");

        // VoteUIManager가 씬에 존재할 때까지 대기
        while (VoteUIManager.Instance == null || VoteUIManager.Instance.gameObject == null)
        {
            Debug.Log("... VoteUIManager 대기 중 (yield return null) ...");
            yield return null;
        }

        Debug.Log("[GM 코루틴] VoteUIManager 준비 완료.");

        if (localPlayerObject != null)
        {
            VoteUIManager.Instance.LinkVoteUI(localPlayerObject);
            Debug.Log("VoteUIManager에 로컬 플레이어 UI 요소 연결 완료.");
        }
        else
        {
            Debug.LogWarning("localPlayerObject가 null이어서 VoteUIManager 연결 실패.");
        }
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

        RoundStartMessage startMsg = null;
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
                    UIManager.Instance.ShowOracleAndRole(currentOracle, "", 1);
                    break;

                case "SHOW_ROLE":
                    roleMsg = JsonUtility.FromJson<ShowRoleMessage>(json);

                    if (localPm != null)
                    {
                        localPm.role = myRole;
                        localPm.godPersonality = roleMsg.data.godPersonality;
                    }

                    UIManager.Instance.ShowOracleAndRole(currentOracle, roleMsg.data.role, 1);

                    if (roleMsg.data.role.ToLower() == "traitor")
                        UIManager.Instance.ShowTraitorInfo(roleMsg.data.godPersonality);

                    yield return new WaitForSeconds(6.0f);
                    break;

                case "NEXT_ROUND_START":
                    startMsg = JsonUtility.FromJson<RoundStartMessage>(json);
                    mySlot = startMsg.mySlot; // GameManager의 mySlot 업데이트

                    if (localPm != null)
                    {
                        localPm.SetRoleAndCards(myRole, mySlot);
                    }

                    UIManager.Instance.ShowOracleAndRole(startMsg.mission, "", startMsg.currentRound);

                    yield return new WaitForSeconds(5.0f);

                    if (RoundManager.Instance != null)
                    {
                        RoundManager.Instance.HandleRoundStart(startMsg);
                    }
                    break;

                case "RECEIVE_CARDS":
                    mySlot = mySlotOverride; // RECEIVE_CARDS는 mySlot을 직접 오버라이드

                    if (localPm != null)
                    {
                        // myRole은 SHOW_ROLE에서 받은 값을 사용
                        localPm.SetRoleAndCards(myRole, mySlot);
                        Debug.Log($"[DEBUG 3] PlayerManager 슬롯 할당 완료: {mySlot}. Color: {localPm.colorName}");
                    }
                    else
                    {
                        Debug.LogError($"[DEBUG 3] localPm이 null이어서 슬롯 할당 실패. mySlot={mySlot}");
                    }

                    if (RoundManager.Instance != null)
                    {
                        RoundStartMessage tempStartMsg = new RoundStartMessage
                        {
                            cards = cardsOverride,
                            mySlot = mySlot,
                            mission = currentOracle,
                            timeLimit = 120,
                            currentRound = 1,
                            // slotColors는 로컬에서 PlayerManager의 colorName을 기반으로 생성됩니다.
                        };

                        RoundManager.Instance.HandleRoundStart(tempStartMsg);
                    }

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
        if (topDownCamera != null)
        {
            topDownCamera.transform.position = topDownStartPos;
            topDownCamera.transform.rotation = topDownStartRot;
        }

        // 🌟 심판 시퀀스 시작 시 UI OFF (SwitchCamera 내부에서 처리)
        SwitchCamera(topDownCamera);

        if (UIManager.Instance.cardSelectionPanel != null)
            UIManager.Instance.cardSelectionPanel.SetActive(false);
        if (UIManager.Instance.toggleCardButton != null)
            UIManager.Instance.toggleCardButton.gameObject.SetActive(false);

        yield return StartCoroutine(AnimateCameraTransform(topDownCamera, judgmentZoomPosition, zoomDuration));
        yield return StartCoroutine(AnimateCameraTransform(topDownCamera, judgmentFinalPosition, settleDuration));

        yield return new WaitForSeconds(1.5f);

        if (UIManager.Instance.judgmentScroll != null)
            UIManager.Instance.judgmentScroll.SetActive(true);

        yield return new WaitForSeconds(5.0f);
        // 🌟 FIX: msg.sentence 대신 msg.fullSentence 사용
        UIManager.Instance.DisplaySentence(msg.fullSentence);
        yield return new WaitForSeconds(5.0f);
        UIManager.Instance.DisplayJudgmentReason(msg.reason);

        yield return new WaitForSeconds(7.0f);

        SwitchCamera(observerCamera);
        //UIManager.Instance.PlayVisualCue(msg.visualCue);
        string score = (msg.score).ToString();
        UIManager.Instance.DisplayJudgmentReason(score);

        yield return new WaitForSeconds(7.0f);

        // 🌟 심판 시퀀스 끝, UI ON (SwitchCamera 내부에서 처리)
        SwitchCamera(firstPersonCamera);
        if (UIManager.Instance.judgmentScroll != null)
            UIManager.Instance.judgmentScroll.SetActive(false);

        yield return new WaitForSeconds(5.0f);
    }

    public void SwitchCamera(Camera targetCamera)
    {
        bool isFirstPerson = (targetCamera == firstPersonCamera);

        if (firstPersonCamera != null) firstPersonCamera.enabled = isFirstPerson;
        if (observerCamera != null) observerCamera.enabled = (targetCamera == observerCamera);
        if (topDownCamera != null) topDownCamera.enabled = (targetCamera == topDownCamera);

        // 🌟 UI 활성화/비활성화 제어
        if (UIManager.Instance != null)
            UIManager.Instance.SetGameUIActive(isFirstPerson);
    }

    public void UpdateVillageHP(int scoreChange)
    {
        currentHP = Mathf.Clamp(currentHP + scoreChange, int.MinValue, 1000);
        Debug.Log($"마을 HP가 {scoreChange}만큼 변경되었습니다. 현재 HP: {currentHP}");
    }


}