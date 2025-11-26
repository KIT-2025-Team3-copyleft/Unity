using UnityEngine;

public class WordChoicePopup : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject panel;  // ÄÑ°í ²ø ÆÐ³Î

    private bool isOpen = false;

    public void TogglePanel()
    {
        isOpen = !isOpen;
        panel.SetActive(isOpen);
    }
}
