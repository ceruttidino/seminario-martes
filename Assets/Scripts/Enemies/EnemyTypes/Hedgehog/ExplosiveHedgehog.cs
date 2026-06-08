using System.Collections.Generic;
using UnityEngine;

public class ExplosiveHedgehog : MonoBehaviour
{
    [Header("Ranges")]
    [SerializeField] private float armingRange = 1.5f;
    [SerializeField] private float explosionRadius = 2.5f;

    [Header("Timing")]
    [SerializeField] private float countdownDuration = 4f;
    [SerializeField] private float hitKnockbackDuration = 0.25f;

    [Header("Combat")]
    [SerializeField] private float explosionDamage = 3f;
    [SerializeField] private float hitKnockbackForce = 8f;
    [SerializeField] private float explosionKnockbackForce = 10f;

    [Header("Layers")]
    [SerializeField] private LayerMask damageLayers;

    [Header("Feedback")]
    [SerializeField] private DamageFlash damageFlash;

    private EnemyHealth enemyHealth;
    private Rigidbody2D rb;
    private bool hasExploded;

    public float ArmingRange => armingRange;
    public float CountdownDuration => countdownDuration;
    public float HitKnockbackDuration => hitKnockbackDuration;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        rb = GetComponent<Rigidbody2D>();

        if (damageFlash == null)
            damageFlash = GetComponent<DamageFlash>();
    }

    public void BeginArmingFeedback()
    {
        damageFlash?.StartLoopFlash();
    }

    public void EndArmingFeedback()
    {
        damageFlash?.StopLoopFlash();
    }

    public void ApplyHitKnockback(Vector2 fromPosition)
    {
        if (rb == null) return;

        Vector2 direction = ((Vector2)transform.position - fromPosition).normalized;
        if (direction.sqrMagnitude < 0.01f)
            direction = Vector2.up;

        rb.linearVelocity = direction * hitKnockbackForce;
    }

    public void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        EndArmingFeedback();

        Vector2 center = transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, explosionRadius, damageLayers);
        var damaged = new HashSet<IDamageable>();
        var knockedBack = new HashSet<Rigidbody2D>();

        foreach (Collider2D hit in hits)
        {
            if (hit.transform.root == transform.root)
                continue;

            Vector2 knockDirection = ((Vector2)hit.transform.position - center).normalized;
            if (knockDirection.sqrMagnitude < 0.01f)
                knockDirection = Vector2.up;

            Rigidbody2D targetRb = hit.attachedRigidbody;
            if (targetRb != null && knockedBack.Add(targetRb))
                ApplyKnockbackToRigidbody(targetRb, knockDirection, explosionKnockbackForce);

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null && damaged.Add(damageable))
                damageable.TakeDamage(explosionDamage);
        }

        if (enemyHealth != null)
            enemyHealth.TakeDamage(enemyHealth.MaxHealth);
        else
            Destroy(gameObject);
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

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
