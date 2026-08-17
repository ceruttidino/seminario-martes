using UnityEngine;


[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyBehaviour))]
public class SpikyTurtle : MonoBehaviour
{
    [Header("Charge Settings")]
    [SerializeField] private float chargeWindup = 0.5f;
    [SerializeField] private float chargeDuration = 3f;
    [SerializeField] private float chargeSpeed = 8f;
    [SerializeField] private float chargeDamage = 15f;
    [SerializeField] private float detectionRange = 4f;

    [Header("Upside Down")]
    [SerializeField] private float upsideDownDuration = 1f;

    [Header("Shell")]
    [SerializeField] private GameObject shellPrefab;

    private EnemyHealth health;
    private EnemyBehaviour behaviour;

    public bool IsUpsideDown { get; private set; }

    public float ChargeWindup => chargeWindup;
    public float ChargeDuration => chargeDuration;
    public float ChargeSpeed => chargeSpeed;
    public float UpsideDownDuration => upsideDownDuration;
    public float DetectionRange => detectionRange;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        behaviour = GetComponent<EnemyBehaviour>();
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

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (behaviour == null) return;
        if (!behaviour.CurrentStateIs<TurtleChargeState>()) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        damageable?.TakeDamage(chargeDamage);
    }

    private void DropShell()
    {
        if (shellPrefab != null)
        {
            Instantiate(shellPrefab, transform.position, Quaternion.identity);
        }
    }
}
