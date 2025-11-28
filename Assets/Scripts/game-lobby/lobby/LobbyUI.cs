using UnityEngine;
using TMPro;
using Room = RoomManager.Room;


public class LobbyUI : MonoBehaviour
{
    [SerializeField] private GameObject startButton;
    [SerializeField] private TMP_Text playerCountText;

    private void Start()
    {
        // 시작 버튼 활성화는 IsHost 기준
        if (LobbyManager.Instance != null)
            startButton.SetActive(LobbyManager.Instance.IsHost);

        // LobbyManager의 이벤트 구독
        LobbyManager.Instance.OnLobbyUpdated += HandleLobbyUpdate;
    }

    private void OnDestroy()
    {
        if (LobbyManager.Instance != null)
            LobbyManager.Instance.OnLobbyUpdated -= HandleLobbyUpdate;
    }

    private void HandleLobbyUpdate(Room room)
    {
        // 플레이어 수 업데이트
        if (room.players != null)
            playerCountText.text = $"{room.players.Length}/4";

        // 방장이 바뀌었을 수도 있으니 다시 갱신
        startButton.SetActive(LobbyManager.Instance.IsHost);
    }
}
