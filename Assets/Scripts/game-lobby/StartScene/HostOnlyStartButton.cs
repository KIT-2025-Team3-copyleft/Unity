using UnityEngine;
using UnityEngine.UI;

public class HostOnlyStartButton : MonoBehaviour
{
    public GameObject startButton;

    private void Start()
    {
        startButton.SetActive(false);

        LobbyManager.Instance.OnLobbyUpdated += room =>
        {
            startButton.SetActive(LobbyManager.Instance.IsHost);
        };
    }
}
