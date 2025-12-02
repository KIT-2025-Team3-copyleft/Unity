using UnityEngine;

public class PlayerVoteState
{
    public string playerId; //플레이어 고유 ID
    public bool votedStep1 = false; //step1에서의 투표여부
    public bool agree = false; //step1에서의 선택

    public bool votedStep2 = false; //step2에서 투표완료여부
    public int selectedPlayerIndex = -1; // 0~3 / -1 = 미선택
}
