// 현재 미사용

/*using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance;

    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    // 🌟 추가: 현재 활성화된 플레이어 목록 (sessionId 기준)
    public Dictionary<string, PlayerManager> activePlayers = new Dictionary<string, PlayerManager>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // 🌟 추가: 로비 플레이어 모두 제거
    public void ClearExistingPlayers()
    {
        foreach (var playerEntry in activePlayers)
        {
            Destroy(playerEntry.Value.gameObject);
        }
        activePlayers.Clear();
    }

    // 🌟 추가: RoomManager로부터 호출되어 로비 플레이어를 업데이트함
    public void UpdateLobbyPlayers(Player[] players, string hostSessionId)
    {
        ClearExistingPlayers();

        string mySessionId = GameManager.Instance.MySessionId; // GameManager에 저장된 내 Session ID
        int spawnIndex = 0;

        foreach (var playerInfo in players)
        {
            if (spawnIndex >= spawnPoints.Length)
            {
                Debug.LogWarning("스폰 포인트가 부족합니다. 이 플레이어는 스폰되지 않습니다.");
                break;
            }

            // SpawnPlayer를 사용하여 플레이어 생성
            PlayerManager newPlayer = SpawnPlayer(
                playerInfo.sessionId,
                playerInfo.nickname,
                spawnIndex
            );

            // 로비 정보 업데이트
            newPlayer.SetColor(playerInfo.color);
            newPlayer.isHost = (playerInfo.sessionId == hostSessionId);

            // 로컬 플레이어 체크 및 UI/카메라 연결
            bool isLocal = (playerInfo.sessionId == mySessionId);
            if (isLocal)
            {
                GameManager.Instance.LinkLocalPlayerUI(newPlayer.gameObject);
            }

            activePlayers.Add(playerInfo.sessionId, newPlayer);

            spawnIndex++;
        }
    }


    // 플레이어 프리펩을 정해진 장소로 소환함
    // 로컬 플레이어 이외의 카메라, 캔버스, 리스너 비활성화
    public PlayerManager SpawnPlayer(string playerId, string nickname, int spawnIndex)
    {
        GameObject playerObj = Instantiate(playerPrefab, spawnPoints[spawnIndex].position, spawnPoints[spawnIndex].rotation);
        PlayerManager pm = playerObj.AddComponent<PlayerManager>();
        pm.playerId = playerId;
        pm.nickname = nickname;
        GameManager.Instance.AddPlayer(playerId, pm);

        // 🌟 수정: mySlot 대신 MySessionId로 로컬 플레이어 체크
        bool isLocalPlayer = (playerId == GameManager.Instance.MySessionId);

        AudioListener listener = playerObj.GetComponentInChildren<AudioListener>(true);
        Transform canvasTransform = playerObj.transform.Find("Canvas");
        Camera cam = playerObj.GetComponentInChildren<Camera>(true);

        if (cam != null)
        {
            if (isLocalPlayer)
            {
                cam.enabled = true;
                GameManager.Instance.firstPersonCamera = cam;
            }
            else
            {
                cam.enabled = false;
            }
        }
        if (canvasTransform != null)
        {
            if (!isLocalPlayer)
            {
                // 원격 플레이어의 캔버스는 비활성화
                canvasTransform.gameObject.SetActive(false);
            }
        }

        if (listener != null)
        {
            if (!isLocalPlayer)
            {
                Destroy(listener);
            }
        }

        // 🌟 LinkLocalPlayerUI 호출은 UpdateLobbyPlayers에서 로컬 체크 후 처리합니다.
        *//*
        if (isLocalPlayer)
        {
            GameManager.Instance.LinkLocalPlayerUI(playerObj);
        }
        *//*

        return pm;
    }
}*/