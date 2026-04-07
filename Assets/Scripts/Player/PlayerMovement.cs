using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Input")]
    private Vector2 moveInput;
    private Vector2 moveDirection;
    private Vector2 lastLookDirection = Vector2.down;

    [Header("Movement Stats")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 18f;
    [SerializeField] private float deceleration = 24f;
    [SerializeField] private float turnAcceleration = 22f;
    [SerializeField] private float inputDeadzone = 0.1f;

    [Header("Feel")]
    [SerializeField] private bool instantStop = false;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 25f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.7f;

    [Header("Public Variables")]
    public Vector2 LastLookDirection => lastLookDirection;
    public Vector2 MoveInput => moveInput;
    public Vector2 MoveDirection => moveDirection;
    public Vector2 CurrentVelocity => rb != null ? rb.linearVelocity : Vector2.zero;
    private bool isDashing = false;
    private bool canDash = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();

        if (moveInput.magnitude < inputDeadzone)
        {
            moveInput = Vector2.zero;
        }

        moveDirection = moveInput.normalized;

        if (moveDirection != Vector2.zero)
        {
            lastLookDirection = moveDirection;
        }
    }

    public void Dash(InputAction.CallbackContext context)
    {
        if (context.performed && canDash && !isDashing)
        {
            StartCoroutine(PerformDash());
        }
    }

    private IEnumerator PerformDash()
    {
        isDashing = true;
        canDash = false;

        Vector2 dashDirection = moveDirection != Vector2.zero ? moveDirection : lastLookDirection;

        rb.linearVelocity = dashDirection * dashSpeed;

        yield return new WaitForSeconds(dashDuration);

        rb.linearVelocity = rb.linearVelocity * 0.35f;

        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void HandleMovement()
    {
        if (isDashing)
            return;

        Vector2 targetVelocity = moveInput * moveSpeed;
        Vector2 currentVelocity = rb.linearVelocity;

        float accelRate;

        if (moveDirection == Vector2.zero)
        {
            if (instantStop)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }
            accelRate = deceleration;
        }
        else
        {
            float velocityDot = Vector2.Dot(currentVelocity.normalized, moveDirection);
            accelRate = velocityDot < 0.5f ? turnAcceleration : acceleration;
        }

        Vector2 newVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, accelRate * Time.fixedDeltaTime);
        rb.linearVelocity = newVelocity;

        UpdateAnimator();
    }

    private void UpdateAnimator()
    {
        float speed = rb.linearVelocity.magnitude;
        animator.SetFloat("Speed", speed);
        animator.SetFloat("X", lastLookDirection.x);

        if (lastLookDirection.x != 0)
        {
            spriteRenderer.flipX = lastLookDirection.x < 0;
        }
    }

    public void IncreaseMoveSpeed(float amount)
    {
        moveSpeed += amount;
    }

    public bool IsDashing() => isDashing;
}