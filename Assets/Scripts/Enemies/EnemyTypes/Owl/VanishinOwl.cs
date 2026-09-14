using UnityEngine;

public class VanishingOwl : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float stalkSpeed = 3.5f;
    [Tooltip("Si esta activo, vuela recto ignorando el esquive de obstaculos de EnemyMovement.")]
    [SerializeField] private bool flyStraight = false;

    [Header("Distances")]
    [SerializeField] private float revealDistance = 1.6f;
    [SerializeField] private float attackRadius = 1.9f;

    [Header("Timing")]
    [SerializeField] private float windupTime = 0.4f;
    [SerializeField] private float recoverTime = 0.6f;

    [Header("Stalk Cycle")]
    [SerializeField] private float invisibleDuration = 3f;
    [SerializeField] private float shadowChaseDuration = 1.5f;

    [Header("Damage")]
    [Tooltip("2 = un corazon entero (en tu PlayerHealth 1 corazon = 2 puntos).")]
    [SerializeField] private float heartDamage = 2f;

    [Header("Visuals")]
    [Tooltip("El cuerpo normal, visible al revelarse/atacar. SpriteRenderer de la raiz.")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [Tooltip("La shadow form (buho oscuro + ojos). Objeto 'Eyes' en la jerarquia.")]
    [SerializeField] private GameObject shadowFormObject;
    [Tooltip("La sombra proyectada ('Shadow'). Se apaga en fase invisible.")]
    [SerializeField] private GameObject projectedShadow;
    [SerializeField] private Animator animator;
    [Header("Refs")]
    [SerializeField] private EnemyMovement movement;

    public float RevealDistance => revealDistance;
    public float AttackRadius => attackRadius;
    public float WindupTime => windupTime;
    public float RecoverTime => recoverTime;
    public float HeartDamage => heartDamage;
    public float InvisibleDuration => invisibleDuration;
    public float ShadowChaseDuration => shadowChaseDuration;

    private void Awake()
    {
        if (movement == null) movement = GetComponent<EnemyMovement>();
        if (animator == null) animator = GetComponent<Animator>();
        if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
    }
    public void MoveTowards(Vector2 target)
    {
        if (movement == null) return;
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        movement.Move(dir, stalkSpeed, !flyStraight);
    }

    public void Stop()
    {
        if (movement != null) movement.Move(Vector2.zero);
    }

    public void FaceTarget(Vector2 target)
    {
        if (movement == null) return;
        movement.Face(target - (Vector2)transform.position);
    }


    public void TriggerAttack()
    {
        if (animator != null) animator.SetTrigger("Attack");
    }

    public void ResetAttackTrigger()
    {
        if (animator != null) animator.ResetTrigger("Attack");
    }

    public void EnterInvisible()
    {
        if (animator != null) animator.SetBool("IsStealthed", true);
        ResetAttackTrigger();
        if (bodyRenderer != null) bodyRenderer.enabled = false;
        if (shadowFormObject != null) shadowFormObject.SetActive(false);
        if (projectedShadow != null) projectedShadow.SetActive(false);
    }
    public void EnterShadow()
    {
        if (animator != null) animator.SetBool("IsStealthed", true);
        if (bodyRenderer != null) bodyRenderer.enabled = false;
        if (shadowFormObject != null) shadowFormObject.SetActive(true);
        if (projectedShadow != null) projectedShadow.SetActive(true);
    }

    public void Reveal()
    {
        if (animator != null) animator.SetBool("IsStealthed", false);
        if (bodyRenderer != null) bodyRenderer.enabled = true;
        if (shadowFormObject != null) shadowFormObject.SetActive(false);
        if (projectedShadow != null) projectedShadow.SetActive(true);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, revealDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}