using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(PlatformPlayerController))]
public class PlayerInputController : MonoBehaviour
{
    PlatformPlayerController m_Player;
    private void Awake()
    {
        m_Player = GetComponent<PlatformPlayerController>();
    }
    public void OnInputMove(InputAction.CallbackContext context)
    {
        m_Player.SetMovement(context.ReadValue<Vector2>());
    }

    public void OnInputJump(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            m_Player.SetJump(true);
        } 
        else if(context.phase == InputActionPhase.Canceled)
        {
            m_Player.SetJump(false);
        } 
    }
}
