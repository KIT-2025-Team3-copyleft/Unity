using UnityEngine;

public class ExitGame : MonoBehaviour
{
    public void OnClickExit()
    {
        Debug.Log("게임 종료 시도됨");

#if UNITY_EDITOR
        
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
