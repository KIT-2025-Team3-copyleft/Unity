using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public GameObject playerPrefab;
    public Transform[] spawnPoints; 

    // 플레이어 프리펩을 정해진 장소로 소환함
    // 로컬 플레이어 이외의 카메라, 캔버스, 리스너 비활성화
    public PlayerManager SpawnPlayer(string playerId, string nickname, int spawnIndex)
    {
        GameObject playerObj = Instantiate(playerPrefab, spawnPoints[spawnIndex].position, spawnPoints[spawnIndex].rotation);
        PlayerManager pm = playerObj.AddComponent<PlayerManager>();
        pm.playerId = playerId;
        pm.nickname = nickname;
        GameManager.Instance.AddPlayer(playerId, pm);

        bool isLocalPlayer = (playerId == GameManager.Instance.mySlot);
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

        if (isLocalPlayer)
        {
            GameManager.Instance.LinkLocalPlayerUI(playerObj);
        }

        return pm;
    }
}