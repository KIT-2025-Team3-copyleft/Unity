using UnityEngine;
using TMPro;

public class CounterManager : MonoBehaviour
{
    public static CounterManager Instance;

    public int playerCount = 0;
    public int maxPlayers = 4;

    public TextMeshProUGUI playerCountText;

    void Awake()
    {
        Instance = this;
    }

    // 플레이어가 생성될 때 호출
    public void RegisterPlayer()
    {
        playerCount++;
        UpdateUI();
    }

    // 플레이어가 삭제될 때 호출
    public void UnregisterPlayer()
    {
        playerCount--;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (playerCountText != null)
        {
            playerCountText.text = $"{playerCount}/{maxPlayers}";
        }
    }
}
