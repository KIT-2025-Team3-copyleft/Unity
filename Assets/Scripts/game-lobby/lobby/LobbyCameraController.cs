using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyCameraController : MonoBehaviour
{
    [Header("Seat Points (Spawn1~Spawn4 같은 자리들)")]
    [SerializeField] private Transform[] seatPoints;

    [Header("카메라 오프셋 (선택)")]
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 0f, 0f);

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();

        // seatPoints 를 인스펙터에서 안 넣었으면 한 번 찾아보기 (선택)
        if ((seatPoints == null || seatPoints.Length == 0))
        {
            var root = GameObject.Find("SpawnRoot");
            if (root != null)
            {
                var list = new System.Collections.Generic.List<Transform>();
                foreach (Transform child in root.transform)
                    list.Add(child);
                seatPoints = list.ToArray();

                Debug.Log($"[LobbyCamera] Auto-filled seatPoints from SpawnRoot → {seatPoints.Length}개");
            }
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (RoomManager.Instance != null)
            RoomManager.Instance.OnLobbyUpdated += OnLobbyUpdated;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (RoomManager.Instance != null)
            RoomManager.Instance.OnLobbyUpdated -= OnLobbyUpdated;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "LobbyScene")
            return;

        ApplyMySeat();
    }

    private void OnLobbyUpdated(RoomManager.Room room)
    {
        if (SceneManager.GetActiveScene().name != "LobbyScene")
            return;

        ApplyMySeat();
    }

    /// <summary>
    /// 내 닉네임을 기준으로 서버에서 내려준 players[]에서 자리를 찾아 카메라를 옮김
    /// </summary>
    private void ApplyMySeat()
    {
        var rm = RoomManager.Instance;
        if (rm == null || rm.CurrentRoom == null)
        {
            Debug.LogWarning("[LobbyCamera] RoomManager / CurrentRoom 없음");
            return;
        }

        if (seatPoints == null || seatPoints.Length == 0)
        {
            Debug.LogWarning("[LobbyCamera] seatPoints 비어 있음");
            return;
        }

        var room = rm.CurrentRoom;
        if (room.players == null || room.players.Length == 0)
        {
            Debug.LogWarning("[LobbyCamera] room.players 없음");
            return;
        }

        string myNick = PlayerPrefs.GetString("PlayerNickname", "Guest");

        // ---- ① 서버 players 배열에서 내 자리 찾기 ----
        int seatIndex = 0;  // 기본값
        bool found = false;

        for (int i = 0; i < room.players.Length; i++)
        {
            var p = room.players[i];
            if (p.nickname == myNick)
            {
                // playerNumber 있으면 그걸 우선, 없으면 배열 index 사용
                seatIndex = p.playerNumber >= 0 ? p.playerNumber : i;
                found = true;
                break;
            }
        }

        if (!found)
        {
            Debug.LogWarning($"[LobbyCamera] 내 닉네임({myNick})을 players에서 못 찾음 → seatIndex=0 사용");
        }

        // ---- ② seatPoints 에서 실제 Transform 가져오기 ----
        Transform seat = seatPoints[seatIndex % seatPoints.Length];
        if (seat == null)
        {
            Debug.LogWarning($"[LobbyCamera] seatPoints[{seatIndex}] 가 null");
            return;
        }

        // 약간 위로/안쪽으로 빼고 싶으면 cameraOffset 조절
        Vector3 finalPos = seat.position + cameraOffset;
        Quaternion finalRot = seat.rotation;

        transform.SetPositionAndRotation(finalPos, finalRot);

        Debug.Log($"[LobbyCamera] myNick={myNick}, seatIndex={seatIndex}, seat={seat.name}, " +
                  $"pos={finalPos}, rot={finalRot.eulerAngles}");
    }
}
