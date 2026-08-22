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
    [SerializeField] private float chargeDamage = 15f;
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

        // No hay sprite/animacion especifica de "boca arriba" todavia: se vuelca
        // el mismo sprite verticalmente para que el jugador pueda distinguir a
        // simple vista cuando la tortuga es vulnerable.
        if (spriteRenderer != null)
            spriteRenderer.flipY = value;
    }

    // Aviso visual de que esta por embestir (mismo mecanismo que usan
    // PoisonousSnake/ExplosiveHedgehog/RegeneratingRat para telegrafiar su ataque).
    public void BeginWindupFeedback()
    {
        damageFlash?.StartLoopFlash();
    }

    public void EndWindupFeedback()
    {
        damageFlash?.StopLoopFlash();
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
        if (shellPrefab == null) return;

        // Sin padre, el caparazon queda en la raiz de la escena: al cambiar de
        // room solo se desactiva el RoomInstance de la sala anterior, y como el
        // caparazon no es hijo de ese RoomInstance queda activo y "sigue" al
        // jugador a la siguiente sala. Lo parenteamos a la room actual (misma
        // logica que ya se usa para los buffs dejados en el piso) para que se
        // desactive junto con ella al salir y quede solo en la sala donde cayo.
        Transform roomParent = GetComponentInParent<RoomInstance>()?.transform ?? transform.parent;
        Instantiate(shellPrefab, transform.position, Quaternion.identity, roomParent);
    }
}
