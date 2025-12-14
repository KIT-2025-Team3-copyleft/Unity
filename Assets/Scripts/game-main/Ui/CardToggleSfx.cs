using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardToggleSfx : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private AudioClip cardClip;

    private Button btn;

    private void Awake()
    {
        btn = GetComponent<Button>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (btn != null && !btn.interactable) return;
        if (AudioManager.I == null) return;

        AudioManager.I.PlaySfx(cardClip);
    }
}