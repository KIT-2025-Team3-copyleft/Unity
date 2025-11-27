using UnityEngine;

public class SafeSpawnHandler : MonoBehaviour
{
    public static SafeSpawnHandler Instance;
    public int MyPlayerNumber;        // 1부터 시작
    public string mySessionId;
    public Room currentRoom;

    public delegate void SpawnReady();
    public static event SpawnReady OnMySpawnReady;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;
    }

    void OnEnable() => OnMySpawnReady += SpawnMyPlayer;
    void OnDisable() => OnMySpawnReady -= SpawnMyPlayer;

    public void TriggerSpawn() => OnMySpawnReady?.Invoke();

    private void SpawnMyPlayer()
    {
        // 필수 컴포넌트 체크
        if (SpawnManager.Instance == null)
        {
            Debug.LogError("SpawnManager is missing!");
            return;
        }
        if (PlayerSpawnManager.Instance == null)
        {
            Debug.LogError("PlayerSpawnManager is missing!");
            return;
        }
        if (RoomManager.Instance == null || RoomManager.Instance.playerPrefab == null)
        {
            Debug.LogError("playerPrefab is missing!");
            return;
        }
        if (currentRoom == null)
        {
            Debug.LogWarning("currentRoom is null!");
            return;
        }

        // 스폰 인덱스 안전 처리
        int index = Mathf.Max(MyPlayerNumber - 1, 0);
        Vector3 spawnPos = SpawnManager.Instance.GetSpawnPosition(index);
        Debug.Log($"Spawning player at index {index}, position {spawnPos}");

        // 이미 플레이어가 있는지 확인
        GameObject player = PlayerSpawnManager.Instance.GetPlayerObject(mySessionId);
        if (player == null)
        {
            player = Instantiate(RoomManager.Instance.playerPrefab, spawnPos, Quaternion.identity);
            PlayerSpawnManager.Instance.RegisterPlayer(mySessionId, player);
            Debug.Log("Player instantiated and registered.");
        }
        else
        {
            player.transform.position = spawnPos;
            Debug.Log("Player already exists, position updated.");
        }
    }

}
