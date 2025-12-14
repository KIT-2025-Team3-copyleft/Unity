using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;


public class Chathandler : MonoBehaviour
{
    public TMP_InputField inputField;
    public RectTransform contentRect;
    public GameObject chatTextPrefab;
    public ScrollRect scrollRect;

    private bool chatActive = false;

    private void Awake()
    {
        // scrollRect 자동 연결 (씬 인스턴스 기준)
        if (scrollRect == null)
            scrollRect = GetComponentInChildren<ScrollRect>(true);

        if (contentRect == null && scrollRect != null)
            contentRect = scrollRect.content;

        // 디버그용: contentRect가 프리팹인지 씬 인스턴스인지 확인
        if (contentRect != null)
        {
            Debug.Log($"[ChatHandler] contentRect scene = '{contentRect.gameObject.scene.name}'");
            // ← 여기서 scene.name 이 ""(빈 문자열)이면 프리팹 에셋, 
            // GamePlay 같이 씬 이름이 나오면 정상.
        }

        // 엔터 입력 처리
        inputField.onSubmit.AddListener(OnSubmitChat);
        // 클릭 처리
        inputField.onSelect.AddListener(OnClickInputField);
    }

    private void OnClickInputField(string text)
    {
        // 채팅 활성화
        ActivateChat();
    }

    void Update()
    {
        // 엔터키로 채팅창 활성화
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (!chatActive)
            {
                ActivateChat();
                return;
            }
        }

        // 외부 UI 클릭시 비활성화
        if (chatActive && Input.GetMouseButtonDown(0))
            TryDeactivateByClick();

        // 입력창 활성화되어있으면 포커스 유지
        if (chatActive && !inputField.isFocused)
            inputField.ActivateInputField();
    }

    private void OnSubmitChat(string text)
    {
        string final = inputField.text.Trim();

        // 입력 내용이 없으면 비활성화
        if (string.IsNullOrEmpty(final))
        {
            DeactivateChat();
            return;
        }

        // 서버로 채팅 전송 (핵심)
        ChatManager.Instance.SendChat(final);

        // 입력 초기화 후 포커스 유지
        inputField.text = "";
        inputField.ActivateInputField();
    }

    private void TryDeactivateByClick()
    {
        PointerEventData ped = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);

        foreach (var r in results)
        {
            if (r.gameObject == inputField.gameObject ||
                r.gameObject.transform.IsChildOf(inputField.transform))
                return;
        }

        DeactivateChat();
    }

    private void ActivateChat()
    {
        chatActive = true;
        inputField.ActivateInputField();

        // 텍스트 끝으로 커서 이동
        inputField.caretPosition = inputField.text.Length;
        inputField.selectionAnchorPosition = inputField.text.Length;
        inputField.selectionFocusPosition = inputField.text.Length;
    }

    private void DeactivateChat()
    {
        chatActive = false;
        inputField.DeactivateInputField();
    }

    // ------------------------------------
    // ChatManager가 호출하는 UI 메서드
    // ------------------------------------
    public void AddChatMessage(string sender, string color, string content)
    {
        if (chatTextPrefab == null || contentRect == null)
        {
            Debug.LogWarning("[ChatHandler] chatTextPrefab 또는 contentRect가 비어 있습니다.");
            return;
        }

        // 부모 없이 먼저 생성
        GameObject newMsg = Instantiate(chatTextPrefab);

        // 그 다음에 부모 설정 (worldPositionStays = false)
        newMsg.transform.SetParent(contentRect, false);

        TMP_Text textComponent = newMsg.GetComponent<TMP_Text>();
        if (textComponent != null)
        {
            textComponent.text = $"<color={color}>{sender}</color>: {content}";
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        scrollRect.verticalNormalizedPosition = 0;
    }

    public void AddSystemMessage(string msg)
    {
        GameObject newMsg = Instantiate(chatTextPrefab, contentRect);
        TMP_Text textComponent = newMsg.GetComponent<TMP_Text>();

        textComponent.text = $"<color=#FF5555>{msg}</color>";

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        scrollRect.verticalNormalizedPosition = 0;
    }

}
