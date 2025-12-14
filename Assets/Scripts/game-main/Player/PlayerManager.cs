using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public string playerId; 
    public string nickname;
    public string role;
    public string slot;
    public string godPersonality; 
    public string colorName;
    public bool actionCompleted = false;
    public bool isHost = false;
    public bool isConnected = true;

    private Renderer playerRenderer;

    private void Awake()
    {
        playerRenderer = GetComponent<Renderer>();
        if (playerRenderer == null)
            playerRenderer = GetComponentInChildren<Renderer>();
    }

    public string sessionId { get; private set; }
    public void SetSessionId(string id)
    {
        sessionId = id;
    }
    
    public string GetNickname() => nickname;

    public void SetColor(string colorName)
    {
        this.colorName = colorName;

        Material mat = Resources.Load<Material>(
            $"Polytope Studio/Lowpoly_Characters/Sources/Materials/{colorName}"
        );

        if (mat != null)
            playerRenderer.material = mat;
        else
            Debug.LogWarning($"Material for color '{colorName}' not found!");
    }

    public void SetRoleAndCards(string assignedRole, string assignedSlot)
    {
        role = assignedRole;
        slot = assignedSlot;
        actionCompleted = false;
    }

    public void MarkActionCompleted()
    {
        actionCompleted = true;
    }
    public void MarkDisconnected()
    {
        isConnected = false;
        gameObject.SetActive(false);
    }
}