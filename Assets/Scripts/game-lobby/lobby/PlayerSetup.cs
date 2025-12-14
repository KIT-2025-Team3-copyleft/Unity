using UnityEngine;
// 사용하시는 네트워킹 라이브러리를 import 합니다. (예: using Mirror;)

public class PlayerSetup : MonoBehaviour
{
    // 로컬 플레이어에게만 필요한 컴포넌트들
    [SerializeField] private Behaviour[] componentsToDisableForRemote;

    // 플레이어 오브젝트의 카메라
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;

    // 네트워킹 솔루션에 맞게 이 변수를 설정해야 합니다.
    // (예: Mirror의 경우, NetworkBehaviour를 상속받고 isLocalPlayer 속성을 사용합니다.)
    // 현재 예시는 로컬/원격 프리팹을 분리했으므로, isLocalPlayer 플래그를 가져오는 로직이 필요합니다.
    private bool isLocal = false; // PlayerSpawnManager에서 설정할 변수

    private void Start()
    {
        // 1. 네트워킹 isLocalPlayer 상태 확인
        // 만약 이 스크립트를 로컬/원격 프리팹 모두에 넣었다면 아래와 같은 방식으로 동작해야 합니다.

        // **(가장 쉬운 방법: 로컬 프리팹에만 이 스크립트의 필요한 로직을 실행)**
        // PlayerSpawnManager가 localPlayerPrefab을 인스턴스화 할 때 
        // 이 스크립트의 isLocal 변수를 true로 설정하는 코드를 추가합니다.

        if (!isLocal)
        {
            // 원격(Remote) 플레이어인 경우, 로컬에서 제어할 필요가 없는 컴포넌트들을 비활성화
            foreach (Behaviour component in componentsToDisableForRemote)
            {
                component.enabled = false;
            }
            if (playerCamera != null) playerCamera.enabled = false;
            if (audioListener != null) audioListener.enabled = false;
        }
        else
        {
            // 로컬(Local) 플레이어인 경우, 카메라와 오디오 리스너 활성화
            if (playerCamera != null) playerCamera.enabled = true;
            if (audioListener != null) audioListener.enabled = true;

            // 로컬 플레이어에 필요한 컨트롤러 등은 이미 enabled = true 상태일 것입니다.
        }
    }

    // 2. PlayerSpawnManager에서 로컬 여부 설정 함수 추가
    public void SetIsLocal(bool isLocalPlayer)
    {
        this.isLocal = isLocalPlayer;
    }
}