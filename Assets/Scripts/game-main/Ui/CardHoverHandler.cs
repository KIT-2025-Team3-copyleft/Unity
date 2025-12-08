using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
public class CardHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string targetSlotId;
    private UIManager uiManager;

    private TMP_Text assignedButtonText;

    public void Initialize(UIManager manager, TMP_Text textComponent)
    {
        this.uiManager = manager;
        this.assignedButtonText = textComponent;
    }

    private void Awake()
    {
        if (UIManager.Instance != null)
        {
            uiManager = UIManager.Instance;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (uiManager == null) uiManager = UIManager.Instance;

        if (assignedButtonText == null)
        {
            assignedButtonText = GetComponentInChildren<TMP_Text>();
            if (assignedButtonText != null)
                Debug.Log("[Hover Debug] Re-acquired assignedButtonText successfully.");
        }

        if (uiManager != null && !string.IsNullOrEmpty(targetSlotId) && assignedButtonText != null)
        {
            uiManager.HighlightSlot(targetSlotId, true, assignedButtonText.text);
        }
        else
        {
            Debug.LogWarning($"[Hover Fail] targetSlotId={targetSlotId}, TextIsNull={assignedButtonText == null}, UIManagerIsNull={uiManager == null}");
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (uiManager == null) uiManager = UIManager.Instance;

        if (uiManager != null && !string.IsNullOrEmpty(targetSlotId))
        {
            uiManager.HighlightSlot(targetSlotId, false, "");
        }
    }
}