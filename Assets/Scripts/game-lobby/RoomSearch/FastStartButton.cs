using UnityEngine;

public class FastStartButton : MonoBehaviour
{
    public void OnClickFastStart()
    {
        if (RoomManager.Instance != null)
        {
            Debug.Log("[FastStartButton] QUICK_JOIN 요청");
            RoomManager.Instance.RequestQuickJoin();
        }
        else
        {
            Debug.LogError("[FastStartButton] RoomManager.Instance = null");
        }
    }
}
