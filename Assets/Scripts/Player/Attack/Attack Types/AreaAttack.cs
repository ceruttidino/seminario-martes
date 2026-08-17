using UnityEngine;

public class AreaAttack : MonoBehaviour, IAttack
{
    [Header("References")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask trashLayer;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Area Attack")]
    [SerializeField] private float damage = 20f;
    [SerializeField] private float cooldown = 2f;
    [SerializeField] private float radius = 2f;

    [SerializeField] private AudioSource sfxSource;

    private float lastUseTime = -999f;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
    }

    public float CooldownRemaining
    {
        get
        {
            float remaining = cooldown - (Time.time - lastUseTime);
            return Mathf.Max(0f, remaining);
        }
    }

    public float CooldownNormalized
    {
        get
        {
            if (cooldown <= 0f) return 1f;
            return 1f - (CooldownRemaining / cooldown);
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

        if (animator != null)
        {
            if (playerMovement != null)
                playerMovement.SetAttackAnimationActive(true);

            animator.SetTrigger("AreaAttack");
        }

        LayerMask combinedMask = enemyLayer | trashLayer;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, combinedMask);

        foreach (Collider2D hit in hits)
        {
            // El shell no se "daña": se empuja alejándolo del centro de la explosión.
            TurtleShell shell = hit.GetComponentInParent<TurtleShell>();
            if (shell != null)
            {
                Vector2 pushDirection = (Vector2)hit.transform.position - (Vector2)transform.position;
                shell.Push(pushDirection);
                continue;
            }

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
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, radius);
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
        radius *= 1f + percent / 100f;
    }
}
