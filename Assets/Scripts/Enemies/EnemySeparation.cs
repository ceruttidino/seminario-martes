using UnityEngine;

public class EnemySeparation : MonoBehaviour
{
    [SerializeField] private float radius = 0.55f;
    [SerializeField] private float push = 2.8f;

    private Rigidbody2D rb;
    private readonly Collider2D[] hits = new Collider2D[8];

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (rb == null || GetComponent<BossBase>() != null)
            return;

        HitReaction reaction = GetComponent<HitReaction>();
        if (reaction != null && (reaction.IsStunned || reaction.IsInKnockback))
            return;

        EnemyMovement sliding = GetComponent<EnemyMovement>();
        if (sliding != null && sliding.IsSliding)
            return;

        int count = Physics2D.OverlapCircle(rb.position, radius, ContactFilter2D.noFilter, hits);
        Vector2 separation = Vector2.zero;
        int others = 0;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || hit.attachedRigidbody == null || hit.attachedRigidbody == rb)
                continue;
            if (hit.GetComponent<EnemyHealth>() == null)
                continue;

            Vector2 away = rb.position - hit.attachedRigidbody.position;
            float dist = away.magnitude;
            if (dist < 0.001f)
                away = Random.insideUnitCircle.normalized;
            else
                away /= dist;

            float overlap = Mathf.Max(0.05f, radius - dist);
            separation += away * overlap;
            others++;
        }

        if (others == 0)
            return;

        // El cuerpo es kinematic y lo mueve el NavMesh. Una fuerza del Rigidbody no lo separa
        // y, si el cuerpo fuera dinamico, pelearia con el avoidance.
        EnemyMovement movement = GetComponent<EnemyMovement>();
        if (movement != null)
        {
            movement.AddSeparation(separation * push * 0.05f);
            return;
        }

        rb.AddForce(separation * push, ForceMode2D.Force);
    }
}
