using UnityEngine;
using TMPro;

public class NickName : MonoBehaviour
{
    public TMP_Text nicknameText;
    private const string NicknameKey = "PlayerNickname";

    void Start()
    {
        // PlayerPrefs에서 닉네임 불러오기
        string nick = PlayerPrefs.GetString(NicknameKey, "Guest");
        nicknameText.text = nick;
    }
}
