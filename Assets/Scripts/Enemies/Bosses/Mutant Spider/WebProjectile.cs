using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WebProjectile : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private float speed = 7f;
    [SerializeField] private float damage = 1f;
    [SerializeField] private float lifetime = 4f;

    [Header("Layers")]
    [SerializeField] private LayerMask wallLayer;

    private Vector2 direction;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    public void Initialize(Vector2 shootDirection)
    {
        direction = shootDirection.normalized;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            IDamageable damageable = other.GetComponent<IDamageable>();

            if (damageable != null)
                damageable.TakeDamage(damage);

            Destroy(gameObject);
            return;
        }

        if ((wallLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            Destroy(gameObject);
        }
    }
}
