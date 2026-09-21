using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Player Movement & Gravity")]

    public float movementSpeed = 5f;
    private CharacterController controller;


    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        HandleMovement();
    }

    void HandleMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float vertialInput = Input.GetAxis("Vertical");

        Vector3 movement = transform.right * horizontalInput + transform.forward * vertialInput;
        movement.y = 0;

        controller.Move(movement * movementSpeed * Time.deltaTime);
    }

}
