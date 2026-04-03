using UnityEngine;

public class AreaAttack : MonoBehaviour, IAttack
{
    [Header("References")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask trashLayer;

    [Header("Area Attack")]
    [SerializeField] private float damage = 20f;
    [SerializeField] private float cooldown = 2f;
    [SerializeField] private float radius = 2f;

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

        LayerMask combinedMask = enemyLayer | trashLayer;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, combinedMask);

        Debug.Log($"AreaAttack ejecutado - Hits detectados: {hits.Length}");

        bool hitSomething = false;

        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out IDamageable damageable))
            {
                damageable.TakeDamage(damage);
                Debug.Log($"Daño a enemigo: {hit.name}");
                hitSomething = true;
            }
            else if (hit.TryGetComponent<BreakableTrash>(out BreakableTrash trash))
            {
                trash.TakeHit(damage);
                Debug.Log($"¡Golpe a cofre! {hit.name}");
                hitSomething = true;
            }
        }

        if (!hitSomething)
            Debug.LogWarning("AreaAttack no golpeó nada");
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
}