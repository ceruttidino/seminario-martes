using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    private Rigidbody2D rb;
    private Vector2 moveInput;

    [Header("Player Stats")]
    [SerializeField] private float moveSpeed = 5f;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        //soft movement
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, moveInput * moveSpeed, 0.2f);
    }

    public void Move(InputAction.CallbackContext context)
    {
        //reads input values
        moveInput = context.ReadValue<Vector2>();
    }
}
