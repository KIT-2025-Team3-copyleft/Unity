using System.Collections;
using UnityEngine;

public class PanelController : MonoBehaviour
{
    public RectTransform panel;
    public RectTransform button;

    public float duration = 0.5f;
    private bool isOpen = false;

    private Vector2 panelOriginPos;
    private Vector2 buttonOriginPos;

    public Vector2 panelOffset = new Vector2(0,0);
    public Vector2 buttonOffset = new Vector2(0,0);

    void Start()
    {
        panelOriginPos = panel.anchoredPosition;
        buttonOriginPos = button.anchoredPosition;
    }

    public void TogglePanel()
    {
        if (isOpen)
        {
            // 닫기 → 원래 위치로
            StartCoroutine(MoveUI(panel, panelOriginPos, duration));
            StartCoroutine(MoveUI(button, buttonOriginPos, duration));
        }
        else
        {
            // 열기 → offset만큼 이동
            StartCoroutine(MoveUI(panel, panelOriginPos - panelOffset, duration));
            StartCoroutine(MoveUI(button, buttonOriginPos - buttonOffset, duration));
        }

        isOpen = !isOpen;
    }

    IEnumerator MoveUI(RectTransform rect, Vector2 targetPos, float time)
    {
        Vector2 start = rect.anchoredPosition;
        float t = 0;

        while (t < time)
        {
            t += Time.deltaTime;
            rect.anchoredPosition = Vector2.Lerp(start, targetPos, t / time);
            yield return null;
        }

        rect.anchoredPosition = targetPos;
    }
}
