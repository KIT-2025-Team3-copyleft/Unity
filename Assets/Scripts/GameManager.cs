using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.SceneManagement;

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
    public float zoomDuration = 0.8f;
    public float settleDuration = 0.7f;

    [Header("Village State")]
    public int currentHP = 100;

    public string PlayerName;
    public int CurrentRoomId;


    // 로컬 플레이어 식별용 ID
    public string MySessionId { get; private set; } = "SESSION_ID_PLACEHOLDER";

    [Header("Player Info")]
    public string myRole;
    public string mySlot;

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

        while (UIManager.Instance == null || UIManager.Instance.gameObject == null)
        {
            Debug.Log("... UIManager 대기 중 (yield return null) ...");
            yield return null; // 다음 프레임까지 대기
        }

        Debug.Log("✅ [GM 코루틴] UIManager 준비 완료. UI 연결을 시도합니다.");

        if (localPlayerObject != null)
        {
            // UIManager가 준비되면 UI 연결을 시도합니다.
            // UIManager.Instance.LinkLocalPlayerUIElements(localPlayerObject); 
            // 💡 참고: LinkLocalPlayerUIElements는 ShowUIAfterLinking에서 호출되도록 수정되어야 안전합니다.
            Debug.Log("✔ [GM 코루틴] UIManager 준비 완료. 메시지 처리를 기다립니다.");

            if (systemMessageText != null)
            {
                systemMessageText.text = "게임 플레이 씬 로드 및 UI 연결 대기 완료!";
            }
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
            case "GAME_START_TIMER":
                StartCoroutine(CountdownAndLoadGameScene());
                break;

            case "LOAD_GAME_SCENE":
                WebSocketManager.Instance?.SendGameReady();
                break;

            case "SHOW_ORACLE":
                // SHOW_ORACLE 메시지를 코루틴으로 위임
                StartCoroutine(ShowUIAfterLinking(json, "SHOW_ORACLE"));
                break;

            case "SHOW_ROLE":
                // SHOW_ROLE 메시지를 코루틴으로 위임
                ShowRoleMessage roleMsg = JsonUtility.FromJson<ShowRoleMessage>(json);
                myRole = roleMsg.data.role;
                StartCoroutine(ShowUIAfterLinking(json, "SHOW_ROLE"));
                break;

            case "NEXT_ROUND_START":
                // 새로운 라운드 시작 이벤트를 코루틴으로 위임
                RoundStartMessage startMsg = JsonUtility.FromJson<RoundStartMessage>(json);
                mySlot = startMsg.mySlot;
                StartCoroutine(ShowUIAfterLinking(json, "NEXT_ROUND_START"));
                break;

            case "RECEIVE_CARDS":
                // RECEIVE_CARDS 메시지를 코루틴으로 위임
                StartCoroutine(ShowUIAfterLinking(json, "RECEIVE_CARDS"));
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
                RoundManager.Instance.HandleRoundResult(JsonUtility.FromJson<RoundResult>(json));
                break;

            default:
                Debug.Log($"[GM] 알 수 없는 서버 이벤트 수신: {eventType}");
                break;
        }
    }

    // ----------------------------------------------------
    // 💡 CS0128 오류 해결 및 이벤트 흐름 제어 코루틴
    // ----------------------------------------------------
    private IEnumerator ShowUIAfterLinking(string json, string eventType)
    {
        // 1. UIManager가 생성될 때까지 기다립니다. (NullReferenceException 방지)
        while (UIManager.Instance == null)
        {
            yield return null;
        }

        // 2. UI가 아직 로컬 플레이어에게 연결되지 않았다면 연결합니다. (최초 1회만)
        if (UIManager.Instance.IsUILinked == false && localPlayerObject != null)
        {
            UIManager.Instance.LinkLocalPlayerUIElements(localPlayerObject);
            Debug.Log("✔ UIManager에 로컬 플레이어 UI 요소 연결 완료.");
        }

        // 🌟🌟🌟 [CS0128 오류 해결] 변수를 switch 문 외부에서 선언합니다. 🌟🌟🌟
        RoundStartMessage startMsg = null;
        ShowRoleMessage roleMsg = null;
        ShowOracleMessage oracleMsg = null;
        PlayerManager localPm = null;

        if (players.ContainsKey(MySessionId))
        {
            localPm = players[MySessionId];
        }


        // 3. UIManager가 준비되면 저장된 메시지를 처리합니다.
        if (UIManager.Instance != null)
        {
            switch (eventType)
            {
                case "SHOW_ORACLE":
                    oracleMsg = JsonUtility.FromJson<ShowOracleMessage>(json);
                    UIManager.Instance.ShowOracleAndRole(oracleMsg.data.oracle, "", 1);
                    break;
                case "SHOW_ROLE":
                    roleMsg = JsonUtility.FromJson<ShowRoleMessage>(json);
                    // myRole은 OnServerMessage에서 이미 설정됨

                    // 🌟 [핵심] 로컬 플레이어의 PlayerManager에 역할 할당
                    if (localPm != null)
                    {
                        localPm.role = myRole; // 역할만 업데이트
                        localPm.godPersonality = roleMsg.data.godPersonality; // 신의 성향 업데이트
                    }

                    // 역할만 표시 (오라클은 빈 문자열로 넘김)
                    UIManager.Instance.ShowOracleAndRole("", roleMsg.data.role, 1);
                    if (roleMsg.data.role.ToLower() == "traitor")
                        UIManager.Instance.ShowTraitorInfo(roleMsg.data.godPersonality);

                    yield return new WaitForSeconds(4f); // 역할 정보 확인을 위한 대기 시간
                    break;

                case "NEXT_ROUND_START":
                    startMsg = JsonUtility.FromJson<RoundStartMessage>(json);
                    mySlot = startMsg.mySlot; // GameManager에 슬롯 저장

                    // 🌟 [핵심] myRole과 mySlot을 PlayerManager에 할당
                    if (localPm != null)
                    {
                        // myRole은 SHOW_ROLE에서 설정된 값을 사용합니다.
                        localPm.SetRoleAndCards(myRole, mySlot);
                        Debug.Log($"[GM] 후속 라운드 로컬 플레이어 ({MySessionId}) 슬롯 할당 완료: {mySlot}");
                    }

                    // 🌟🌟🌟 [핵심] 새로운 오라클/미션 표시 (역할은 빈 문자열로 넘김)
                    UIManager.Instance.ShowOracleAndRole(startMsg.mission, "", 1);

                    yield return new WaitForSeconds(3f); // 새로운 오라클/미션을 보여주는 시간 대기

                    // 🌟 [핵심] 카드 선택/라운드 시작 로직 위임
                    if (RoundManager.Instance != null)
                    {
                        // RoundManager는 카드, 타이머 설정 등의 라운드 시작 필수 로직을 수행합니다.
                        RoundManager.Instance.HandleRoundStart(startMsg);
                    }
                    break;
                case "RECEIVE_CARDS": // 카드를 받는 것은 라운드 시작과 동일한 로직으로 처리 (첫 라운드 시작)
                    startMsg = JsonUtility.FromJson<RoundStartMessage>(json);

                    // 🌟 [핵심 수정]: 첫 라운드의 슬롯 정보를 PlayerManager에 할당
                    mySlot = startMsg.mySlot; // GameManager의 mySlot 업데이트
                    if (localPm != null)
                    {
                        // myRole은 SHOW_ROLE에서 설정된 값을 사용합니다.
                        localPm.SetRoleAndCards(myRole, mySlot);
                        Debug.Log($"[GM] 첫 라운드 로컬 플레이어 ({MySessionId}) 슬롯 할당 완료: {mySlot}");
                    }

                    if (RoundManager.Instance != null)
                    {
                        // RoundManager는 mySlot을 GameManager.Instance.mySlot에 저장하고, 라운드를 시작합니다.
                        RoundManager.Instance.HandleRoundStart(startMsg);
                    }
                    break;
            }
        }
    }
    // ----------------------------------------------------


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

        // 씬 로드 요청 (로비 씬을 GamePlay 씬으로)
        SceneManager.LoadScene("GamePlay");
    }

    public void AddPlayer(string playerId, PlayerManager player)
    {
        if (!players.ContainsKey(playerId))
            players.Add(playerId, player);
        else
            players[playerId] = player;
    }

    public void OnCardSelected(string card)
    {
        // Session ID를 사용하여 로컬 플레이어의 존재 여부를 확인합니다.
        if (string.IsNullOrEmpty(MySessionId) || !players.ContainsKey(MySessionId))
        {
            Debug.LogError("[GM] 로컬 플레이어의 Session ID를 찾을 수 없습니다. 카드 전송 실패.");
            return;
        }

        // 여기서 mySlot 변수는 RoundManager.HandleRoundStart에서 설정된 값을 사용합니다.

        WebSocketManager.Instance?.SendCardSelection(card);
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
        UIManager.Instance.DisplaySentence(msg.finalSentence);
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

        while (elapsedTime < duration)
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
    }

    private IEnumerator JudgmentSequence(RoundResult msg)
    {
        if (topDownCamera != null)
        {
            topDownCamera.transform.position = topDownStartPos;
            topDownCamera.transform.rotation = topDownStartRot;
        }

        SwitchCamera(topDownCamera);

        yield return StartCoroutine(AnimateCameraTransform(topDownCamera, judgmentZoomPosition, zoomDuration));
        yield return StartCoroutine(AnimateCameraTransform(topDownCamera, judgmentFinalPosition, settleDuration));

        if (UIManager.Instance.judgmentScroll != null)
            UIManager.Instance.judgmentScroll.SetActive(true);

        yield return new WaitForSeconds(3f);
        UIManager.Instance.DisplayJudgmentReason(msg.reason);
        yield return new WaitForSeconds(5f);

        SwitchCamera(observerCamera);
        UIManager.Instance.PlayVisualCue(msg.visualCue);
        yield return new WaitForSeconds(5f);

        SwitchCamera(firstPersonCamera);
        if (UIManager.Instance.judgmentScroll != null)
            UIManager.Instance.judgmentScroll.SetActive(false);

        // 🌟 다음 라운드 시작을 위해 서버 메시지를 기다립니다.
        yield return new WaitForSeconds(10f);
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
        currentHP = Mathf.Clamp(currentHP + scoreChange, int.MinValue, 10000);
        Debug.Log($"마을 HP가 {scoreChange}만큼 변경되었습니다. 현재 HP: {currentHP}");
    }
}