using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UiButtonSfx : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;

    private Button btn;

    private void Awake()
    {
        btn = GetComponent<Button>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (btn != null && !btn.interactable) return;
        if (AudioManager.I == null) return;
        AudioManager.I.PlaySfx(hoverClip);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (btn != null && !btn.interactable) return;
        if (AudioManager.I == null) return;
        AudioManager.I.PlaySfx(clickClip);
    }
}
