using UnityEngine;

public class QuickAttack : MonoBehaviour, IAttack
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerDirection;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask trashLayer;

    [Header("Quick Attack")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float cooldown = 0.5f;
    [SerializeField] private float attackDistance = 1f;
    [SerializeField] private Vector2 attackBoxSize = new Vector2(1.2f, 0.8f);

    private float lastUseTime = -999f;

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
        return Time.time >= lastUseTime + cooldown;
    }

    public void Execute()
    {
        if (!CanExecute()) return;

        lastUseTime = Time.time;

        Vector2 lookDirection = playerDirection.LastLookDirection;
        Vector2 attackCenter = (Vector2)transform.position + lookDirection * attackDistance;

        // Combinamos las layers correctamente
        LayerMask combinedMask = enemyLayer | trashLayer;

        Collider2D[] hits = Physics2D.OverlapBoxAll(attackCenter, attackBoxSize, 0f, combinedMask);

        Debug.Log($"QuickAttack ejecutado - Hits detectados: {hits.Length}");

        bool hitSomething = false;

        foreach (Collider2D hit in hits)
        {
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                Debug.Log($"Daño a enemigo: {hit.name}");
                hitSomething = true;
            }
            else if (hit.TryGetComponent<BreakableTrash>(out BreakableTrash trash))
            {
                trash.TakeHit(damage);
                Debug.Log($"¡Golpe a cofre! {hit.name} - Hits actuales: {trash.GetCurrentHits()}");
                hitSomething = true;
            }
        }

        if (!hitSomething)
            Debug.LogWarning("QuickAttack no golpeó nada (ni enemigo ni cofre)");
    }

    private void OnDrawGizmosSelected()
    {
        if (playerDirection == null) return;
        Gizmos.color = Color.red;
        Vector2 lookDirection = playerDirection.LastLookDirection;
        Vector2 attackCenter = (Vector2)transform.position + lookDirection * attackDistance;
        Gizmos.DrawWireCube(attackCenter, attackBoxSize);
    }

    public void IncreaseDamage(float amount)
    {
        damage += amount;
    }
}