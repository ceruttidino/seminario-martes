using UnityEngine;

// Contiene la configuracion y logica del mordisco de la Regenerating Rat.
// El mecanismo de regeneracion (RatRegeneration/RatBody) es independiente de
// este componente y no se ve afectado por el estado de ataque.
[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyBehaviour))]
public class RegeneratingRat : MonoBehaviour
{
    [Header("Ataque (mordisco)")]
    [Tooltip("Distancia a la que la rata se detiene y comienza el ataque.")]
    [SerializeField] private float attackTriggerRange = 0.75f;
    [Tooltip("Distancia maxima a la que el mordisco puede conectar (un poco mayor a attackTriggerRange para tolerar el movimiento durante el windup).")]
    [SerializeField] private float attackHitRange = 0.85f;
    [Tooltip("Duracion total de la animacion de ataque.")]
    [SerializeField] private float attackDuration = 1f;
    [Tooltip("Momento (dentro de attackDuration) en el que el mordisco realmente hace daño.")]
    [SerializeField] private float hitTiming = 0.5f;
    [SerializeField] private float attackDamage = 1f;
    [SerializeField] private DamageFlash damageFlash;

    public float AttackTriggerRange => attackTriggerRange;
    public float AttackDuration => attackDuration;
    public float HitTiming => hitTiming;

    private void Awake()
    {
        if (damageFlash == null)
            damageFlash = GetComponent<DamageFlash>();
    }

    // Aviso visual de que esta con la boca preparada para morder (mismo mecanismo
    // que usan ExplosiveHedgehog y PoisonousSnake para telegrafiar su ataque).
    public void BeginAttackFeedback()
    {
        damageFlash?.StartLoopFlash();
    }

    public void EndAttackFeedback()
    {
        damageFlash?.StopLoopFlash();
    }

    // Se llama en el momento exacto del mordisco (hitTiming). Vuelve a chequear
    // la distancia por si el jugador esquivo alejandose durante el windup: en
    // ese caso el ataque falla y no hace daño.
    public void TryStrike(Transform player)
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance > attackHitRange) return;

        IDamageable damageable = player.GetComponent<IDamageable>();
        damageable?.TakeDamage(attackDamage);
    }
}
