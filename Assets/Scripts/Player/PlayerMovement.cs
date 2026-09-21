using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1.5f;

    private CharacterController controller;
    private PlayerInputHandler input;
    private Vector3 velocity;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInputHandler>();
    }

    void Update()
    {
        GroundedCheck();

        HandleMovement();

    }

    private void GroundedCheck()
    {
        // Ground Check
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Keep grounded firmly
        }
    }

    private void HandleMovement()
    {
        // 1. USE MOVEINPUT TO CALCULATE DIRECTION
        // moveInput.x = A/D (Right/Left), moveInput.y = W/S (Forward/Backward)
        Vector3 moveDirection = transform.right * input.moveInput.x + transform.forward * input.moveInput.y;

        // Determine Speed (Sprint check)
        float currentSpeed = input.isSprinting ? sprintSpeed : walkSpeed;

        // Apply Movement
        controller.Move(currentSpeed * Time.deltaTime * moveDirection);

        // 2. JUMPING
        if (input.isJumping && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            input.isJumping = false; // Reset jump flag after consuming
        }

        // 3. APPLY GRAVITY
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
