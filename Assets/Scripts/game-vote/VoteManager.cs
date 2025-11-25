using System.Collections.Generic;
using UnityEngine;

public class VoteManager : MonoBehaviour
{
    public VoteRequest voteRequestUI;
    public HereticVote heresyVoteUI;

    private List<Player> players = new List<Player>();

    void Start()
    {
        players.Add(new Player(1, "Player1", Color.red));
        players.Add(new Player(2, "Player2", Color.blue));
        players.Add(new Player(3, "Player3", Color.green));
        players.Add(new Player(4, "Player4", Color.yellow));

        voteRequestUI.OnVoteEnd += OnVoteRequestEnd;
        heresyVoteUI.OnHeresyVoteEnd += OnHeresyVoteEnd;

        voteRequestUI.StartVote(players);
    }

    void OnVoteRequestEnd(bool passed)
    {
        Debug.Log("VoteRequest 종료, passed=" + passed);
        if (passed)
        {
            heresyVoteUI.StartVote(players);
        }
    }

    void OnHeresyVoteEnd(List<Player> result)
    {
        Debug.Log("HeresyVote 종료! 결과: " + string.Join(",", result.ConvertAll(p => p.nickname)));
    }
}
