using UnityEngine;

/// <summary>
/// Mario 64 Style Character Controller for Unity
/// 
/// Written by Gaz Robinson, 2023
/// </summary>
namespace GaRo
{
    public struct PlayerState
	{
        public Vector2 InputDirection;
        
        public bool IsGrounded;
        public bool IsSliding;

        public Vector3 Velocity;
        public Vector3 FinalVelocity;
        public float ForwardVelocity;
        public float SideVelocity;

        public Vector3 ContactPosition;
        public Vector3 ShadowPosition;
        public Vector3 GroundNormal;

        public bool JumpHeld;
	}
    [RequireComponent(typeof(CharacterController))]
    public class PlatformPlayerController : MonoBehaviour
    {
        public float GroundSpeed = 4.0f;
        public float GroundAcceleration = 8.0f;
        public float MaxRotationDelta = 10.0f;

        public float JumpHeight = 2.0f;
        [Tooltip("How much forward velocity should be translated into vertical jump velocity?\nThis will reduce the forward velocity by the same percentage.")]
        public float ForwardVelocityJumpInfluence = 0.25f;
        public float AirSpeed = 2.0f;
        public float AirAcceleration = 4.0f;
        public float AirDrag = 2.0f;

        [Tooltip("How much should gravity be increased if the player releases the jump button early.")]
        public float ShortHopMultiplier = 2.0f;
        public float SlopeCheckThreshold = 0.75f;

        public bool UseUnityGravity = true;
        public float CustomGravity = -19.82f;

        //Info
        private Vector3 Velocity = Vector3.zero;
        private float ForwardVelocity = 0.0f;
        private bool IsGrounded = false;
        private bool IsSliding = false;

        //Input
        private Vector2 InputDirection = Vector2.zero;

        //Has the jump button been pressed
        private bool JumpTrigger = false;

        //Is the jump button being held
        private bool JumpHeld = false;

        private CharacterController CharController = null;
        private Animator Animator = null;
        private Camera PlayerCamera = null;

        public PlayerState GetStateInfo
		{
			get { return StateInfo; }
		}
        private PlayerState StateInfo;

        public void SetInputDirection(Vector2 inputDirection)
        {
            InputDirection = inputDirection;
        }

        public void SetJump(bool jumpState)
        {
            JumpTrigger |= jumpState;
            JumpHeld = jumpState;
        }

        void Awake()
        {
            //Get the attached components
            CharController = GetComponent<CharacterController>();
            Animator = transform.GetChild(0).GetComponent<Animator>();
            PlayerCamera = Camera.main;
        }

        // Update is called once per frame
        void Update()
        {
            StateInfo = new PlayerState();
            //Velocity = CharController.velocity;
            //Check to see if we're grounded
            RaycastHit HitInfo;
            Vector3 GroundNormal = Vector3.up;
            float raycastDistanceMultiplier = 0.6f;


            bool AboveGround = Physics.Raycast(transform.position, Vector3.down, out HitInfo, CharController.height*2.5f);
			if (AboveGround)
			{
                StateInfo.ShadowPosition = HitInfo.point;
            }
			else
			{
                StateInfo.ShadowPosition = transform.position -
                (Vector3.up * CharController.height * 0.5f);
            }
            
            if (Physics.SphereCast(transform.position, CharController.radius, Vector3.down, out HitInfo, CharController.height * raycastDistanceMultiplier - CharController.radius))
            {

                GroundNormal = HitInfo.normal;
                StateInfo.ContactPosition = HitInfo.point;
                StateInfo.GroundNormal = GroundNormal;

                //Only become grounded if we're not already grounded and we're falling.
                //This avoids becoming grounded when jumping over a ledge
                if (/*!IsGrounded &&*/ Velocity.y <= 0)
                {
                    Vector3 checkVec = ((HitInfo.distance + CharController.radius) - (CharController.height * 0.5f)) * Vector3.down;

                    CharController.Move(checkVec);
                    if (Vector3.Angle(GroundNormal, Vector3.up) < CharController.slopeLimit)
					{
                        IsGrounded = true;
                        Velocity.y = 0.0f;
                        IsSliding = false;
                    }
					else
                    {
                        IsGrounded = false;
                        IsSliding = true;
                    }
                }
            }
            else
            {
                //We might have walked off an edge
                /*if (Physics.SphereCast(transform.position, CharController.radius, Vector3.down, out HitInfo, CharController.height * SlopeCheckThreshold - CharController.radius))
                {
                    GroundNormal = HitInfo.normal;
                   // CharController.Move((HitInfo.distance - (CharController.height * 0.5f)) * Vector3.down);
                }
                else*/
                {
                    //print("Ungrounded!");
                    IsGrounded = false;
                    IsSliding = false;
                }
            }

            //Vertical resolution - Apply gravity and check for a jump
            if (!IsGrounded || (IsGrounded && IsSliding))
            {
                float grav = UseUnityGravity ? Physics.gravity.y : CustomGravity;

                //Gravity is increased if the player is moving up but not holding the jump button
                //This lets the play do short hops
                //This won't play nice as-is if you launch the player somehow
                if (Velocity.y > 0.0f && !JumpHeld)
                {
                    grav *= ShortHopMultiplier;
                }
                Velocity.y += grav * Time.deltaTime;
            }
            else
            {
                if (JumpTrigger)
                {
                    PerformJump();
                }
            }
            //Reset the jump trigger, so we cannot queue up an air jump
            JumpTrigger = false;

            //Calculate which way "forward" is, relative to the camera
            Vector3 ViewForward = Vector3.ProjectOnPlane(PlayerCamera.transform.forward, Vector3.up).normalized;

            //Calculate which way the player is trying to go
            Vector3 TargetDirection = (ViewForward * InputDirection.y + PlayerCamera.transform.right * InputDirection.x).normalized;
            Vector3 ProjectedDirection = Vector3.ProjectOnPlane(TargetDirection, GroundNormal).normalized;
            
            //How fast does the player want to go this Update?
            float TargetMagnitude;
            if (IsGrounded)
            {
                //TODO: You go faster uphill if you walk at an angle
                TargetMagnitude = GroundSpeed * InputDirection.magnitude;
                float slope = Vector3.Dot(transform.right, (Vector3.Cross(Vector3.up, GroundNormal)));
              //  TargetMagnitude += TargetMagnitude * slope;
                //* Mathf.Clamp01(1.0f -  Vector3.Dot(Vector3.up, ProjectedDirection));
                // TargetMagnitude *= Mathf.Clamp01(1.0f - Vector3.Dot(ProjectedDirection, Vector3.up));
            }
			else
            {
                TargetMagnitude =  AirSpeed * InputDirection.magnitude;
            }

            //What is our non-Y velocity this Update?
            Vector3 LateralVelocity = Vector3.zero;
            Vector3 ForwardDirection = transform.forward;
            if (IsGrounded)
            {
                //Rotate to face the way the player wants to go at a rate of MaxRotationDelta per second
                Vector3 Facing = TargetDirection;
                Facing.y = 0.0f;
                
                transform.forward = Vector3.RotateTowards(transform.forward, Facing, MaxRotationDelta * Time.deltaTime, 0);
                ForwardDirection = Vector3.RotateTowards(transform.forward, ProjectedDirection, MaxRotationDelta * Time.deltaTime, 0);
                //Accelerate until we hit our TargetMagnitude
                ForwardVelocity = Mathf.Min(ForwardVelocity + (GroundAcceleration * Time.deltaTime), TargetMagnitude);
            }
            else
            {
                //In the air, we want to try to keep going forward
                //A separate SideVelocity allows the user to strafe left/right at a constant speed
                float SideVelocity = 0.0f;
                if (InputDirection.sqrMagnitude > 0)
                {
                    float Angle = Vector3.SignedAngle(transform.forward, TargetDirection, Vector3.up) * Mathf.Deg2Rad;

                    //Accelerate based on how far the stick is pushed
                    ForwardVelocity += Mathf.Cos(Angle) * AirAcceleration * InputDirection.magnitude * Time.deltaTime;

                    //Strafe velocity is a constant based on how far the stick is pushed
                    SideVelocity = Mathf.Sin(Angle) * TargetMagnitude;
                }

                //Start applying drag if we're going faster than AirSpeed
                if (Mathf.Abs(ForwardVelocity) > AirSpeed)
                {
                    ForwardVelocity -= Mathf.Sign(ForwardVelocity) * AirDrag * Time.deltaTime;
                }

                //Apply the SideVelocity to our lateral velocity
                LateralVelocity += transform.right * SideVelocity;
                StateInfo.SideVelocity = SideVelocity;
            }

            LateralVelocity += ForwardDirection * ForwardVelocity;


            Vector3 finalVelocity = Velocity;
            

            //Apply our lateral velocity to our velocity for this frame     
            if (IsGrounded)
            {
                finalVelocity.x = LateralVelocity.x;
                if (!IsSliding)
                {
                    finalVelocity.y = LateralVelocity.y;
                }
              //  finalVelocity.y -= Physics.gravity.y * Time.deltaTime;
                finalVelocity.z = LateralVelocity.z;
			}
			else
            {
                finalVelocity.x = LateralVelocity.x;
                finalVelocity.z = LateralVelocity.z;
                if (IsSliding)
                {
                    Velocity.x = 0;
                    Velocity.z = 0;
                       finalVelocity = Vector3.ProjectOnPlane(Velocity, GroundNormal) + LateralVelocity;
                    finalVelocity = Vector3.ProjectOnPlane(finalVelocity, GroundNormal) + LateralVelocity * Mathf.Clamp01(Vector3.Dot(LateralVelocity.normalized, GroundNormal.normalized));
                }
            }

            CharController.Move(finalVelocity * Time.deltaTime);
            Animator.SetFloat("Speed", LateralVelocity.magnitude);
            Animator.SetBool("Grounded", IsGrounded);


            //Update state for debug info
            StateInfo.ForwardVelocity = ForwardVelocity;
            StateInfo.InputDirection = InputDirection;
            StateInfo.IsGrounded = IsGrounded;
            StateInfo.IsSliding = IsSliding;
            StateInfo.JumpHeld = JumpHeld;
            StateInfo.Velocity = Velocity;
            StateInfo.FinalVelocity = finalVelocity;
        }

        private void PerformJump()
        {
            JumpTrigger = false;    //Clear the jump trigger
            IsGrounded = false;
            IsSliding = false;
            //Calculate our desired jump velocity based on the current gravity value
            float vel = Mathf.Sqrt(-2.0f * (UseUnityGravity ? Physics.gravity.y : CustomGravity) * JumpHeight);
            Velocity.y = vel + ForwardVelocity * ForwardVelocityJumpInfluence;

            ForwardVelocity *= 1.0f - ForwardVelocityJumpInfluence;

            Animator.SetTrigger("Jump");
        }

		private void OnDrawGizmos()
		{
            if (IsGrounded || IsSliding) {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(StateInfo.ContactPosition, 0.05f);
                Gizmos.DrawRay(StateInfo.ContactPosition, StateInfo.GroundNormal);
            }
		}
	}
}
