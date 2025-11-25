using UnityEngine;

[System.Serializable]
public class Player
{
    public int id;
    public string nickname;
    public Color color;
    public bool hasVoted = false;

    public GameObject playerObject;   // 실제 플레이어 오브젝트
    public Light highlightLight;      // 투표 조명

    public Player(int id, string name, Color color)
    {
        this.id = id;
        nickname = name;
        this.color = color;
    }
}
