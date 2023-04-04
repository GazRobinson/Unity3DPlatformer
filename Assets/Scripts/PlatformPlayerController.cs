/// <summary>
/// Mario 64 Style Character Controller for Unity
/// 
/// Written by Gaz Robinson, 2023
/// </summary>

using UnityEngine;

namespace GaRo
{
    /// <summary>
    /// Playerstate used by external classes to get information about the Player
    /// </summary>
    public struct PlayerState
	{
        public Vector2  InputDirection;
        
        public bool     IsGrounded;
        public bool     IsSliding;

        public Vector3  Velocity;
        public Vector3  FinalVelocity;
        public float    ForwardVelocity;
        public float    SideVelocity;

        public Vector3  ContactPosition;
        public Vector3  ShadowPosition;
        public Vector3  GroundNormal;

        public bool     JumpHeld;
	}

    [RequireComponent(typeof(CharacterController))]
    public class PlatformPlayerController : MonoBehaviour
    {
        public System.Action OnJump;

        public float    GroundSpeed = 4.0f;
        public float    GroundAcceleration = 8.0f;
        public float    MaxRotationDelta = 10.0f;

        public float    JumpHeight = 2.0f;
        [Tooltip("How much forward velocity should be translated into vertical jump velocity?\nThis will reduce the forward velocity by the same percentage.")]
        public float    ForwardVelocityJumpInfluence = 0.25f;
        public float    AirSpeed = 2.0f;
        public float    AirAcceleration = 4.0f;
        public float    AirDrag = 2.0f;

        [Tooltip("How much should gravity be increased if the player releases the jump button early.")]
        public float    ShortHopMultiplier = 2.0f;

        // This is only used by the camera just now, but could be used for placing a blob shadow
        // It's used to see if the player is directly above ground
        public float    MaxShadowDistance = 5.0f;   

        public bool     UseUnityGravity = true;
        public float    CustomGravity = -19.82f;
        public PlayerState GetStateInfo
        {
            get { return StateInfo; }
        }

        ////Private////
        ///Info///
        private Vector3 Velocity = Vector3.zero;
        private float   ForwardVelocity = 0.0f;
        private bool    IsGrounded = false;
        private bool    IsSliding = false;
        private PlayerState StateInfo;

        ///Input///
        private Vector2 InputDirection = Vector2.zero;        
        private bool JumpTrigger = false;   //Has the jump button been pressed        
        private bool JumpHeld = false;      //Is the jump button being held

        ///Components///
        private CharacterController CharController = null;
        private Camera PlayerCamera = null;

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
            PlayerCamera = Camera.main;
        }

        /// <summary>
        /// TODO: This needs to be split up. The PlayerState could be used for storing the information and Update could
        /// Then be split into sub-functions
        /// But it's a pretty linear execution, so I don't think it's too bad right now
        /// </summary>
        void Update()
        {
            StateInfo = new PlayerState();

            RaycastHit HitInfo;
            Vector3 GroundNormal = Vector3.up;
            float heightCheckDistance = 0.1f; //How much of our height should 
            float halfHeight = CharController.height * 0.5f;

            //Check to see if we are above a solid surface
			if (Physics.Raycast(transform.position, Vector3.down, out HitInfo, MaxShadowDistance))
			{
                StateInfo.ShadowPosition = HitInfo.point;
            }
			else
			{
                //This is a hack for a camera trick. Sorry!
                //You'd really want to hide the Shadow Position with another variable
                //TODO: The above.
                StateInfo.ShadowPosition = transform.position + (Vector3.down * halfHeight);
            }

            //Check to see if we're grounded
            //We use a SphereCast to avoid any weird capsule collider issues
            if (Physics.SphereCast(transform.position, CharController.radius, Vector3.down, 
                out HitInfo, halfHeight + heightCheckDistance - CharController.radius))
            {
                GroundNormal = HitInfo.normal;
                StateInfo.ContactPosition = HitInfo.point;

                //Only become grounded if we're falling.
                //This avoids becoming grounded when jumping over a ledge
                if (Velocity.y <= 0)
				{
					//Move the character to the ground.
					//This allows the character to stay attached to a downward slope
					CharController.Move(((HitInfo.distance + CharController.radius) - halfHeight) * Vector3.down);
					
                    //If the ground is steep, make us slide and set grounded to false
                    //Otherwise, we're grounded!
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
                //If our cast didn't hit anything, we're airborne
                IsGrounded = false;
                IsSliding = false;
            }

            //Vertical resolution
            //Apply gravity if we're in the air or sliding and check for a jump if we're grounded
            if (!IsGrounded || IsSliding)
            {
                float Gravity = UseUnityGravity ? Physics.gravity.y : CustomGravity;

                //Gravity is increased if the player is moving up but not holding the jump button
                //This lets the play do short hops
                //This won't play nice as-is if you launch the player somehow
                if (Velocity.y > 0.0f && !JumpHeld)
                {
                    Gravity *= ShortHopMultiplier;
                }
                Velocity.y += Gravity * Time.deltaTime;
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

            //Calculate which way the player is trying to go on the XZ plane
            Vector3 TargetDirection = (ViewForward * InputDirection.y + PlayerCamera.transform.right * InputDirection.x).normalized;

            //Adjust the direction to account for the slope of the ground
            Vector3 ProjectedDirection = Vector3.ProjectOnPlane(TargetDirection, GroundNormal).normalized;
            
            //How fast does the player want to go this Update?
            float TargetMagnitude;
            //What is our non-Y velocity this Update?
            Vector3 LateralVelocity = Vector3.zero;
            Vector3 ForwardDirection = transform.forward;

            if (IsGrounded)
            {
                TargetMagnitude = GroundSpeed * InputDirection.magnitude;
            }
			else
            {
                TargetMagnitude =  AirSpeed * InputDirection.magnitude;
            }

            if (IsGrounded)
            {
                //Rotate to face the way the player wants to go at a rate of MaxRotationDelta per second
                transform.forward = Vector3.RotateTowards(transform.forward, TargetDirection, MaxRotationDelta * Time.deltaTime, 0);
                //Our move direction is potentially different if we're going up or down a slope
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

            //Store the velocity because we don't want to modify it accidentally
            Vector3 desiredVelocity = Velocity;            

            //Apply our lateral velocity to our velocity for this frame     
            if (IsGrounded)
            {
                desiredVelocity.x = LateralVelocity.x;
                if (!IsSliding)
                {
                    //Right now, we cannot be sliding if we're grounded, so this is always true
                    //Left in for anyone who wants to change the behaviour
                    desiredVelocity.y = LateralVelocity.y;
                }
                desiredVelocity.z = LateralVelocity.z;
			}
			else
            {
                desiredVelocity.x = LateralVelocity.x;
                desiredVelocity.z = LateralVelocity.z;
                if (IsSliding)
                {
                    //We project out desired velocity onto the surface we're sliding against
                    //Then we add back our intended movement as long as it doesn't move us into the surface
                    //There's probably a better solution for this, but most of the other ways had some other trade off!
                    //Happy to hear suggestions at g.robinson@abertay.ac.uk!
                    desiredVelocity = Vector3.ProjectOnPlane(desiredVelocity, GroundNormal) 
                        + LateralVelocity * Mathf.Clamp01(Vector3.Dot(LateralVelocity.normalized, GroundNormal.normalized));
                }
            }

            //FINALLY, we can actually move our character
            CharController.Move(desiredVelocity * Time.deltaTime);

            //Update state for debug info
            StateInfo.ForwardVelocity   = ForwardVelocity;
            StateInfo.InputDirection    = InputDirection;
            StateInfo.IsGrounded        = IsGrounded;
            StateInfo.IsSliding         = IsSliding;
            StateInfo.JumpHeld          = JumpHeld;
            StateInfo.Velocity          = desiredVelocity;          //Velocity will be how fast we wanted to go
            StateInfo.FinalVelocity     = CharController.velocity;  //FinalVelocity will be how fast we actually went
            StateInfo.GroundNormal      = GroundNormal;
        }

        private void PerformJump()
        {
            JumpTrigger = false;    //Clear the jump trigger
            IsGrounded = false;     //Force us to be ungrounded
            IsSliding = false;

            //Calculate our desired jump velocity based on the current gravity value
            float JumpVelocity = Mathf.Sqrt(-2.0f * (UseUnityGravity ? Physics.gravity.y : CustomGravity) * JumpHeight);

            //We jump higher if we're moving forward and reduce our forward velocity slightly
            Velocity.y = JumpVelocity + ForwardVelocity * ForwardVelocityJumpInfluence;
            ForwardVelocity *= 1.0f - ForwardVelocityJumpInfluence;

            //Fire the OnJump action for anything that cares about it
            if(OnJump != null)
                OnJump();
        }
	}
}
