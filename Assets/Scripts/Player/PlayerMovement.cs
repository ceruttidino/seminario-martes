using System.Collections;
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
    private PlayerSlowStatus slowStatus;

    [Header("Input")]
    private Vector2 moveInput;
    private Vector2 moveDirection;
    private Vector2 lastFacingDirection = Vector2.down;
    private bool attackAnimationActive;

    private static readonly int AttackBlendTreeHash = Animator.StringToHash("Raccoon_Attack_BlendTree");
    private static readonly int AreaAttackHash = Animator.StringToHash("Raccoon_Area_Attack");
    private static readonly int DigHash = Animator.StringToHash("Raccoon_Dig");
    private static readonly int TakeBuffHash = Animator.StringToHash("Raccoon_TakeBuff");
    private bool isDigging;
    private float digLockUntil;
    private bool isPosing;

    // Knockback (ej: golpear a la Hormiga mientras ataca): el jugador sale despedido y no controla el movimiento un instante.
    private float knockbackTimer;
    private float knockbackTotal;
    private Vector2 knockbackStartVelocity;

    [Header("Movement Stats")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 18f;
    [SerializeField] private float deceleration = 24f;
    [SerializeField] private float turnAcceleration = 22f;
    [SerializeField] private float inputDeadzone = 0.1f;

    [Header("Feel")]
    [SerializeField] private bool instantStop = false;

    [Header("Attack Feel")] // NUEVO
    [Tooltip("Multiplicador de velocidad mientras dura el slow del ataque (1 = sin slow)")]
    [SerializeField, Range(0.1f, 1f)] private float attackMoveMultiplier = 0.75f; // NUEVO

    // NUEVO: lunge del ataque (pasito hacia adelante). No bloquea acciones, así se puede encadenar golpes.
    private float lungeTimer;
    private float lungeDuration;
    private Vector2 lungeVelocity;
    private float attackSlowTimer;

    [Header("Public Variables")]
    public Vector2 LastLookDirection => lastFacingDirection;
    public Vector2 LastFacingDirection => lastFacingDirection;
    public Vector2 MoveInput => moveInput;
    public Vector2 MoveDirection => moveDirection;
    public Vector2 CurrentVelocity => rb != null ? rb.linearVelocity : Vector2.zero;
    public bool IsDigging => isDigging;
    public bool IsPosing => isPosing;
    public bool IsKnockedBack => knockbackTimer > 0f;
    public bool IsStunned => stunTimer > 0f;
    public bool IsActionLocked => isDigging || isPosing || IsStunned || IsKnockedBack;

    private float stunTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (playerDash == null)
            playerDash = GetComponent<PlayerDash>();

        if (playerAim == null)
            playerAim = GetComponent<PlayerAim>();

        if (GetComponent<PlayerWalkableClamp>() == null)
            gameObject.AddComponent<PlayerWalkableClamp>();

        slowStatus = GetComponent<PlayerSlowStatus>();
        if (slowStatus == null)
            slowStatus = gameObject.AddComponent<PlayerSlowStatus>();
    }

    private void OnEnable()
    {
        GamePause.GameplayFrozen += FreezeMovement;
    }

    private void OnDisable()
    {
        GamePause.GameplayFrozen -= FreezeMovement;

        // Si el script se apaga a mitad del cavado, Unity corta la corrutina y el flag quedaba en true para siempre.
        isDigging = false;
        digLockUntil = 0f;
        if (animator != null)
            animator.ResetTrigger("Dig");
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    private void FreezeMovement()
    {
        moveInput = Vector2.zero;
        moveDirection = Vector2.zero;
        CancelAttackMovement(); // NUEVO

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (animator != null)
            animator.SetFloat("Speed", 0f);
    }

    void Update()
    {
        if (isDigging && digLockUntil > 0f && Time.unscaledTime > digLockUntil)
        {
            isDigging = false;
            digLockUntil = 0f;
            if (animator != null)
                animator.ResetTrigger("Dig");
            ReleaseFromSolids();
        }

        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
            if (rb != null && knockbackTimer <= 0f)
                rb.linearVelocity = Vector2.zero;
        }
    }

    /// <summary>
    /// Empuja al jugador 'distance' unidades en 'direction' durante 'duration' segundos (velocidad inicial alta que baja
    /// de forma lineal). Mientras dura no puede moverse, atacar ni hacer dash.
    /// </summary>
    public void ApplyKnockback(Vector2 direction, float distance, float duration)
    {
        if (direction.sqrMagnitude < 0.0001f)
            direction = -lastFacingDirection;

        CancelAttackMovement(); // NUEVO: un knockback recibido corta el lunge

        duration = Mathf.Max(0.05f, duration);
        knockbackTotal = duration;
        knockbackTimer = duration;
        knockbackStartVelocity = direction.normalized * (2f * Mathf.Max(0f, distance) / duration);

        if (rb != null)
            rb.linearVelocity = knockbackStartVelocity;

        if (animator != null)
            animator.SetFloat("Speed", 0f);
    }

    private void StepKnockback()
    {
        knockbackTimer -= Time.fixedDeltaTime;

        float k = Mathf.Clamp01(knockbackTimer / Mathf.Max(0.01f, knockbackTotal));
        if (rb != null)
            rb.linearVelocity = knockbackTimer > 0f ? knockbackStartVelocity * k : Vector2.zero;

        if (animator != null)
            animator.SetFloat("Speed", 0f);
    }

    // NUEVO
    /// <summary>
    /// Lo llaman los ataques: da un impulso corto hacia 'direction' (lunge) que se funde con el input del jugador,
    /// y aplica un slow de 'slowDuration' segundos para que el golpe tenga peso.
    /// </summary>
    public void ApplyAttackLunge(Vector2 direction, float speed, float duration, float slowDuration)
    {
        if (IsActionLocked || IsDashing()) return;

        attackSlowTimer = Mathf.Max(attackSlowTimer, slowDuration);

        if (speed <= 0f || duration <= 0f || direction.sqrMagnitude < 0.0001f || rb == null)
            return;

        lungeDuration = duration;
        lungeTimer = duration;
        lungeVelocity = direction.normalized * speed;
        rb.linearVelocity = lungeVelocity;
    }

    // NUEVO
    private void CancelAttackMovement()
    {
        lungeTimer = 0f;
        attackSlowTimer = 0f;
    }

    public void Stun(float duration)
    {
        CancelAttackMovement(); // NUEVO
        stunTimer = Mathf.Max(stunTimer, duration);
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    void LateUpdate()
    {
        ApplySpriteFlip();
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (GamePause.IsGameplayFrozen)
        {
            moveInput = Vector2.zero;
            moveDirection = Vector2.zero;
            return;
        }

        moveInput = context.ReadValue<Vector2>();

        if (moveInput.magnitude < inputDeadzone)
            moveInput = Vector2.zero;

        moveDirection = moveInput.normalized;
    }

    private void HandleMovement()
    {
        if (knockbackTimer > 0f && !GamePause.IsGameplayFrozen)
        {
            StepKnockback();
            return;
        }

        if (GamePause.IsGameplayFrozen || isDigging || isPosing || IsStunned)
        {
            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            if (animator != null)
                animator.SetFloat("Speed", 0f);
            return;
        }

        if (playerDash != null && playerDash.IsDashing)
        {
            CancelAttackMovement(); // CAMBIADO: el dash corta el lunge para que no "reviva" al terminar
            return;
        }

        float speedMultiplier = slowStatus != null ? slowStatus.SpeedMultiplier : 1f;

        // NUEVO: slow mientras ataca
        if (attackSlowTimer > 0f)
        {
            attackSlowTimer -= Time.fixedDeltaTime;
            speedMultiplier *= attackMoveMultiplier;
        }

        Vector2 targetVelocity = moveInput * moveSpeed * speedMultiplier;

        // NUEVO: lunge. Arranca con el impulso y se funde suave con la velocidad que pide el input
        if (lungeTimer > 0f)
        {
            lungeTimer -= Time.fixedDeltaTime;
            float k = Mathf.Clamp01(lungeTimer / lungeDuration);
            rb.linearVelocity = Vector2.Lerp(targetVelocity, lungeVelocity, k * k);
            UpdateAnimator();
            return;
        }

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
        if (IsActionState(current.shortNameHash))
            return true;

        if (!animator.IsInTransition(0))
            return false;

        AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
        return IsActionState(next.shortNameHash);
    }

    private static bool IsActionState(int hash)
    {
        return hash == AttackBlendTreeHash || hash == AreaAttackHash || hash == DigHash || hash == TakeBuffHash;
    }

    /// <summary>
    /// Pose de "agarre de buff": el mapache se queda QUIETO (sin moverse, atacar ni hacer dash) mientras dura la animacion
    /// Raccoon_TakeBuff, mostrando el objeto que acaba de conseguir. Devuelve false si no se puede arrancar ahora (ya esta
    /// posando, dash en curso, juego pausado...).
    /// </summary>
    public bool TryPlayTakeBuff(System.Action onComplete = null)
    {
        if (isPosing || animator == null || GamePause.IsGameplayFrozen)
            return false;

        if (playerDash != null && playerDash.IsDashing)
            return false;

        isPosing = true;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        animator.SetFloat("Speed", 0f);
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("AreaAttack");
        animator.ResetTrigger("Dig");
        animator.SetTrigger("TakeBuff");

        StartCoroutine(WaitForTakeBuff(onComplete));
        return true;
    }

    private IEnumerator WaitForTakeBuff(System.Action onComplete)
    {
        const float timeout = 3f;
        const float enterGrace = 0.5f;

        float elapsed = 0f;
        bool entered = false;

        while (elapsed < timeout)
        {
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);

            if (current.shortNameHash == TakeBuffHash)
            {
                entered = true;
                if (current.normalizedTime >= 1f && !animator.IsInTransition(0))
                    break;
            }
            else if (entered && !animator.IsInTransition(0))
            {
                break;
            }
            else if (!entered && elapsed > enterGrace)
            {
                // El animator nunca entro al estado (controller sin el estado, etc.): no se deja al jugador trabado.
                break;
            }

            elapsed += Time.deltaTime;
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
            animator.SetFloat("Speed", 0f);
            yield return null;
        }

        // Por si el trigger quedo sin consumir (que no vuelva a disparar la pose al salir del estado).
        animator.ResetTrigger("TakeBuff");

        isPosing = false;
        onComplete?.Invoke();
    }

    public bool TryPlayDig(System.Action onComplete)
    {
        if (isDigging || GamePause.IsGameplayFrozen)
            return false;

        if (playerDash != null && playerDash.IsDashing)
            return false;

        // Con enemigos vivos, cavar traba al jugador contra el pozo o la puerta. F no hace nada y, si ya quedó metido, lo suelta.
        if (RoomHasLivingEnemies())
        {
            ReleaseFromSolids();
            return false;
        }

        isDigging = true;
        digLockUntil = Time.unscaledTime + 1.8f;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("AreaAttack");
            animator.ResetTrigger("Dig");
            animator.SetTrigger("Dig");
        }

        StartCoroutine(WaitForDig(onComplete));
        return true;
    }

    private IEnumerator WaitForDig(System.Action onComplete)
    {
        // La animacion dura 1 segundo. El tope es de respaldo: si el estado no arranca o no avanza,
        // el jugador no se queda con el movimiento bloqueado.
        const float timeout = 1.6f;
        const float enterGrace = 0.35f;

        if (animator == null)
        {
            FinishDig(onComplete);
            yield break;
        }

        float elapsed = 0f;
        bool enteredDig = false;
        float lastSample = -1f;
        float stalled = 0f;

        while (elapsed < timeout)
        {
            if (animator != null)
            {
                AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
                bool inDig = current.shortNameHash == DigHash;

                if (inDig)
                {
                    enteredDig = true;
                    // Si el trigger sigue prendido, Any State vuelve a entrar a Raccoon_Dig y no termina nunca.
                    animator.ResetTrigger("Dig");

                    if (!animator.IsInTransition(0) && current.normalizedTime >= 0.98f)
                        break;

                    float sample = current.normalizedTime;
                    if (lastSample >= 0f && sample <= lastSample + 0.001f)
                        stalled += Time.unscaledDeltaTime;
                    else
                        stalled = 0f;

                    lastSample = sample;
                    if (stalled > 0.45f)
                        break;
                }
                else if (enteredDig && !animator.IsInTransition(0))
                {
                    break;
                }
                else if (!enteredDig && elapsed > enterGrace)
                {
                    break;
                }
            }

            // unscaled: con timeScale en 0 (pausa / hit stop) el contador igual avanza y el bloqueo se suelta.
            elapsed += Time.unscaledDeltaTime;
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
            if (animator != null)
                animator.SetFloat("Speed", 0f);
            yield return null;
        }

        FinishDig(onComplete);
    }

    private void FinishDig(System.Action onComplete)
    {
        isDigging = false;
        digLockUntil = 0f;
        if (animator != null)
            animator.ResetTrigger("Dig");

        ReleaseFromSolids();

        if (onComplete == null)
            return;

        try
        {
            onComplete.Invoke();
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }

    private static bool RoomHasLivingEnemies()
    {
        RoomInstance room = DungeonManager.Instance != null ? DungeonManager.Instance.CurrentRoom : null;
        return room != null && room.HasLivingEnemies();
    }

    /// <summary>
    /// Si el centro del cuerpo quedó adentro de un collider sólido (pozo, puerta, pared), lo corre hacia la sala.
    /// Parado al lado de una pared el centro no está adentro, así que caminar normal no se toca.
    /// </summary>
    public void ReleaseFromSolids()
    {
        if (rb == null || !CenterIsInsideSolid(rb.position))
            return;

        Vector2 origin = rb.position;
        Vector2 inward = Vector2.down;
        if (DungeonManager.Instance != null && DungeonManager.Instance.CurrentRoom != null)
        {
            Vector2 toRoom = (Vector2)DungeonManager.Instance.CurrentRoom.transform.position - origin;
            if (toRoom.sqrMagnitude > 0.0001f)
                inward = toRoom.normalized;
        }

        Vector2[] dirs =
        {
            inward,
            Vector2.down,
            Vector2.up,
            Vector2.left,
            Vector2.right
        };

        RoomPathGrid grid = null;
        if (DungeonManager.Instance != null && DungeonManager.Instance.CurrentRoom != null)
            grid = RoomPathGrid.ForQuick(DungeonManager.Instance.CurrentRoom.transform);

        for (float dist = 0.45f; dist <= 2.1f; dist += 0.35f)
        {
            for (int i = 0; i < dirs.Length; i++)
            {
                Vector2 candidate = origin + dirs[i] * dist;
                if (CenterIsInsideSolid(candidate))
                    continue;

                if (grid != null && grid.IsOverVoid(candidate))
                    continue;

                rb.position = candidate;
                rb.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();
                GetComponent<PlayerWalkableClamp>()?.ResetTo(candidate);
                return;
            }
        }
    }

    private bool CenterIsInsideSolid(Vector2 point)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(point);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || !hit.enabled || hit.isTrigger)
                continue;

            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;

            return true;
        }

        return false;
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