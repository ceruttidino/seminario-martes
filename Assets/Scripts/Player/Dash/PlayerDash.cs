using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDash : MonoBehaviour
{
    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 25f;
    [SerializeField] private float dashDuration = 0.25f;
    [SerializeField] private float dashCooldown = 1.0f;

    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Rigidbody2D rb;

    private float lastDashTime = -999f;
    private bool isDashing = false;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (!context.performed || isDashing)
            return;

        if (Time.time >= lastDashTime + dashCooldown)
        {
            PerformDash();
        }
    }

    private void PerformDash()
    {
        Vector2 dashDirection = playerMovement.LastLookDirection;

        if (dashDirection == Vector2.zero)
            dashDirection = Vector2.down;

        dashDirection.Normalize();

        lastDashTime = Time.time;
        isDashing = true;

        Debug.Log($"Dash activado hacia: {dashDirection}");

        StartCoroutine(DashCoroutine(dashDirection));
    }

    private System.Collections.IEnumerator DashCoroutine(Vector2 direction)
    {
        float startTime = Time.time;

        while (Time.time < startTime + dashDuration)
        {
            rb.linearVelocity = direction * dashSpeed;
            yield return null;
        }

        isDashing = false;
        rb.linearVelocity *= 0.6f;

        Debug.Log("Dash terminado");
    }

    public bool IsDashing => isDashing;
}