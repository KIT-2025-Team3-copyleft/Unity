using UnityEngine;
using UnityEngine.EventSystems;
using TMPro; 
public class CardHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string targetSlotId;
    private UIManager uiManager;
    private TMP_Text buttonText; 

    private void Awake()
    {
        if (UIManager.Instance != null)
        {
            uiManager = UIManager.Instance;
        }
        buttonText = GetComponentInChildren<TMP_Text>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (uiManager != null && !string.IsNullOrEmpty(targetSlotId) && buttonText != null)
        {
            uiManager.HighlightSlot(targetSlotId, true, buttonText.text);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (uiManager != null && !string.IsNullOrEmpty(targetSlotId))
        {
            uiManager.HighlightSlot(targetSlotId, false, "");
        }
    }
}