using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public GameObject playerPrefab;
    public Transform[] spawnPoints; // 플레이어 4명 자리 좌표(아직 못함)

    // 플레이어 프리펩을 정해진 장소로 소환함
    public PlayerManager SpawnPlayer(string playerId, string nickname, int spawnIndex)
    {
        GameObject playerObj = Instantiate(playerPrefab, spawnPoints[spawnIndex].position, Quaternion.identity);
        PlayerManager pm = playerObj.AddComponent<PlayerManager>();
        pm.playerId = playerId;
        pm.nickname = nickname;
        GameManager.Instance.AddPlayer(playerId, pm); 
        return pm;
    }
}