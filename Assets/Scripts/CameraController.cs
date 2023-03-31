using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform m_FollowTarget = null;
    public Transform m_LookAtTarget = null;
    public Vector3 m_Offset = Vector3.zero;
    public float m_Distance = 5.0f;

    public float m_Sensitivity = 5.0f;
    public bool m_InvertHorizontal = false;
    public bool m_InvertVertical = true;

    private float m_Yaw = 0.0f;
    private float m_Pitch = 0.0f;

    private float m_CameraDegrees = 180.0f;

    private Vector2 m_CameraInput = Vector2.zero;

    public void SetInput(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        m_CameraInput = context.ReadValue<Vector2>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        //Reset
        transform.position = m_FollowTarget.position + m_Offset;
        transform.rotation = Quaternion.identity;

        m_Yaw = Mathf.Repeat(m_Yaw + m_CameraInput.x * Time.deltaTime * m_Sensitivity * m_CameraDegrees * (m_InvertHorizontal ? -1.0f : 1.0f), 360.0f);
        m_Pitch = Mathf.Clamp(m_Pitch + m_CameraInput.y * Time.deltaTime * m_Sensitivity * m_CameraDegrees * (m_InvertVertical ? -1.0f : 1.0f), -89.0f, 89.0f);

        transform.rotation = Quaternion.AngleAxis(m_Yaw, Vector3.up) * Quaternion.AngleAxis(m_Pitch, transform.right);
        transform.Translate(Vector3.back * m_Distance, Space.Self);
        transform.LookAt(m_LookAtTarget);
    }
}
