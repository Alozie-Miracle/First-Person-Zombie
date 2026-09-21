using UnityEngine;

public class FPSCamera : MonoBehaviour
{
    [SerializeField] float mouseSensitivity = 0.1f;
    [SerializeField] Transform playerBody;

    PlayerInputHandler input;
    float xRotate = 0f;

    void Start()
    {
        input = GetComponentInParent<PlayerInputHandler>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    void Update()
    {
        if (input == null) return;

        // Use lookInput (X = Mouse X / Horizontal, Y = Mouse Y / Vertical)
        float mouseX = input.lookInput.x * mouseSensitivity;
        float mouseY = input.lookInput.y * mouseSensitivity;

        // Rotate camera vertically (Up/Down) with clamping to prevent flipping
        xRotate -= mouseY;
        xRotate = Mathf.Clamp(xRotate, -90f, 90f);
        transform.localRotation = Quaternion.Euler(xRotate, 0f, 0f);

        // Rotate player body horizontally (Left/Right)
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
