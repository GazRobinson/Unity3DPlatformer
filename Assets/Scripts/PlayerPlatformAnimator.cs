/// <summary>
/// Animator hooks for PlatformPlayerController
/// 
/// Written by Gaz Robinson, 2023
/// </summary>

using UnityEngine;

namespace GaRo {
    [RequireComponent(typeof(CharacterController))]
    public class PlayerPlatformAnimator : MonoBehaviour
    {
        [SerializeField] private Animator Animator = null;
        private PlatformPlayerController Player = null;

        void Awake()
        {
            //Get the attached components
            Player = GetComponent<PlatformPlayerController>();
            Animator = transform.GetChild(0).GetComponent<Animator>();

            Player.OnJump += HandleJump;
        }

        // Update is called once per frame
        void Update()
        {
            PlayerState State = Player.GetStateInfo;

            Animator.SetFloat("Speed", Vector3.ProjectOnPlane(State.FinalVelocity, Vector3.up).magnitude);
            Animator.SetBool("Grounded", State.IsGrounded);
        }

        void HandleJump()
        {
            Animator.SetTrigger("Jump");
        }
    }
}
