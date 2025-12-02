using UnityEngine;
using TMPro;

public class PlayerNameTag : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;

    public void SetName(string nickname)
    {
        if (nameText != null)
            nameText.text = nickname;
    }
}
