using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI References")]
    public Text systemMessageText;
    public InputField chatInput;
    public Button trialButton;
   

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
    }

    public void AddPlayer(string playerId, PlayerManager player)
    {
        if (!players.ContainsKey(playerId))
        {
            players.Add(playerId, player);
        }
    }

    public void OnCardSelected(string card)
    {
        PlayerManager myPlayer = players[mySlot]; 
        if (myPlayer != null && !myPlayer.actionCompleted)
        {
            NetworkManager.Instance.SendCardSelection(mySlot, card);
            myPlayer.actionCompleted = true; 
            UIManager.Instance.DisableMyCards(); 
        }
    }
}