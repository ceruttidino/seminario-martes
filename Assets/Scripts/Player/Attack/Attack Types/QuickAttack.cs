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

    private float lastUseTime = -999f;

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

        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 directionToMouse = (mouseWorldPos - (Vector2)transform.position).normalized;

        Vector2 lookDirection = GetCardinalDirection(directionToMouse);

        if (GetComponent<SpriteRenderer>().flipX)
            lookDirection *= -1;

        animator.SetFloat("AttackX", lookDirection.x);
        animator.SetFloat("AttackY", lookDirection.y);
        animator.SetTrigger("Attack");

        Vector2 attackCenter = (Vector2)transform.position + lookDirection * attackDistance;

        LayerMask combinedMask = enemyLayer | trashLayer;

        Collider2D[] hits = Physics2D.OverlapBoxAll(attackCenter, attackBoxSize, 0f, combinedMask);

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
        if (playerDirection == null) return;
        Gizmos.color = Color.red;
        Vector2 lookDirection = playerDirection.LastLookDirection;
        Vector2 attackCenter = (Vector2)transform.position + lookDirection * attackDistance;
        Gizmos.DrawWireCube(attackCenter, attackBoxSize);
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
