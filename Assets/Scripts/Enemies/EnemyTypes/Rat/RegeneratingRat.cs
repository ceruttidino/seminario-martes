using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyBehaviour))]
public class RegeneratingRat : MonoBehaviour
{
    [Header("Ataque (mordisco)")]
    [Tooltip("Distancia a la que la rata se detiene y comienza el ataque.")]
    [SerializeField] private float attackTriggerRange = 1.1f;
    [Tooltip("Distancia maxima a la que el mordisco puede conectar (un poco mayor a attackTriggerRange para tolerar el movimiento durante el windup).")]
    [SerializeField] private float attackHitRange = 1.25f;
    [Tooltip("Duracion total de la animacion de ataque.")]
    [SerializeField] private float attackDuration = 1f;
    [Tooltip("Momento (dentro de attackDuration) en el que el mordisco realmente hace daño.")]
    [SerializeField] private float hitTiming = 0.5f;
    [SerializeField] private float attackDamage = 1f;
    public float AttackDamage => attackDamage;
    [SerializeField] private DamageFlash damageFlash;

    public float AttackTriggerRange => attackTriggerRange;
    public float AttackDuration => attackDuration;
    public float HitTiming => hitTiming;

    [Header("Revivir")]
    [SerializeField] private Animator animator;
    [SerializeField] private float reviveDuration = 1f;
    [SerializeField] private Collider2D hitCollider;

    private EnemyBehaviour behaviour;
    private EnemyMovement movement;

    private void Awake()
    {
        if (damageFlash == null)
            damageFlash = GetComponent<DamageFlash>();

        behaviour = GetComponent<EnemyBehaviour>();
        movement = GetComponent<EnemyMovement>();
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void BeginRevive()
    {
        StartCoroutine(ReviveRoutine());
    }

    public void BeginAttackFeedback()
    {
        damageFlash?.StartLoopFlash();
    }

    public void EndAttackFeedback()
    {
        damageFlash?.StopLoopFlash();
    }

    public void TryStrike(Transform player)
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance > attackHitRange) return;

        IDamageable damageable = player.GetComponent<IDamageable>();
        damageable?.TakeDamage(attackDamage);
    }

    public void OnReviveAnimationComplete()
    {
        if (hitCollider != null) hitCollider.enabled = true;
        if (behaviour != null) behaviour.enabled = true;
    }

    private System.Collections.IEnumerator ReviveRoutine()
    {
        if (behaviour != null) behaviour.enabled = false;

        movement?.Move(Vector2.zero);
        if (hitCollider != null) hitCollider.enabled = false;

        if (animator != null)
        {
            animator.Play("RatReviving", 0, 0f);
            animator.Update(0f);
        }

        yield return new WaitForSeconds(reviveDuration);

        if (hitCollider != null) hitCollider.enabled = true;
        if (behaviour != null) behaviour.enabled = true;
    }
}
