using UnityEngine;

public class AreaAttack : MonoBehaviour, IAttack
{
    [Header("References")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask trashLayer;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerDash playerDash;
    [SerializeField] private SquashStretch playerSquash; // NUEVO

    [Header("Area Attack")]
    [SerializeField] private float damage = 20f;
    [SerializeField] private float cooldown = 2f;
    [SerializeField] private float radius = 2f;
    [SerializeField] private float attackActiveDuration = 0.3f;

    [Header("Feel - Knockback")] // NUEVO
    [Tooltip("Más fuerte que el quick attack: es el golpe pesado")]
    [SerializeField] private float knockbackForce = 15f; // NUEVO

    [Header("Feel - Hit Stop / Camera")] // NUEVO
    [SerializeField] private float hitStopDuration = 0.08f; // NUEVO
    [SerializeField] private float hitStopPerExtraEnemy = 0.01f; // NUEVO
    [SerializeField] private float shakeTraumaOnHit = 0.45f; // NUEVO
    [Tooltip("La explosión sacude un poco aunque no pegue a nadie")]
    [SerializeField] private float shakeTraumaOnWhiff = 0.2f; // NUEVO
    [SerializeField] private Vector2 attackSquash = new Vector2(1.3f, 0.75f); // NUEVO
    [Tooltip("El ataque pesado 'planta' al player un instante (camina más lento)")]
    [SerializeField] private float attackSlowDuration = 0.25f; // NUEVO

    [Header("Attack VFX")]
    [SerializeField] private GameObject attackVfxPrefab;
    [SerializeField] private float vfxLifetime = 0.5f;
    [SerializeField] private bool vfxFollowsPlayer = true;

    [SerializeField] private AudioSource sfxSource;

    private float lastUseTime = -999f;
    private bool isAttacking = false;

    /// <summary>Se dispara cada vez que el player usa el ataque pesado (posicion, radio). Lo usan, por ejemplo, las orbes desviables del jefe final.</summary>
    public static event System.Action<Vector2, float> Performed;

    public bool IsAttacking => isAttacking;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (playerDash == null)
            playerDash = GetComponent<PlayerDash>();

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

    public float CooldownNormalized
    {
        get
        {
            if (cooldown <= 0f) return 1f;
            return 1f - (CooldownRemaining / cooldown);
        }
    }

    public bool CanExecute()
    {
        if (GamePause.IsGameplayFrozen) return false;
        if (playerDash != null && playerDash.IsDashing) return false;
        if (playerMovement != null && playerMovement.IsActionLocked) return false;
        return Time.time >= lastUseTime + cooldown;
    }

    public void Execute()
    {
        if (!CanExecute()) return;

        lastUseTime = Time.time;

        isAttacking = true;
        CancelInvoke(nameof(EndAttackWindow));
        Invoke(nameof(EndAttackWindow), attackActiveDuration);

        AudioManager.PlaySfx(GameSfx.AreaAttack, sfxSource != null ? sfxSource.clip : null);

        if (animator != null)
        {
            if (playerMovement != null)
                playerMovement.SetAttackAnimationActive(true);

            animator.SetTrigger("AreaAttack");
        }

        SpawnAttackVfx(); // CAMBIADO: antes solo salía si había Animator asignado

        if (playerSquash != null) // NUEVO
            playerSquash.Punch(attackSquash);

        if (playerMovement != null) // NUEVO: sin lunge, solo slow
            playerMovement.ApplyAttackLunge(Vector2.zero, 0f, 0f, attackSlowDuration);

        Performed?.Invoke(transform.position, radius);

        LayerMask combinedMask = enemyLayer | trashLayer;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, combinedMask);

        // CAMBIADO: resolución compartida. Knockback radial (alejando del centro de la explosión)
        Vector2 origin = transform.position;
        AttackHitResolver.HitResult result = AttackHitResolver.Resolve(
            hits, origin, damage, knockbackForce,
            c => (Vector2)c.transform.position - origin);

        // NUEVO: feedback
        CombatFeedback fx = CombatFeedback.Instance;
        if (fx == null) return;

        if (result.AnyHit)
        {
            float stop = result.enemiesHit > 0
                ? hitStopDuration + Mathf.Max(0, result.enemiesHit - 1) * hitStopPerExtraEnemy
                : hitStopDuration * 0.5f;

            fx.HitStop(stop);
            fx.AddTrauma(shakeTraumaOnHit);
            fx.PlayHitSound();
        }
        else
        {
            fx.AddTrauma(shakeTraumaOnWhiff);
        }
    }

    private void EndAttackWindow()
    {
        isAttacking = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, radius);
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
        radius *= 1f + percent / 100f;
    }

    private void SpawnAttackVfx()
    {
        if (attackVfxPrefab == null) return;

        GameObject vfx = Instantiate(attackVfxPrefab, transform.position, Quaternion.identity);

        // NUEVO: el VFX escala con el radio (si agarrás upgrades de rango, el efecto lo refleja).
        // Asume que el prefab está hecho para radio = 2 (el valor por defecto).
        vfx.transform.localScale *= radius / 2f;

        if (vfxFollowsPlayer)
            vfx.transform.SetParent(transform, worldPositionStays: true);

        if (vfxLifetime > 0f)
            Destroy(vfx, vfxLifetime);
    }

}