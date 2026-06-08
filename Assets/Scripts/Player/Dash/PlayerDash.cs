using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerDash : MonoBehaviour
{
    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 25f;
    [SerializeField] private float dashDuration = 0.25f;
    [SerializeField] private float dashCooldown = 1.0f;

    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Rigidbody2D rb;

    private InputAction dashAction;
    private float lastDashTime = -999f;
    private bool isDashing = false;

    public bool IsDashing => isDashing;

    public float CooldownRemaining
    {
        get
        {
            float remaining = dashCooldown - (Time.time - lastDashTime);
            return Mathf.Max(0f, remaining);
        }
    }

    public float CooldownNormalized
    {
        get
        {
            if (dashCooldown <= 0f) return 1f;
            return 1f - (CooldownRemaining / dashCooldown);
        }
    }

    public bool CanDash => CooldownRemaining <= 0f && !isDashing;

    private void Awake()
    {
        dashAction = GetComponent<PlayerInput>().actions["Dash"];

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (WasDashPressed())
            TryDash();
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        TryDash();
    }

    private bool WasDashPressed()
    {
        if (dashAction != null && dashAction.WasPerformedThisFrame())
            return true;

        if (Keyboard.current == null)
            return false;

        return Keyboard.current.leftShiftKey.wasPressedThisFrame
            || Keyboard.current.rightShiftKey.wasPressedThisFrame;
    }

    private void TryDash()
    {
        if (!CanDash) return;
        PerformDash();
    }

    private void PerformDash()
    {
        Vector2 dashDirection = playerMovement.MoveDirection;

        if (dashDirection == Vector2.zero)
            dashDirection = playerMovement.LastFacingDirection;

        if (dashDirection == Vector2.zero)
            dashDirection = Vector2.down;

        dashDirection.Normalize();

        lastDashTime = Time.time;
        isDashing = true;

        StartCoroutine(DashCoroutine(dashDirection));
    }

    private IEnumerator DashCoroutine(Vector2 direction)
    {
        float startTime = Time.time;

        while (Time.time < startTime + dashDuration)
        {
            rb.linearVelocity = direction * dashSpeed;
            yield return null;
        }

        isDashing = false;
        rb.linearVelocity *= 0.6f;
    }
}
