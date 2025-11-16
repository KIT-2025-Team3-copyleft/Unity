using UnityEngine;
using UnityEngine.EventSystems;

public class CardHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string targetSlotId;

    private UIManager uiManager;


    private void Awake()
    {
        if (UIManager.Instance != null)
        {
            uiManager = UIManager.Instance;
        }
    }

    // 마우스 포인터가 버튼 위에 들어왔을 때
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (uiManager != null && !string.IsNullOrEmpty(targetSlotId))
        {
            uiManager.HighlightSlot(targetSlotId, true);
        }
    }

    // 마우스 포인터가 버튼에서 벗어났을 때
    public void OnPointerExit(PointerEventData eventData)
    {
        if (uiManager != null && !string.IsNullOrEmpty(targetSlotId))
        {
            uiManager.HighlightSlot(targetSlotId, false);
        }
    }
}