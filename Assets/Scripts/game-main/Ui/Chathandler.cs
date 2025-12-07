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
        if (Keyboard.current == null) return;

        // 엔터키로 채팅창 활성화
        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            if (!chatActive)
            {
                ActivateChat();
                return;
            }

        }

        // 외부 UI 클릭시 비활성화
        if (chatActive && Mouse.current.leftButton.wasPressedThisFrame)
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
            position = Mouse.current.position.ReadValue()
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
        GameObject newMsg = Instantiate(chatTextPrefab, contentRect);
        TMP_Text textComponent = newMsg.GetComponent<TMP_Text>();

        textComponent.text = $"<color={color}>{sender}</color>: {content}";

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
