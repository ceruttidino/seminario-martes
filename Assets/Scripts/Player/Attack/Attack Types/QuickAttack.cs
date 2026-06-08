using UnityEngine;

public class QuickAttack : MonoBehaviour, IAttack
{
    [Header("References")]
    [SerializeField] private PlayerAim playerAim;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask trashLayer;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Quick Attack")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float cooldown = 0.5f;
    [SerializeField] private float attackDistance = 1f;
    [SerializeField] private Vector2 attackBoxSize = new Vector2(1.2f, 0.8f);

    [SerializeField] private AudioSource sfxSource;

    private float lastUseTime = -999f;
    private Vector2 lastAttackDirection = Vector2.down;

    private void Awake()
    {
        if (playerAim == null)
            playerAim = GetComponent<PlayerAim>();

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public float CooldownRemaining
    {
        get
        {
            float remaining = cooldown - (Time.time - lastUseTime);
            return Mathf.Max(0f, remaining);
        }
    }

    public bool CanExecute()
    {
        return Time.time >= lastUseTime + cooldown;
    }

    private void Update()
    {
        if (playerAim == null || !playerAim.IsAimingNow()) return;
        TryPerformAttack();
    }

    public void Execute()
    {
        TryPerformAttack();
    }

    private void TryPerformAttack()
    {
        if (!CanExecute()) return;
        if (playerAim == null) return;

        Vector2 attackDirection = playerAim.GetAttackDirection();
        if (attackDirection == Vector2.zero) return;

        lastUseTime = Time.time;
        lastAttackDirection = attackDirection;

        if (sfxSource != null)
            sfxSource.Play();

        if (animator != null)
        {
            if (playerMovement != null)
                playerMovement.SetAttackAnimationActive(true);

            if (spriteRenderer != null)
                spriteRenderer.flipX = false;

            animator.SetFloat("AttackX", attackDirection.x);
            animator.SetFloat("AttackY", attackDirection.y);
            animator.SetTrigger("Attack");
        }

        Vector2 attackCenter = (Vector2)transform.position + attackDirection * attackDistance;

        Vector2 boxSize = (attackDirection.y != 0)
            ? new Vector2(attackBoxSize.y, attackBoxSize.x)
            : attackBoxSize;

        LayerMask combinedMask = enemyLayer | trashLayer;
        Collider2D[] hits = Physics2D.OverlapBoxAll(attackCenter, boxSize, 0f, combinedMask);

        foreach (Collider2D hit in hits)
        {
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }
            else if (hit.TryGetComponent<BreakableTrash>(out BreakableTrash trash))
            {
                trash.TakeHit(damage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 attackCenter = (Vector2)transform.position + lastAttackDirection * attackDistance;
        Vector2 boxSize = (lastAttackDirection.y != 0)
            ? new Vector2(attackBoxSize.y, attackBoxSize.x)
            : attackBoxSize;
        Gizmos.DrawWireCube(attackCenter, boxSize);
    }

    public void IncreaseDamage(float amount)
    {
        damage += amount;
    }

    public void IncreaseDamagePercent(float percent)
    {
        damage *= 1f + percent / 100f;
    }

    public void IncreaseRange(float percent)
    {
        attackBoxSize *= 1f + percent / 100f;
    }
}
