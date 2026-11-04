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

    [Header("Animacion")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameRate = 12f;

    private Vector2 direction;
    private SpriteRenderer body;
    private float frameTimer;
    private int frameIndex;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        body = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        AdvanceFrame();
    }

    private void AdvanceFrame()
    {
        if (body == null || frames == null || frames.Length == 0 || frameRate <= 0f)
            return;

        frameTimer += Time.deltaTime;
        float step = 1f / frameRate;
        while (frameTimer >= step)
        {
            frameTimer -= step;
            frameIndex = (frameIndex + 1) % frames.Length;
            if (frames[frameIndex] != null)
                body.sprite = frames[frameIndex];
        }
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
