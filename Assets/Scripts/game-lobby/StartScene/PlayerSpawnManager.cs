using UnityEngine;

public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance;

    public Transform[] spawnPoints; // 0 = origin1 ...

    private void Awake()
    {
        Instance = this;
    }

    public GameObject SpawnLocalPlayer(int playerNumber, GameObject playerPrefab)
    {
        int index = playerNumber - 1;

        if (index < 0 || index >= spawnPoints.Length)
        {
            Debug.LogError("스폰 포인트 인덱스 오류!");
            return null;
        }

        return Instantiate(playerPrefab,
                           spawnPoints[index].position,
                           spawnPoints[index].rotation);
    }
}
