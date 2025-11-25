//1.마우스의 움직임에 따라 카메라 각도 조절(130도까지로 제한)
//2.투표기능임시 (서버가 없으므로 임시로 4개 다 투표)
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float mouseSensitivity = 100f; // 마우스 감도

    private float baseYRotation;   // 시작 방향
    private float xRotation = 0f; // x축 회전값
    private float yRotation = 0f; //y축 회전값

    private bool initializedRotation = false; // 최초 회전 보정 완료 여부

    public Transform playerChest;
    public Transform playerBody;

    void Start()
    {
        // 커서 숨기기
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 플레이어의 원래 바라보는 방향 저장
        baseYRotation = playerBody.localEulerAngles.y;
    }

    void LateUpdate()
    {
        // 마우스 입력값
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // 최초 회전이 시작되기 전에는 입력이 0이 아닐 때 초기화
        if (!initializedRotation)
        {
            if (Mathf.Abs(mouseX) > 0.0001f || Mathf.Abs(mouseY) > 0.0001f)
            {
                initializedRotation = true;
                // 바로 회전하지 않고 일단 한 프레임 무시해서 0부터 시작하게 만들기
                return;
            }
            else
            {
                // 움직이지 않으면 정면 고정 유지
                return;
            }
        }

        // 회전 처리
        mouseX *= mouseSensitivity * Time.deltaTime;
        mouseY *= mouseSensitivity * Time.deltaTime;

        // 상하 회전
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        // 좌우 회전
        yRotation += mouseX;
        yRotation = Mathf.Clamp(yRotation, -65f, 65f);
        // 상체, 하체 회전 적용
        playerChest.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.localRotation = Quaternion.Euler(0f, baseYRotation + yRotation, 0f);
    }
}
