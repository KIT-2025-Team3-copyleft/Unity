using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Move : MonoBehaviour
{
    public Rigidbody rb;
    public Transform cam;
    private float turn_speed = 3f;

    private void Mouse() //마우스로 캐릭터 회전
    {
        float AngleX = Input.GetAxis("Mouse X") * turn_speed;
        float AngleY = Input.GetAxis("Mouse Y") * turn_speed;
        Vector3 Angle = new Vector3(0f, AngleX, 0f);
        Vector3 cam_Angle = new Vector3(-AngleY, 0f, 0f);
        rb.rotation = rb.rotation * Quaternion.Euler(Angle);
        cam.rotation = cam.rotation * Quaternion.Euler(cam_Angle);
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Mouse();
        }
    }
}
