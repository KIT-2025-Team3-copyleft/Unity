using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
public class CardHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string targetSlotId;
    private UIManager uiManager;

    // 🌟 UIManager가 명시적으로 할당해 줄 참조
    private TMP_Text assignedButtonText;

    // UIManager가 이 함수를 호출하여 필요한 참조를 할당합니다.
    public void Initialize(UIManager manager, TMP_Text textComponent)
    {
        this.uiManager = manager;
        this.assignedButtonText = textComponent;
        // targetSlotId는 UIManager에서 할당됨
    }

    private void Awake()
    {
        // 🌟 Awake에서 인스턴스를 즉시 참조 시도 (Initialize에서 할당을 보장)
        if (UIManager.Instance != null)
        {
            uiManager = UIManager.Instance;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // UIManager 인스턴스가 늦게 설정될 수 있으므로, 매번 확인합니다.
        if (uiManager == null) uiManager = UIManager.Instance;

        // 🌟🌟🌟 수정: assignedButtonText가 null일 경우, 다시 한 번 찾도록 시도
        if (assignedButtonText == null)
        {
            assignedButtonText = GetComponentInChildren<TMP_Text>();
            if (assignedButtonText != null)
                Debug.Log("[Hover Debug] Re-acquired assignedButtonText successfully.");
        }

        // assignedButtonText에 텍스트 값이 있는지 확인
        if (uiManager != null && !string.IsNullOrEmpty(targetSlotId) && assignedButtonText != null)
        {
            uiManager.HighlightSlot(targetSlotId, true, assignedButtonText.text);
            Debug.Log($"[DEBUG 14] 호버 이벤트 발생! Slot: {targetSlotId}, Word: {assignedButtonText.text}");
        }
        else
        {
            // 이 로그가 계속 출력된다면 EventSystem 또는 assignedButtonText의 할당 실패가 원인
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