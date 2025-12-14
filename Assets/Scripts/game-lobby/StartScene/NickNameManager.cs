using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic; // Dictionary 대신 DTO 사용
using UnityEngine.SceneManagement; // 사용하지 않는 using은 제거 가능
using TMPro; // 사용하지 않는 using은 제거 가능

// DTO: 서버로 닉네임 설정을 요청할 때 사용하는 데이터 구조
[System.Serializable]
public class NicknamePayload
{
    public string nickname;
}

[System.Serializable]
public class NicknameRequest
{
    public string action = "SET_NICKNAME";
    public NicknamePayload payload;
}

// DTO: 서버로부터 닉네임 성공 시 응답받는 Player 정보 구조
[System.Serializable]
public class ServerPlayer
{
    // 서버 응답 JSON의 필드 이름과 일치해야 합니다.
    public string nickname;
    // 필요한 경우 다른 필드 (e.g., id) 추가 가능
}

// DTO: 서버로부터 받은 모든 메시지의 최상위 구조
[System.Serializable]
public class ServerMessage
{
    // 'event' 키워드는 C#에서 예약어이므로, JSON 필드가 "event"라면
    // 필드 이름을 @event로 사용합니다.
    public string @event;
    public ServerPlayer player; // NICKNAME_SUCCESS 시 사용
    public string message; // NICKNAME_FAIL 시 메시지 전송용
}


public class NickNameManager : MonoBehaviour
{
    public static NickNameManager Instance;

    private const string NicknameKey = "PlayerNickname";

    public event Action<string> OnNicknameSuccess;
    public event Action<string> OnNicknameFail;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // OnDestroy에서 해지할 수 있도록, WebSocketManager가 준비될 때까지 기다립니다.
        StartCoroutine(RegisterWebSocketEvent());
    }

    void OnEnable()
    {
        if (WebSocketManager.Instance != null)
            WebSocketManager.Instance.OnServerMessage += HandleEvent;
    }

    // ⭐ 중요: 컴포넌트 파괴 시 이벤트 구독을 반드시 해지해야 메모리 누수를 방지합니다.
    void OnDestroy()
    {
        if (WebSocketManager.Instance != null)
        {
            WebSocketManager.Instance.OnServerMessage -= HandleEvent;
            Debug.Log("[NickNameManager] WebSocket 구독 해지 완료");
        }
    }

    IEnumerator RegisterWebSocketEvent()
    {
        // WebSocketManager의 인스턴스가 준비될 때까지 대기
        while (WebSocketManager.Instance == null)
            yield return null;

        // 중복 구독 방지 및 구독
        WebSocketManager.Instance.OnServerMessage -= HandleEvent;
        WebSocketManager.Instance.OnServerMessage += HandleEvent;

        Debug.Log("[NickNameManager] WebSocket 구독 완료");
    }


    public void SendNickname(string nickname)
    {
        // DTO를 사용하여 요청 데이터 생성
        var request = new NicknameRequest
        {
            payload = new NicknamePayload { nickname = nickname }
        };

        // JsonUtility를 사용하여 JSON 직렬화
        string json = JsonUtility.ToJson(request);

        Debug.Log("[WS SEND] " + json);

        if (WebSocketManager.Instance != null && WebSocketManager.Instance.IsConnected)
            WebSocketManager.Instance.Send(json);
        else
            Debug.LogWarning("[NickNameManager] WebSocket 연결 안됨");
    }

    private void HandleEvent(string json)
    {
        try
        {
            // 공통 파싱
            var serverMsg = JsonUtility.FromJson<ServerMessage>(json);

            if (serverMsg == null)
            {
                Debug.LogWarning("[NickNameManager] serverMsg null: " + json);
                return;
            }

            switch (serverMsg.@event)
            {
                case "NICKNAME_SUCCESS":
                    if (serverMsg.player != null)
                    {
                        SaveAndNotify(serverMsg.player.nickname);
                    }
                    else
                    {
                        Debug.LogWarning("[NickNameManager] NICKNAME_SUCCESS지만 player 정보 없음");
                    }
                    break;

                case "NICKNAME_FAIL":
                case "NICKNAME_DUPLICATE":
                    // 서버에서 내려준 message를 그대로 사용
                    var msg = string.IsNullOrEmpty(serverMsg.message)
                        ? "닉네임 설정 실패"
                        : serverMsg.message;

                    Debug.Log("[NickNameManager] 닉네임 실패/중복: " + msg);
                    OnNicknameFail?.Invoke(msg);
                    break;

                default:
                    Debug.Log("[NickNameManager] 무시 이벤트: " + json);
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[NickNameManager] JSON 파싱 오류: {e.Message}\n수신 JSON: {json}");
        }
    }

    private void SaveAndNotify(string nickname)
    {
        PlayerPrefs.SetString(NicknameKey, nickname);
        PlayerPrefs.Save();

        Debug.Log("[NickNameManager] 닉네임 성공 처리: " + nickname);

        // 닉네임만 처리하면 NickNameManager 는 더 이상 서버 이벤트를 받을 필요 없음
        if (WebSocketManager.Instance != null)
        {
            WebSocketManager.Instance.OnServerMessage -= HandleEvent;
            Debug.Log("[NickNameManager] 닉네임 설정 완료 → WebSocket 구독 해제");
        }

        OnNicknameSuccess?.Invoke(nickname);
    }
}