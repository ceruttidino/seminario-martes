using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyBehaviour))]
public class PoisonousSnake : MonoBehaviour
{
    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 3f;
    [SerializeField] private float attackRange = 0.7f;

    [Header("Attack")]
    [SerializeField] private float windupDuration = 0.5f;
    [SerializeField] private float attackDamage = 1f; // media vida de corazon
    [SerializeField] private DamageFlash damageFlash;

    [Header("Flee")]
    [SerializeField] private float fleeSpeed = 4f;
    [SerializeField] private float fleeDuration = 4f;

    [Header("Poison")]
    [SerializeField] private float poisonMinDuration = 3f;
    [SerializeField] private float poisonMaxDuration = 5f;
    [SerializeField] private float poisonTickDamage = 1f; // media vida de corazon por tic

    public float ChaseSpeed => chaseSpeed;
    public float AttackRange => attackRange;
    public float WindupDuration => windupDuration;
    public float FleeSpeed => fleeSpeed;
    public float FleeDuration => fleeDuration;

    private void Awake()
    {
        if (damageFlash == null)
            damageFlash = GetComponent<DamageFlash>();
    }

    // Aviso visual de que esta preparando el ataque (mismo mecanismo que usa
    // ExplosiveHedgehog para su estado de armado), en vez de un estado extra
    // en el Animator.
    public void BeginWindupFeedback()
    {
        damageFlash?.StartLoopFlash();
    }

    public void EndWindupFeedback()
    {
        damageFlash?.StopLoopFlash();
    }

    // Se llama al finalizar el windup. Vuelve a chequear el rango por si el jugador escapo mientras se preparaba.
    public void PerformAttack(Transform player)
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance > attackRange + 0.15f) return;

        IDamageable damageable = player.GetComponent<IDamageable>();
        damageable?.TakeDamage(attackDamage);

        PlayerPoisonStatus poisonStatus = player.GetComponent<PlayerPoisonStatus>();
        if (poisonStatus == null)
            poisonStatus = player.gameObject.AddComponent<PlayerPoisonStatus>();

        float duration = Random.Range(poisonMinDuration, poisonMaxDuration);
        poisonStatus.ApplyPoison(duration, poisonTickDamage);
    }
}
