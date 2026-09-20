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
    [SerializeField] private Animator animator;
    [SerializeField] private QuickAttack quickAttack;
    [SerializeField] private AreaAttack areaAttack;

    [SerializeField] private AudioSource sfxSource;

    private InputAction dashAction;
    private float lastDashTime = -999f;
    private bool isDashing = false;
    private Collider2D[] playerColliders;
    private readonly System.Collections.Generic.List<Collider2D> ignoredEnemyColliders = new System.Collections.Generic.List<Collider2D>();

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

    public bool CanDash => CooldownRemaining <= 0f && !isDashing && !IsAnyAttackActive() && !GamePause.IsGameplayFrozen
        && (playerMovement == null || !playerMovement.IsDigging);

    private bool IsAnyAttackActive()
    {
        return (quickAttack != null && quickAttack.IsAttacking)
            || (areaAttack != null && areaAttack.IsAttacking);
    }

    private void Awake()
    {
        dashAction = GetComponent<PlayerInput>().actions["Dash"];

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator> ();

        if (quickAttack == null)
            quickAttack = GetComponent<QuickAttack>();

        if (areaAttack == null)
            areaAttack = GetComponent<AreaAttack>();

        playerColliders = GetComponentsInChildren<Collider2D>();
    }

    private void OnEnable()
    {
        GamePause.GameplayFrozen += CancelDash;
    }

    private void OnDisable()
    {
        GamePause.GameplayFrozen -= CancelDash;
    }

    private void Update()
    {
        if (GamePause.IsGameplayFrozen) return;

        if (WasDashPressed())
            TryDash();
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (GamePause.IsGameplayFrozen) return;
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
        IgnoreEnemyCollisions(true);

        if (animator != null)
        {
            animator.SetBool("IsDashing", true);
        }

        AudioManager.PlaySfx(GameSfx.Dash, sfxSource != null ? sfxSource.clip : null);
            

        StartCoroutine(DashCoroutine(dashDirection));
    }

    private IEnumerator DashCoroutine(Vector2 direction)
    {
        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            if (GamePause.IsGameplayFrozen)
            {
                CancelDash();
                yield break;
            }

            elapsed += Time.deltaTime;
            rb.linearVelocity = direction * dashSpeed;
            yield return null;
        }

        EndDash(keepMomentum: true);
    }

    private void CancelDash()
    {
        if (!isDashing) return;
        StopAllCoroutines();
        EndDash(keepMomentum: false);
    }

    private void EndDash(bool keepMomentum)
    {
        isDashing = false;
        IgnoreEnemyCollisions(false);

        if (animator != null)
            animator.SetBool("IsDashing", false);

        if (rb == null) return;

        if (keepMomentum)
            rb.linearVelocity *= 0.6f;
        else
            rb.linearVelocity = Vector2.zero;
    }

    private void IgnoreEnemyCollisions(bool ignore)
    {
        if (playerColliders == null) return;

        if (!ignore)
        {
            for (int i = 0; i < ignoredEnemyColliders.Count; i++)
            {
                Collider2D enemyCol = ignoredEnemyColliders[i];
                if (enemyCol == null) continue;

                for (int p = 0; p < playerColliders.Length; p++)
                {
                    Collider2D playerCol = playerColliders[p];
                    if (playerCol == null) continue;
                    Physics2D.IgnoreCollision(playerCol, enemyCol, false);
                }
            }

            ignoredEnemyColliders.Clear();
            return;
        }

        ignoredEnemyColliders.Clear();
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer < 0) return;

        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null) continue;

            Collider2D[] enemyCols = enemies[i].GetComponentsInChildren<Collider2D>();
            for (int e = 0; e < enemyCols.Length; e++)
            {
                Collider2D enemyCol = enemyCols[e];
                if (enemyCol == null || enemyCol.isTrigger) continue;

                for (int p = 0; p < playerColliders.Length; p++)
                {
                    Collider2D playerCol = playerColliders[p];
                    if (playerCol == null) continue;
                    Physics2D.IgnoreCollision(playerCol, enemyCol, true);
                }

                ignoredEnemyColliders.Add(enemyCol);
            }
        }
    }
}
