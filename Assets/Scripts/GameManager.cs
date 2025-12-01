using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public TextMeshProUGUI systemMessageText; // 디버그 용도 
    public Button trialButton;
    public InputField chatInput; // TMP InputField로 변경 요망

    [Header("Judgment Animation Positions")]
    public Transform judgmentZoomPosition;  //  줌인 목표 위치
    public Transform judgmentFinalPosition; //  줌인 후 최종 정착 위치
    public float zoomDuration = 0.8f;
    public float settleDuration = 0.7f;

    [Header("Village State")]
    public int currentHP = 100;

    public string PlayerName;
    public int CurrentRoomId;


    [Header("Player Info")]
    public string myRole;
    public string mySlot;

    private List<string> availableColors = new List<string> { "red", "blue", "green", "yellow", "pink" };
    private List<string> usedColors = new List<string>();

    private Dictionary<string, PlayerManager> players = new Dictionary<string, PlayerManager>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        
        // 탑다운 카메라 초기위치 저장
        if (topDownCamera != null)
        {
            topDownStartPos = topDownCamera.transform.position;
            topDownStartRot = topDownCamera.transform.rotation;
        }
    }

   
    public void AssignColorToPlayer(PlayerManager player)
    {
        if (availableColors.Count == 0)
        {
            Debug.LogWarning("모든 색상이 사용되었습니다. 기본 색상 할당");
            player.SetColor("green");
            return;
        }

        int randIndex = UnityEngine.Random.Range(0, availableColors.Count);
        string chosenColor = availableColors[randIndex];

        usedColors.Add(chosenColor);
        availableColors.RemoveAt(randIndex);

        player.SetColor(chosenColor);
    }

    // GameManager.cs 파일 내부 (추가)

    // PlayerSpawner에서 호출되어 로컬 플레이어의 UI/카메라를 매니저에 연결합니다.
    public void LinkLocalPlayerUI(GameObject localPlayerRoot)
    {
        // 1. [카메라 연결]
        Camera fpCam = localPlayerRoot.GetComponentInChildren<Camera>();
        if (fpCam != null)
        {
            firstPersonCamera = fpCam;
        }

        // 2. [UI 연결] UIManager에게 모든 UI 요소 할당을 위임합니다.
        UIManager ui = UIManager.Instance;
        if (ui != null)
        {
            ui.LinkLocalPlayerUIElements(localPlayerRoot);
        }
    }


    public void OnServerMessage(string json)
    {
        string eventType = NetworkMessageHelper.GetEventType(json);

        switch (eventType)
        {
            case "GAME_START":
                StartCoroutine(CountdownAndLoadGameScene());
                break;
            case "ROUND_START":
                RoundManager.Instance.HandleRoundStart(JsonUtility.FromJson<RoundStartMessage>(json));
                break;
            case "CARD_SELECTION_CONFIRMED":
                RoundManager.Instance.HandleCardSelectionConfirmed();
                break;
            case "PLAYER_ACTION_UPDATE":
                PlayerActionUpdate msg = JsonUtility.FromJson<PlayerActionUpdate>(json);
                if (players.ContainsKey(msg.playerId))
                {
                    players[msg.playerId].MarkActionCompleted();
                }
                systemMessageText.text = $"{msg.playerId}가 행동을 완료했습니다."; // debug로 빠질 수 있음
                break;
            case "INTERPRETATION_END":
                RoundManager.Instance.HandleInterpretationEnd(JsonUtility.FromJson<InterpretationEnd>(json));
                break;
            case "ROUND_RESULT":
                RoundManager.Instance.HandleRoundResult(JsonUtility.FromJson<RoundResult>(json));
                break;
        }
    }

    private IEnumerator CountdownAndLoadGameScene()
    {
        int count = 3;
        while (count > 0)
        {
            yield return new WaitForSeconds(1f);
            count--;
        }
    } // 씬 변경 용도, 현재 미사용

    public void AddPlayer(string playerId, PlayerManager player)
    {
        if (!players.ContainsKey(playerId))
        {
            players.Add(playerId, player);
        }
    }

    public void OnCardSelected(string card)
    {
        if (string.IsNullOrEmpty(mySlot))
        {
            Debug.LogError("mySlot이 null이거나 비어있습니다!");
            return;
        }

        if (!players.ContainsKey(mySlot))
        {
            Debug.LogError($"players 딕셔너리에 mySlot({mySlot}) 키가 존재하지 않습니다!");
            return;
        }

        PlayerManager myPlayer = players[mySlot];
        if (myPlayer == null)
        {
            Debug.LogError("해당 슬롯의 PlayerManager가 null입니다!");
            return;
        }

        if (!myPlayer.actionCompleted)
        {
            NetworkManager.Instance?.SendCardSelection(mySlot, card); 
            myPlayer.actionCompleted = true;
            UIManager.Instance?.DisableMyCards(); 
        }
    }



    public void StartJudgmentSequence(RoundResult msg)
    {
        // UIManager에 완성 문장을 미리 전달
        UIManager.Instance.DisplaySentence(msg.finalSentence);

        // 코루틴 시작
        StartCoroutine(JudgmentSequence(msg));
    }

    // 카메라 이동 
    private IEnumerator AnimateCameraTransform(Camera cameraToMove, Transform targetTransform, float duration)
    {
        if (cameraToMove == null || !cameraToMove.enabled) yield break;

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
            float smoothT = t * t * (3f - 2f * t); // EaseInOut

            camTransform.position = Vector3.Lerp(startPos, targetPos, smoothT);
            camTransform.rotation = Quaternion.Slerp(startRot, targetRot, smoothT);
            yield return null;
        }
        camTransform.position = targetPos;
        camTransform.rotation = targetRot;
    }

    // 심판 연출 
    private IEnumerator JudgmentSequence(RoundResult msg)
    {
        // 카메라 전환
        if (topDownCamera != null)
        {
            topDownCamera.transform.position = topDownStartPos;
            topDownCamera.transform.rotation = topDownStartRot;
        }
        SwitchCamera(topDownCamera);

        // 1. 줌인
        yield return StartCoroutine(AnimateCameraTransform(
            topDownCamera, judgmentZoomPosition, zoomDuration
        ));

        // 살짝 돌아오기
        yield return StartCoroutine(AnimateCameraTransform(
            topDownCamera, judgmentFinalPosition, settleDuration
        ));

        // 양피지 UI 활성화
        if (UIManager.Instance.judgmentScroll != null)
        {
            UIManager.Instance.judgmentScroll.SetActive(true);
        }
        yield return new WaitForSeconds(3.0f);

        // 심판 이유 출력
        UIManager.Instance.DisplayJudgmentReason(msg.reason);
        yield return new WaitForSeconds(5.0f);


        // 옵저버 카메라로 전환
        SwitchCamera(observerCamera);

        // 꽃/번개 이펙트 출력
        UIManager.Instance.PlayVisualCue(msg.visualCue);
        yield return new WaitForSeconds(5.0f);

        // 1인칭 카메라로 전환
        SwitchCamera(firstPersonCamera);
        if (UIManager.Instance.judgmentScroll != null)
        {
            UIManager.Instance.judgmentScroll.SetActive(false);
        }
        yield return new WaitForSeconds(10.0f);
    }

    // 카메라 전환 및 UI 제어
    public void SwitchCamera(Camera targetCamera)
    {
        bool isFirstPerson = (targetCamera == firstPersonCamera);

        if (firstPersonCamera != null) firstPersonCamera.enabled = isFirstPerson;
        if (observerCamera != null) observerCamera.enabled = (targetCamera == observerCamera);
        if (topDownCamera != null) topDownCamera.enabled = (targetCamera == topDownCamera);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetGameUIActive(isFirstPerson);
        }
    }

    public void UpdateVillageHP(int scoreChange)
    {
        currentHP = Mathf.Clamp(currentHP + scoreChange, int.MinValue, 10000);

        Debug.Log($"마을 HP가 {scoreChange}만큼 변경되었습니다. 현재 HP: {currentHP}");

    }
}