using System.Collections;
using NUnit.Framework;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Player Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1.5f;

    private CharacterController controller;
    private PlayerInputHandler input;
    private Vector3 velocity;
    private bool isGrounded;

    [Header("Player Footstep Settings")]
    public AudioSource footSound;
    public AudioClip[] footstepClips; // Array of footstep clips for variety
    
    [SerializeField] private float walkStepInterval = 0.5f;   // Seconds between walk steps
    [SerializeField] private float sprintStepInterval = 0.3f; // Seconds between sprint steps
    
    private bool isStepping = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInputHandler>();
    }

    void Update()
    {
        GroundedCheck();
        HandleMovement();
        HandleJump();
        HandleGravity();
        HandleFootStepState();
    }

    void GroundedCheck()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
    }

    void HandleMovement()
    {
        Vector3 moveDirection = transform.right * input.moveInput.x + transform.forward * input.moveInput.y;
        float currentSpeed = input.isSprinting ? sprintSpeed : walkSpeed;
        controller.Move(currentSpeed * Time.deltaTime * moveDirection);
    }

    void HandleJump()
    {
        if (input.isJumping && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            input.isJumping = false;
        }
    }

    void HandleGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // Controls starting and stopping the footstep coroutine smoothly
    void HandleFootStepState()
    {
        
        if (input.isMoving && isGrounded)
        {
            if (!isStepping)
            {
                isStepping = true;
                StartCoroutine(FootstepRoutine());
            }
        }
    }

    // Coroutine loop that plays step sounds and dynamically handles walk vs sprint timing
    IEnumerator FootstepRoutine()
    {
        PlayFootstepAudio();

        float currentInterval = input.isSprinting ? sprintStepInterval : walkStepInterval;
        yield return new WaitForSeconds(currentInterval);
        
        isStepping = false;
    }

    void PlayFootstepAudio()
    {
        if (footSound == null) return;

        // If an array of clips is provided, pick one at random
        if (footstepClips != null && footstepClips.Length > 0)
        {
            AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
            
            // Subtle pitch variation prevents repetitive sound fatigue
            footSound.pitch = Random.Range(0.9f, 1.1f); 
            footSound.PlayOneShot(clip);
        }
        else if (footSound.clip != null)
        {
            // Fallback to assigned default clip on AudioSource
            footSound.pitch = Random.Range(0.9f, 1.1f);
            footSound.PlayOneShot(footSound.clip);
        }
    }
}