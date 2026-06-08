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
        if (rb == null) return;
        if (direction.sqrMagnitude < 0.01f) return;

        rb.linearVelocity = direction.normalized * force;
    }
}
