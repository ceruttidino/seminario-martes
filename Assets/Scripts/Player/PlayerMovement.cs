using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private PlayerDash playerDash;
    [SerializeField] private PlayerAim playerAim;

    [Header("Input")]
    private Vector2 moveInput;
    private Vector2 moveDirection;
    private Vector2 lastFacingDirection = Vector2.down;
    private bool attackAnimationActive;

    private static readonly int AttackBlendTreeHash = Animator.StringToHash("Raccoon_Attack_BlendTree");
    private static readonly int AreaAttackHash = Animator.StringToHash("Raccoon_Area_Attack");

    [Header("Movement Stats")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 18f;
    [SerializeField] private float deceleration = 24f;
    [SerializeField] private float turnAcceleration = 22f;
    [SerializeField] private float inputDeadzone = 0.1f;

    [Header("Feel")]
    [SerializeField] private bool instantStop = false;

    [Header("Public Variables")]
    public Vector2 LastLookDirection => lastFacingDirection;
    public Vector2 LastFacingDirection => lastFacingDirection;
    public Vector2 MoveInput => moveInput;
    public Vector2 MoveDirection => moveDirection;
    public Vector2 CurrentVelocity => rb != null ? rb.linearVelocity : Vector2.zero;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (playerDash == null)
            playerDash = GetComponent<PlayerDash>();

        if (playerAim == null)
            playerAim = GetComponent<PlayerAim>();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void LateUpdate()
    {
        ApplySpriteFlip();
    }

    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();

        if (moveInput.magnitude < inputDeadzone)
            moveInput = Vector2.zero;

        moveDirection = moveInput.normalized;
    }

    private void HandleMovement()
    {
        if (playerDash != null && playerDash.IsDashing)
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

        rb.linearVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, accelRate * Time.fixedDeltaTime);

        UpdateAnimator();
    }

    private void UpdateFacingDirection()
    {
        if (playerAim != null && playerAim.HasAimInput)
            lastFacingDirection = playerAim.LastAimDirection;
        else if (moveDirection != Vector2.zero)
            lastFacingDirection = moveDirection;
    }

    private void UpdateAnimator()
    {
        UpdateFacingDirection();

        float speed = rb.linearVelocity.magnitude;
        Vector2 animFacing = GetCardinalFacing(lastFacingDirection);

        // Solo existe Raccoon_Walk_Right: horizontal siempre usa X=1 y flipX para izquierda
        float animX = animFacing.x;
        float animY = animFacing.y;
        if (animX != 0f)
            animX = 1f;

        animator.SetFloat("Speed", speed);
        animator.SetFloat("X", animX);
        animator.SetFloat("Y", animY);
    }

    private void ApplySpriteFlip()
    {
        if (spriteRenderer == null || animator == null) return;

        if (attackAnimationActive || IsPlayingActionAnimation())
        {
            spriteRenderer.flipX = false;
            if (!IsPlayingActionAnimation())
                attackAnimationActive = false;
            return;
        }

        Vector2 facing = GetCardinalFacing(lastFacingDirection);
        spriteRenderer.flipX = facing.x < 0f;
    }

    public void SetAttackAnimationActive(bool active)
    {
        attackAnimationActive = active;

        if (active && spriteRenderer != null)
            spriteRenderer.flipX = false;
    }

    private bool IsPlayingActionAnimation()
    {
        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        if (current.shortNameHash == AttackBlendTreeHash || current.shortNameHash == AreaAttackHash)
            return true;

        if (!animator.IsInTransition(0))
            return false;

        AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
        return next.shortNameHash == AttackBlendTreeHash || next.shortNameHash == AreaAttackHash;
    }

    private static Vector2 GetCardinalFacing(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.01f)
            return Vector2.down;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x > 0f ? Vector2.right : Vector2.left;

        return direction.y > 0f ? Vector2.up : Vector2.down;
    }

    public void IncreaseMoveSpeed(float amount)
    {
        moveSpeed += amount;
    }

    public void IncreaseMoveSpeedPercent(float percent)
    {
        moveSpeed *= 1f + percent / 100f;
    }

    public bool IsDashing() => playerDash != null && playerDash.IsDashing;
}
