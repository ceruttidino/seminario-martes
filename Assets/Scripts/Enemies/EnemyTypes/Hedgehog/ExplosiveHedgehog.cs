using System.Collections.Generic;
using UnityEngine;

public class ExplosiveHedgehog : MonoBehaviour
{
    [Header("Ranges")]
    [Tooltip("Distancia a la que empieza a deformarse y armar la explosion.")]
    [SerializeField] private float armingRange = 1.5f;
    [Tooltip("Si el jugador se aleja esta distancia (GDD: 2 metros), cancela y vuelve a perseguir.")]
    [SerializeField] private float cancelRange = 2f;
    [SerializeField] private float explosionRadius = 2.5f;

    [Header("Timing")]
    [SerializeField] private float countdownDuration = 2f;
    [SerializeField] private float hitKnockbackDuration = 0.3f;

    [Header("Movement")]
    [Tooltip("Persecucion rapida. El resto de enemigos camina a 3.")]
    [SerializeField] private float chaseSpeed = 4.35f;
    [SerializeField] private float armingMoveSpeed = 1.1f;

    [Header("Combat")]
    [Tooltip("Un corazon lleno. En este proyecto 2 HP = 1 corazon.")]
    [SerializeField] private float playerExplosionDamage = 2f;
    [Tooltip("Golpe fuerte a enemigos cercanos (una rata tiene 30 HP).")]
    [SerializeField] private float enemyExplosionDamage = 15f;
    [SerializeField] private float hitKnockbackForce = 7.5f;
    [SerializeField] private float explosionKnockbackForce = 10f;

    [Header("Deform")]
    [SerializeField] private float maxDeformScale = 1.45f;
    [SerializeField] private float deformPulse = 0.07f;

    [Header("Layers")]
    [SerializeField] private LayerMask damageLayers;
    [SerializeField] private LayerMask destructibleLayers;

    [Header("Feedback")]
    [SerializeField] private DamageFlash damageFlash;

    private EnemyHealth enemyHealth;
    private Rigidbody2D rb;
    private bool hasExploded;
    private bool deforming;
    private float deformProgress;
    private float knockbackUntil;
    private Vector3 restScale;
    public float ExplosionDamage => playerExplosionDamage;

    public float ArmingRange => armingRange;
    public float CancelRange => Mathf.Max(cancelRange, armingRange + 0.25f);
    public float CountdownDuration => countdownDuration;
    public float HitKnockbackDuration => hitKnockbackDuration;
    public float ChaseSpeed => chaseSpeed;
    public float ArmingMoveSpeed => armingMoveSpeed;
    public bool IsBeingKnockedBack => Time.time < knockbackUntil;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        rb = GetComponent<Rigidbody2D>();
        restScale = transform.localScale;

        if (damageFlash == null)
            damageFlash = GetComponent<DamageFlash>();

        countdownDuration = 2f;
    }

    private void Start()
    {
        if (enemyHealth != null)
            enemyHealth.OnDamaged += HandleDamaged;

        SnapToArenaCenter();
    }

    private void OnDestroy()
    {
        if (enemyHealth != null)
            enemyHealth.OnDamaged -= HandleDamaged;
    }

    private void LateUpdate()
    {
        if (!deforming)
            return;

        float swell = Mathf.Lerp(1f, maxDeformScale, deformProgress);
        float pulse = 1f + Mathf.Sin(Time.time * 14f) * deformPulse * deformProgress;
        transform.localScale = restScale * swell * pulse;
    }

    public void BeginArmingFeedback()
    {
        deforming = true;
        deformProgress = 0f;
        damageFlash?.StartLoopFlash();
    }

    public void UpdateArmingFeedback(float normalizedProgress)
    {
        deformProgress = Mathf.Clamp01(normalizedProgress);
    }

    public void EndArmingFeedback()
    {
        deforming = false;
        deformProgress = 0f;
        transform.localScale = restScale;
        damageFlash?.StopLoopFlash();
    }

    public void ApplyHitKnockback(Vector2 fromPosition)
    {
        if (rb == null || hasExploded) return;

        Vector2 direction = ((Vector2)transform.position - fromPosition).normalized;
        if (direction.sqrMagnitude < 0.01f)
            direction = Vector2.up;

        rb.linearVelocity = direction * hitKnockbackForce;
        knockbackUntil = Time.time + hitKnockbackDuration;
    }

    public void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        EndArmingFeedback();
        CameraShake.Play(0.38f, 0.32f);

        Vector2 center = transform.position;
        LayerMask explosionMask = damageLayers | destructibleLayers;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, explosionRadius, explosionMask);

        var damaged = new HashSet<IDamageable>();
        var knockedBack = new HashSet<Rigidbody2D>();
        var brokenStones = new HashSet<BreakableStone>();
        var hitTrash = new HashSet<BreakableTrash>();

        foreach (Collider2D hit in hits)
        {
            if (IsOwnCollider(hit))
                continue;

            if (hit.TryGetComponent<BreakableStone>(out BreakableStone stone))
            {
                if (!IsWithinExplosionRadius(center, stone.transform.position))
                    continue;

                if (brokenStones.Add(stone))
                    stone.BreakFromExplosion();

                continue;
            }

            if (hit.TryGetComponent<BreakableTrash>(out BreakableTrash trash))
            {
                if (!IsWithinExplosionRadius(center, trash.transform.position))
                    continue;

                if (hitTrash.Add(trash))
                    trash.TakeHit(playerExplosionDamage);

                continue;
            }

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable is not MonoBehaviour damageableBehaviour)
                continue;

            if (!IsWithinExplosionRadius(center, damageableBehaviour.transform.position))
                continue;

            if (!damaged.Add(damageable))
                continue;

            Vector2 knockDirection = ((Vector2)damageableBehaviour.transform.position - center).normalized;
            if (knockDirection.sqrMagnitude < 0.01f)
                knockDirection = Vector2.up;

            Rigidbody2D targetRb = damageableBehaviour.GetComponent<Rigidbody2D>();
            if (targetRb != null && knockedBack.Add(targetRb) && damageable is not PlayerHealth)
                ApplyKnockbackToRigidbody(targetRb, knockDirection, explosionKnockbackForce);

            damageable.TakeDamage(ResolveExplosionDamage(damageable));
        }

        if (enemyHealth != null)
            enemyHealth.TakeDamage(enemyHealth.MaxHealth);
        else
            Destroy(gameObject);
    }

    private float ResolveExplosionDamage(IDamageable damageable)
    {
        if (damageable is PlayerHealth)
            return playerExplosionDamage;

        return enemyExplosionDamage;
    }

    private void HandleDamaged()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        ApplyHitKnockback(player.transform.position);
    }

    private void SnapToArenaCenter()
    {
        RoomInstance room = GetComponentInParent<RoomInstance>();
        if (room == null) return;

        Vector3 center = room.transform.position;
        bool initialized = false;
        Bounds bounds = default;

        Collider2D[] colliders = room.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D col = colliders[i];
            if (col == null || !col.enabled) continue;
            if (col.gameObject.layer != 10) continue;

            if (!initialized)
            {
                bounds = col.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(col.bounds);
            }
        }

        if (initialized)
            center = bounds.center;

        center.z = transform.position.z;
        transform.position = center;
    }

    private bool IsWithinExplosionRadius(Vector2 center, Vector2 targetPosition)
    {
        return (targetPosition - center).sqrMagnitude <= explosionRadius * explosionRadius;
    }

    private bool IsOwnCollider(Collider2D hit)
    {
        return hit.transform == transform || hit.transform.IsChildOf(transform);
    }

    private static void ApplyKnockbackToRigidbody(Rigidbody2D targetRb, Vector2 direction, float force)
    {
        KnockbackReceiver knockback = targetRb.GetComponent<KnockbackReceiver>();
        if (knockback != null)
        {
            knockback.ApplyKnockback(direction, force);
            return;
        }

        targetRb.linearVelocity = direction.normalized * force;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, armingRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, cancelRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
