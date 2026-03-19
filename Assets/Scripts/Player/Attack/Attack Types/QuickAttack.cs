using Unity.VisualScripting;
using UnityEngine;

public class QuickAttack : MonoBehaviour, IAttack
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerDirection;
    [SerializeField] private LayerMask enemyLayer;

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
            float remaining = cooldown - (Time.deltaTime - lastUseTime);
            return Mathf.Max(0f, remaining);
        }
    }

    public bool CanExecute()
    {
        return Time.time >= lastUseTime + cooldown;
    }

    public void Execute()
    {
        if (!CanExecute())
        {
            return;
        }

        lastUseTime = Time.time;

        Vector2 lookDirection = playerDirection.LastLookDirection;
        Vector2 attackCenter = (Vector2)transform.position + lookDirection * attackDistance;

        Collider2D[] hits = Physics2D.OverlapBoxAll(attackCenter, attackBoxSize,0f,enemyLayer);

        foreach (Collider2D hit in hits) 
        {
            if(hit.TryGetComponent<IDamageable>(out IDamageable damageable))
            {
                damageable.TakeDamage(damage);
            }
        }
    }

    //visualize on "scene view" area of attack
    private void OnDrawGizmosSelected()
    {
        if(playerDirection == null)
        {
            return;
        }

        Gizmos.color = Color.red;

        Vector2 lookDirection = playerDirection.LastLookDirection;
        Vector2 attackCenter = (Vector2)transform.position + lookDirection * attackDistance;

        Gizmos.DrawWireCube(attackCenter, attackBoxSize);
    }
}
