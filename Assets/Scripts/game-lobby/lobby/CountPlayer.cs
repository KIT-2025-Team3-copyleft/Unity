using UnityEngine;

public class CountPlayer : MonoBehaviour
{
    void Start()
    {
        // 플레이어 생성 시 자동 등록
        CounterManager.Instance.RegisterPlayer();
    }

    void OnDestroy()
    {
        // 플레이어가 제거될 경우 카운트 감소
        if (CounterManager.Instance != null)
            CounterManager.Instance.UnregisterPlayer();
    }
}
