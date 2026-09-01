using System.Collections;
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
    [SerializeField] private float windupTime = 0.7f;
    [SerializeField] private float recoverTime = 0.6f;

    [Header("Damage")]
    [Tooltip("2 = un corazon entero (en tu PlayerHealth 1 corazon = 2 puntos).")]
    [SerializeField] private float heartDamage = 2f;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private GameObject eyeGlow;
    [SerializeField] private Animator animator;

    [Header("Refs")]
    [SerializeField] private EnemyMovement movement;

    public float RevealDistance => revealDistance;
    public float AttackRadius => attackRadius;
    public float WindupTime => windupTime;
    public float RecoverTime => recoverTime;
    public float HeartDamage => heartDamage;

    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (movement == null) movement = GetComponent<EnemyMovement>();
        if (animator == null) animator = GetComponent<Animator>();
        if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    // --- Movimiento (via EnemyMovement: corre fly-anim, flip y X/Y) ---

    public void MoveTowards(Vector2 target)
    {
        if (movement == null) return;
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        movement.Move(dir, stalkSpeed, !flyStraight);
    }

    public void Stop()
    {
        if (movement != null) movement.Move(Vector2.zero); // frena y pone AnimSpeed a 0
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

    // --- Visuales ---

    public void EnterStealth()
    {
        if (animator != null) animator.SetBool("IsStealthed", true);
    }

    public void Reveal()
    {
        if (animator != null) animator.SetBool("IsStealthed", false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, revealDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}