using System.Collections; // NUEVO
using UnityEngine;

public class QuickAttack : MonoBehaviour, IAttack
{
    [Header("References")]
    [SerializeField] private PlayerAim playerAim;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerDash playerDash;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask trashLayer;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SquashStretch playerSquash; // NUEVO: squash del sprite del player (ideal en un hijo "Visual")

    [Header("Quick Attack")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float cooldown = 0.5f;
    [SerializeField] private float attackDistance = 1f;
    [Tooltip("X = largo hacia adelante, Y = ancho del golpe. Rota con la dirección del ataque.")] // CAMBIADO
    [SerializeField] private Vector2 attackBoxSize = new Vector2(1.2f, 0.8f);
    [SerializeField] private float attackActiveDuration = 0.25f;

    [Header("Feel - Timing")] // NUEVO
    [Tooltip("Demora entre que arranca el swing y el frame en que pega. 0 = instantáneo. Probá 0.02-0.05 para sincronizar con el frame del slash.")]
    [SerializeField] private float hitDelay = 0f; // NUEVO
    [Tooltip("Si pasa más que esto entre golpes, el combo visual vuelve al primer swing")]
    [SerializeField] private float comboResetTime = 0.6f; // NUEVO

    [Header("Feel - Lunge del player")] // NUEVO
    [Tooltip("Velocidad del pasito hacia adelante al atacar. 0 = sin lunge.")]
    [SerializeField] private float lungeSpeed = 6f; // NUEVO
    [SerializeField] private float lungeDuration = 0.1f; // NUEVO
    [Tooltip("Si el golpe conecta, el lunge se achica (el player 'frena' contra el enemigo en vez de meterse encima)")]
    [SerializeField, Range(0f, 1f)] private float lungeOnHitMultiplier = 0.3f; // NUEVO
    [Tooltip("Tiempo que el player camina más lento después de atacar")]
    [SerializeField] private float attackSlowDuration = 0.15f; // NUEVO

    [Header("Feel - Knockback")] // NUEVO
    [Tooltip("Velocidad inicial del empuje (unidades/seg). 10 con duración 0.15 ≈ medio tile.")]
    [SerializeField] private float knockbackForce = 10f; // NUEVO

    [Header("Feel - Hit Stop / Camera")] // NUEVO
    [SerializeField] private float hitStopDuration = 0.05f; // NUEVO
    [SerializeField] private float hitStopPerExtraEnemy = 0.015f; // NUEVO
    [SerializeField] private float shakeTrauma = 0.2f; // NUEVO
    [SerializeField] private float cameraKick = 0.1f; // NUEVO
    [SerializeField] private Vector2 attackSquash = new Vector2(1.15f, 0.88f); // NUEVO

    [Header("Attack VFX")]
    [SerializeField] private GameObject attackVfxPrefab;
    [Tooltip("Distancia del slash al centro del player. Con el SlashArcVfx usá 0 (el arco ya rodea al player).")]
    [SerializeField] private float vfxDistance = 1f;
    [Tooltip("Corrige dónde está el 'centro' del player. Si el pivot del sprite está en los pies, subilo (ej: 0, 0.4).")]
    [SerializeField] private Vector2 vfxCenterOffset = Vector2.zero; // NUEVO
    [SerializeField] private float vfxLifetime = 0.5f;
    [SerializeField] private bool vfxFollowsPlayer = true;
    [SerializeField] private float vfxAngleOffset = 0f;
    [Tooltip("Espeja el slash en golpes alternados (arriba→abajo, abajo→arriba)")]
    [SerializeField] private bool alternateSlashVfx = true; // NUEVO
    [Tooltip("Nombre de un parámetro int del Animator para alternar animaciones de swing. Vacío = no se usa.")]
    [SerializeField] private string swingIndexParam = ""; // NUEVO

    [SerializeField] private AudioSource sfxSource;

    private float lastUseTime = -999f;
    private Vector2 lastAttackDirection = Vector2.down;
    private bool isAttacking = false;
    private int swingIndex = 0; // NUEVO

    public bool IsAttacking => isAttacking;

    private void Awake()
    {
        if (playerAim == null)
            playerAim = GetComponent<PlayerAim>();

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (playerDash == null)
            playerDash = GetComponent<PlayerDash>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (playerSquash == null) // NUEVO
            playerSquash = GetComponentInChildren<SquashStretch>();
    }

    public float CooldownRemaining
    {
        get
        {
            float remaining = cooldown - (Time.time - lastUseTime);
            return Mathf.Max(0f, remaining);
        }
    }

    public bool CanExecute()
    {
        if (GamePause.IsGameplayFrozen) return false;
        if (playerDash != null && playerDash.IsDashing) return false;
        if (playerMovement != null && playerMovement.IsActionLocked) return false;
        return Time.time >= lastUseTime + cooldown;
    }

    private void Update()
    {
        if (GamePause.IsGameplayFrozen) return;
        if (playerAim == null || !playerAim.IsAimingNow()) return;
        TryPerformAttack();
    }

    public void Execute()
    {
        TryPerformAttack();
    }

    private void TryPerformAttack()
    {
        if (!CanExecute()) return;
        if (playerAim == null) return;

        Vector2 attackDirection = playerAim.GetAttackDirection();
        if (attackDirection == Vector2.zero) return;
        attackDirection.Normalize(); // NUEVO: por si el aim no viene normalizado

        // NUEVO: golpes seguidos alternan el swing; si pasó mucho tiempo, vuelve al primero
        swingIndex = (Time.time - lastUseTime > comboResetTime) ? 0 : 1 - swingIndex;

        lastUseTime = Time.time;
        lastAttackDirection = attackDirection;

        isAttacking = true;
        CancelInvoke(nameof(EndAttackWindow));
        Invoke(nameof(EndAttackWindow), attackActiveDuration);

        AudioManager.PlayQuickAttack(sfxSource != null ? sfxSource.clip : null);

        if (animator != null)
        {
            if (playerMovement != null)
                playerMovement.SetAttackAnimationActive(true);

            if (spriteRenderer != null)
                spriteRenderer.flipX = false;

            animator.SetFloat("AttackX", attackDirection.x);
            animator.SetFloat("AttackY", attackDirection.y);

            if (!string.IsNullOrEmpty(swingIndexParam)) // NUEVO
                animator.SetInteger(swingIndexParam, swingIndex);

            animator.SetTrigger("Attack");
        }

        SpawnAttackVfx(attackDirection); // CAMBIADO: antes solo salía si había Animator asignado

        if (playerSquash != null) // NUEVO
            playerSquash.Punch(attackSquash);

        // CAMBIADO: la detección de golpe se movió a PerformHit() (y puede ir con un pequeño delay)
        if (hitDelay > 0f)
            StartCoroutine(DelayedHit(attackDirection));
        else
            PerformHit(attackDirection);
    }

    // NUEVO
    private IEnumerator DelayedHit(Vector2 attackDirection)
    {
        yield return new WaitForSeconds(hitDelay);
        PerformHit(attackDirection);
    }

    // NUEVO (antes este código estaba dentro de TryPerformAttack)
    private void PerformHit(Vector2 attackDirection)
    {
        Vector2 attackCenter = GetAttackCenter(attackDirection);

        // CAMBIADO: la caja ahora ROTA con la dirección. Antes se intercambiaban X/Y,
        // lo que fallaba con apuntado diagonal (cualquier Y != 0 rotaba la caja).
        float angle = DirectionToAngle(attackDirection);

        LayerMask combinedMask = enemyLayer | trashLayer;
        Collider2D[] hits = Physics2D.OverlapBoxAll(attackCenter, attackBoxSize, angle, combinedMask);

        // CAMBIADO: la resolución del impacto ahora es compartida (y no golpea dos veces al mismo enemigo)
        AttackHitResolver.HitResult result = AttackHitResolver.Resolve(
            hits, transform.position, damage, knockbackForce, _ => attackDirection);

        // NUEVO: lunge del player (más corto si conectó)
        if (playerMovement != null)
        {
            float lunge = result.enemiesHit > 0 ? lungeSpeed * lungeOnHitMultiplier : lungeSpeed;
            playerMovement.ApplyAttackLunge(attackDirection, lunge, lungeDuration, attackSlowDuration);
        }

        if (!result.AnyHit) return; // al aire = sin hit stop, así el golpe que conecta se siente distinto

        // NUEVO: feedback de impacto
        CombatFeedback fx = CombatFeedback.Instance;
        if (fx == null) return;

        float stop = result.enemiesHit > 0
            ? hitStopDuration + Mathf.Max(0, result.enemiesHit - 1) * hitStopPerExtraEnemy
            : hitStopDuration * 0.5f; // objetos (tachos): hit stop más suave

        fx.HitStop(stop);
        fx.AddTrauma(shakeTrauma);
        fx.Kick(attackDirection, cameraKick);
        fx.PlayHitSound();
    }

    private void EndAttackWindow()
    {
        isAttacking = false;
    }

    // NUEVO
    private Vector2 GetAttackCenter(Vector2 direction)
    {
        return (Vector2)transform.position + direction * attackDistance;
    }

    // NUEVO
    private static float DirectionToAngle(Vector2 direction)
    {
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    // CAMBIADO: el gizmo ahora dibuja la caja rotada, igual que la detección real
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 attackCenter = GetAttackCenter(lastAttackDirection);

        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(attackCenter, Quaternion.Euler(0f, 0f, DirectionToAngle(lastAttackDirection)), Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, attackBoxSize);
        Gizmos.matrix = previous;
    }

    public void IncreaseDamage(float amount)
    {
        damage += amount;
    }

    public void IncreaseDamagePercent(float percent)
    {
        damage *= 1f + percent / 100f;
    }

    public void IncreaseRange(float percent)
    {
        attackBoxSize *= 1f + percent / 100f;
    }

    private void SpawnAttackVfx(Vector2 direction)
    {
        if (attackVfxPrefab == null) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + vfxAngleOffset;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
        Vector3 spawnPos = transform.position + (Vector3)(vfxCenterOffset + direction * vfxDistance); // CAMBIADO: suma el offset del centro

        GameObject vfx = Instantiate(attackVfxPrefab, spawnPos, rotation);

        // NUEVO: en el segundo swing el slash se espeja, así el combo se lee como "ida y vuelta"
        if (alternateSlashVfx && swingIndex == 1)
        {
            Vector3 scale = vfx.transform.localScale;
            scale.y = -scale.y;
            vfx.transform.localScale = scale;
        }

        if (vfxFollowsPlayer)
            vfx.transform.SetParent(transform, worldPositionStays: true);

        if (vfxLifetime > 0f)
            Destroy(vfx, vfxLifetime);
    }
}