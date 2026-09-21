using UnityEngine;


[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyBehaviour))]
public class SpikyTurtle : MonoBehaviour
{
    [Header("Charge Settings")]
    [Tooltip("Tiempo de preparacion antes de arrancar la embestida (GDD: 0.8s).")]
    [SerializeField] private float chargeWindup = 0.8f;
    [Tooltip("Duracion de la embestida en si (GDD: 1.5s).")]
    [SerializeField] private float chargeDuration = 1.5f;
    [SerializeField] private float chargeSpeed = 8f;
    [SerializeField] private float chargeDamage = 1f;
    public float ChargeDamage => chargeDamage;
    [SerializeField] private float detectionRange = 4f;

    [Header("Upside Down")]
    [SerializeField] private float upsideDownDuration = 1f;

    [Header("Shell")]
    [SerializeField] private GameObject shellPrefab;

    [Header("Feedback")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private DamageFlash damageFlash;

    private EnemyHealth health;
    private EnemyBehaviour behaviour;
    private Rigidbody2D rb;

    public bool IsUpsideDown { get; private set; }

    public float ChargeWindup => chargeWindup;
    public float ChargeDuration => chargeDuration;
    public float ChargeSpeed => chargeSpeed;
    public float UpsideDownDuration => upsideDownDuration;
    public float DetectionRange => detectionRange;

    private bool hitPlayerThisCharge;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        behaviour = GetComponent<EnemyBehaviour>();
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.mass = 8f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (damageFlash == null)
            damageFlash = GetComponent<DamageFlash>();
    }

    private void Start()
    {
        SetUpsideDown(false);

        if (health != null)
            health.OnDeath += DropShell;
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnDeath -= DropShell;
    }

    public void SetUpsideDown(bool value)
    {
        IsUpsideDown = value;
        health?.SetDamageable(value);

        if (spriteRenderer != null)
            spriteRenderer.flipY = value;
    }

    public void BeginWindupFeedback()
    {
        damageFlash?.StartLoopFlash();
    }

    public void EndWindupFeedback()
    {
        damageFlash?.StopLoopFlash();
    }

    public void BeginCharge()
    {
        hitPlayerThisCharge = false;
    }

    public void TryHitPlayerDuringCharge()
    {
        if (hitPlayerThisCharge)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        float reach = 0.7f;
        if (Vector2.Distance(transform.position, player.transform.position) > reach)
            return;

        hitPlayerThisCharge = true;
        player.GetComponent<IDamageable>()?.TakeDamage(chargeDamage);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (behaviour == null) return;
        if (!behaviour.CurrentStateIs<TurtleChargeState>()) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        damageable?.TakeDamage(chargeDamage);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (rb == null || behaviour == null) return;
        if (behaviour.CurrentStateIs<TurtleChargeState>()) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        rb.linearVelocity = Vector2.zero;
    }

    private void DropShell()
    {
        if (shellPrefab == null) return;

        // Si no es hijo de la room, al cambiar de sala el caparazón queda suelto y sigue al jugador.
        Transform roomParent = GetComponentInParent<RoomInstance>()?.transform ?? transform.parent;
        Instantiate(shellPrefab, transform.position, Quaternion.identity, roomParent);
    }
}
