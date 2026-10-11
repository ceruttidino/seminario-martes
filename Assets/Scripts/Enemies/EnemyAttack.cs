using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float damage = 10f;
    [Tooltip("Alcance medido desde el CENTRO del cuerpo del enemigo. Si el cuerpo es grande (ej: la Araña), el alcance real se amplia solo para llegar siempre hasta la superficie del cuerpo + Surface Reach.")]
    [SerializeField] private float attackRange = 1f;
    [Tooltip("Cuanto llega el golpe mas alla de la SUPERFICIE del collider del enemigo. Garantiza que un enemigo grande pueda pegarle al jugador que esta tocando su cuerpo.")]
    [SerializeField] private float surfaceReach = 0.35f;
    [SerializeField] private float attackCooldown = 1f;

    public float Damage => damage;
    public float AttackRange => attackRange;
    public float AttackCooldown => attackCooldown;
    [SerializeField] private LayerMask targetLayer;

    private float lastAttackTime = float.NegativeInfinity;
    private Collider2D[] bodyColliders;
    private readonly Collider2D[] hitBuffer = new Collider2D[8];
    private ContactFilter2D targetFilter;
    private bool filterReady;

    private void Awake()
    {
        CacheBody();
    }

    private void CacheBody()
    {
        Collider2D[] all = GetComponents<Collider2D>();
        int solid = 0;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && !all[i].isTrigger)
                solid++;
        }

        bodyColliders = new Collider2D[solid];
        int index = 0;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && !all[i].isTrigger)
                bodyColliders[index++] = all[i];
        }
    }

    /// <summary>
    /// Centro y radio (en mundo) del cuerpo del enemigo, calculados con sus colliders solidos. El golpe se mide desde
    /// ahi y NO desde el pivot del transform: el collider puede estar desplazado y puede ser mucho mas grande que el
    /// alcance configurado (Araña: escala 4), lo que dejaba al jugador dentro del cuerpo pero fuera del alcance.
    /// </summary>
    public void GetBody(out Vector2 center, out float radius)
    {
        center = transform.position;
        radius = 0f;

        if (bodyColliders == null)
            CacheBody();

        bool any = false;
        Bounds bounds = default;
        for (int i = 0; i < bodyColliders.Length; i++)
        {
            Collider2D col = bodyColliders[i];
            if (col == null || !col.enabled)
                continue;

            if (!any)
            {
                bounds = col.bounds;
                any = true;
            }
            else
            {
                bounds.Encapsulate(col.bounds);
            }
        }

        if (!any)
            return;

        center = bounds.center;
        radius = Mathf.Max(bounds.extents.x, bounds.extents.y);
    }

    /// <summary>Alcance real del golpe desde el centro del cuerpo: nunca menor que la superficie del cuerpo + Surface Reach.</summary>
    public float Reach
    {
        get
        {
            GetBody(out _, out float radius);
            return Mathf.Max(attackRange, radius + surfaceReach);
        }
    }

    public bool CanAttack()
    {
        return Time.time >= lastAttackTime + attackCooldown;
    }

    public bool TryAttack()
    {
        if (!CanAttack()) return false;

        if (!filterReady)
        {
            targetFilter = new ContactFilter2D();
            targetFilter.useLayerMask = true;
            targetFilter.layerMask = targetLayer;
            targetFilter.useTriggers = true;
            filterReady = true;
        }

        GetBody(out Vector2 center, out float radius);
        float reach = Mathf.Max(attackRange, radius + surfaceReach);

        // Se revisan todos los colliders del jugador dentro del alcance (cuerpo y pies) y se pega al primero que
        // tenga vida: antes solo se miraba UN collider y se medía desde el pivot del enemigo.
        int count = Physics2D.OverlapCircle(center, reach, targetFilter, hitBuffer);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = hitBuffer[i];
            if (hit == null) continue;

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null) continue;

            damageable.TakeDamage(damage);
            lastAttackTime = Time.time;
            return true;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        if (Application.isPlaying)
        {
            GetBody(out Vector2 center, out float radius);
            Gizmos.DrawWireSphere(center, Mathf.Max(attackRange, radius + surfaceReach));
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
