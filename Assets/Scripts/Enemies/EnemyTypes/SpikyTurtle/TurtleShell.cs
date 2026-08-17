using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class TurtleShell : MonoBehaviour
{
    [SerializeField] private float pushSpeed = 12f;
    [SerializeField] private float damage = 20f;
    [SerializeField] private int maxHits = 3;
    [SerializeField] private LayerMask enemyLayer;

    private Rigidbody2D rb;
    private int hitCount;
    private Vector2 currentDirection;
    private bool isMoving;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Push(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) return;

        currentDirection = direction.normalized;
        isMoving = true;
        rb.linearVelocity = currentDirection * pushSpeed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isMoving) return;

        IDamageable damageable = collision.gameObject.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(damage);

            bool isEnemy = ((1 << collision.gameObject.layer) & enemyLayer.value) != 0;
            if (isEnemy)
                RegisterHit();

            return;
        }

        if (collision.contacts.Length > 0)
        {
            Vector2 normal = collision.contacts[0].normal;
            currentDirection = Vector2.Reflect(currentDirection, normal).normalized;
            rb.linearVelocity = currentDirection * pushSpeed;
        }
    }

    private void RegisterHit()
    {
        hitCount++;

        if (hitCount >= maxHits)
        {
            Destroy(gameObject);
        }
    }
}
