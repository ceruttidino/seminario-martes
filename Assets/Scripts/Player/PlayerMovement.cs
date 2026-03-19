using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 lastLookDirection = Vector2.down;

    [Header("Player Stats")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Public Variables")]
    public Vector2 LastLookDirection => lastLookDirection;
    public Vector2 MoveInput => moveInput;
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

        //know last look direction so the player can attack in that same direction
        if (moveInput.sqrMagnitude > 0.01f) 
        {
            lastLookDirection = moveInput.normalized;
        }
    }
}
