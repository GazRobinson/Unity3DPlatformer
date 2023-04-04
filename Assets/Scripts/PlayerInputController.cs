/// <summary>
/// Mario 64 Style Character Controller for Unity
/// 
/// Written by Gaz Robinson, 2023
/// </summary>

using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GaRo
{
    [RequireComponent(typeof(PlatformPlayerController))]
    public class PlayerInputController : MonoBehaviour
    {
        PlatformPlayerController Player;
        private void Awake()
        {
            Player = GetComponent<PlatformPlayerController>();
        }
#if ENABLE_INPUT_SYSTEM
        public void OnInputMove(InputAction.CallbackContext context)
        {
            Player.SetInputDirection(context.ReadValue<Vector2>());
        }

        public void OnInputJump(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Started)
            {
                Player.SetJump(true);
            }
            else if (context.phase == InputActionPhase.Canceled)
            {
                Player.SetJump(false);
            }
        }

        public void OnInputSlowMotion(InputAction.CallbackContext context)
        {
            Time.timeScale = 1.0f - context.ReadValue<float>();
            if (context.phase == InputActionPhase.Canceled)
                Time.timeScale = 1.0f;
        }
#else
        void Update(){
            Vector2 InputDirection = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            Player.SetInputDirection(InputDirection);

            if(Input.GetButtonDown("Jump"))
            {
                Player.SetJump(true);
            }
            if (Input.GetButtonUp("Jump"))
            {
                Player.SetJump(false);
            }
        }
#endif
    }
}
