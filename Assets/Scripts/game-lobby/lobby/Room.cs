using System;

[Serializable]
public class Room
{
    public string roomId;
    public string roomCode;
    public string roomTitle;
    public string status;
    public string hostSessionId;
    public Player[] players;

    public int current_hp;
    public int current_round;
    public string god_personality;
    public bool is_voting_disabled;

    public string currentPhase;
    public string createdAt;

}