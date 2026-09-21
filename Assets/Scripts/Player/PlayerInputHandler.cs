using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Input Values")]
    public Vector2 moveInput;
    public Vector2 lookInput;
    public bool isSprinting;
    public bool isJumping;
    public bool isShooting;
    public bool isMoving;

    // Called automatically by Player Input (Send Messages) when WASD / Left Stick moves
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();


        isMoving = moveInput.sqrMagnitude > 0.01f;
    }

    // Called automatically when Mouse moves or Right Stick is moved
    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    // Called automatically when Shift / Sprint button is pressed or released
    public void OnSprint(InputValue value)
    {
        isSprinting = value.isPressed;
    }

    // Called automatically when Space / Jump button is pressed
    public void OnJump(InputValue value)
    {
        isJumping = value.isPressed;
    }

    // Called automatically when Left Mouse Button / Trigger is pressed
    public void OnAttack(InputValue value)
    {
        // Note: The default InputSystem_Actions map names the fire action "Attack"
        isShooting = value.isPressed;
    }
}