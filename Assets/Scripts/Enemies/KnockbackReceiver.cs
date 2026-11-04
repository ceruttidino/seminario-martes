using UnityEngine;

public class KnockbackReceiver : MonoBehaviour
{
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void ApplyKnockback(Vector2 direction, float force)
    {
        if (direction.sqrMagnitude < 0.01f) return;

        EnemyMovement movement = GetComponent<EnemyMovement>();
        if (movement != null && GetComponent<BossBase>() == null)
        {
            movement.BeginKnockback(direction.normalized * force, 0.15f);
            return;
        }

        if (rb == null) return;
        rb.linearVelocity = direction.normalized * force;
    }
}
