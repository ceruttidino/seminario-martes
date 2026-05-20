using UnityEngine;
using UnityEngine.InputSystem;

public class QuickAttack : MonoBehaviour, IAttack
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerDirection;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask trashLayer;
    [SerializeField] private Animator animator;

    [Header("Quick Attack")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float cooldown = 0.5f;
    [SerializeField] private float attackDistance = 1f;
    [SerializeField] private Vector2 attackBoxSize = new Vector2(1.2f, 0.8f);

    [SerializeField] private AudioSource sfxSource;

    private float lastUseTime = -999f;
    private SpriteRenderer spriteRenderer;
    private Vector2 lastAttackDirection = Vector2.down;

    private void Awake()
    {
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

    public void Execute()
    {
        if (!CanExecute()) return;

        lastUseTime = Time.time;

        if (sfxSource != null)
            sfxSource.Play();

        if (Camera.main == null || Mouse.current == null) return;

        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 directionToMouse = (mouseWorldPos - (Vector2)transform.position).normalized;

        // direccion cardinal hacia el mouse — siempre correcta, nunca invertida
        Vector2 attackDirection = GetCardinalDirection(directionToMouse);
        lastAttackDirection = attackDirection;

        if (animator != null)
        {
            // la animacion invierte X si el sprite esta flippeado para que se vea correcto visualmente,
            // pero esto NO afecta al hitbox
            Vector2 animDirection = attackDirection;
            if (spriteRenderer != null && spriteRenderer.flipX)
                animDirection.x *= -1;

            animator.SetFloat("AttackX", animDirection.x);
            animator.SetFloat("AttackY", animDirection.y);
            animator.SetTrigger("Attack");
        }

        // el hitbox siempre va hacia donde apunta el mouse, independiente del flip visual
        Vector2 attackCenter = (Vector2)transform.position + attackDirection * attackDistance;

        // si el ataque es vertical, rotar las dimensiones del box (mas alto que ancho)
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

    private Vector2 GetCardinalDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x > 0 ? Vector2.right : Vector2.left;
        else
            return direction.y > 0 ? Vector2.up : Vector2.down;
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
