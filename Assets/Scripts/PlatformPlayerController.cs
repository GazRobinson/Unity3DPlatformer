using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlatformPlayerController : MonoBehaviour
{
    public float m_GroundSpeed = 10.0f;
    public float m_GroundAcceleration = 20.0f;
    public float m_MaxRotationDelta = 180.0f;

    public float m_JumpHeight = 3.0f;

    //Info
    private Vector3 m_Velocity = Vector3.zero;
    private float m_ForwardVelocity = 0.0f;
    private bool m_IsGrounded = false;

    //Input
    private Vector2 m_InputDirection = Vector2.zero;
    private bool m_JumpTrigger = false;
    private bool m_JumpHeld = false;


    private CharacterController m_CC = null;
    private Animator m_Animator = null;

    public void SetMovement(Vector2 input)
    {
        m_InputDirection = input;
    }

    public void SetJump(bool jump)
    {
        m_JumpTrigger |= jump;
        m_JumpHeld = jump;
    }

    void Jump()
    {
        m_JumpTrigger = false;
        float vel = Mathf.Sqrt(-2.0f * Physics.gravity.y * m_JumpHeight);
        m_Velocity.y = vel;
        m_Animator.SetTrigger("Jump");
    }
    
    void Awake()
    {
        m_CC = GetComponent<CharacterController>();
        m_Animator = transform.GetChild(0).GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Physics.Raycast(transform.position, Vector3.down, m_CC.height * 0.55f) )
        {
            if (!m_IsGrounded && m_Velocity.y < 0)
            {
                m_IsGrounded = true;
                m_Velocity.y = 0.0f;
            }
        }
        else
        {
            m_IsGrounded = false;
        }
        if (!m_IsGrounded)
        {
            float grav = Physics.gravity.y;
            if(m_Velocity.y > 0.0f && !m_JumpHeld)
            {
                grav *= 2.0f;
            }
            m_Velocity.y += grav * Time.deltaTime;
        }
        else
        {
            if (m_JumpTrigger)
            {
                Jump();
            }
        }
        
        Vector3 forward = (Camera.main.transform.forward + Camera.main.transform.up) * 0.5f;
        forward.y = 0.0f;
        forward.Normalize();

        Vector3 targetDirection = forward * m_InputDirection.y + Camera.main.transform.right * m_InputDirection.x;
        targetDirection.Normalize();

        //transform.LookAt(transform.position + targetDirection); 
        transform.forward = Vector3.RotateTowards(transform.forward, targetDirection, m_MaxRotationDelta * Time.deltaTime, 0);

        float targetMagnitude = m_GroundSpeed * m_InputDirection.magnitude;

        m_ForwardVelocity = Mathf.Min(m_ForwardVelocity + (m_GroundAcceleration * Time.deltaTime), targetMagnitude);
                
        Vector3 move = transform.forward * m_ForwardVelocity;
        m_Velocity.x = move.x;
        m_Velocity.z = move.z;
        m_CC.Move(m_Velocity * Time.deltaTime);

        m_Animator.SetFloat("Speed", move.magnitude);
        m_Animator.SetBool("Grounded", m_IsGrounded);
    }
}
