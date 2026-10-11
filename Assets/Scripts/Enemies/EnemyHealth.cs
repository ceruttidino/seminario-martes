using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 10;
    [SerializeField] private DamageFlash damageFlash;

    private float currentHealth;
    private bool isDead;

    private bool canTakeDamage = true;

    public event System.Action OnDeath;
    public event System.Action OnDamaged;

    /// <summary>Bosses con fases: si devuelve true la muerte se cancela (el boss repone la vida cuando quiera).</summary>
    public System.Func<bool> DeathInterceptor;

    /// <summary>true: el daño no dispara sangre / flash / sonido (ej: el cuervo cuando esta escondido).</summary>
    public bool SuppressHitFeedback;

    public bool IsDead => isDead;
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool CanTakeDamage => canTakeDamage;

    private void Awake()
    {
        currentHealth = maxHealth;

        if (damageFlash == null)
            damageFlash = GetComponent<DamageFlash>();

        const int minEnemySort = 2;
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer != null && renderer.sortingOrder < minEnemySort)
                renderer.sortingOrder += minEnemySort;
        }

        if (GetComponent<BossBase>() == null)
        {
            if (GetComponent<EnemyBodyCollision>() == null)
                gameObject.AddComponent<EnemyBodyCollision>();
            if (GetComponent<EnemySeparation>() == null)
                gameObject.AddComponent<EnemySeparation>();
        }
        else if (GetComponent<EnemyBodyCollision>() == null)
        {
            gameObject.AddComponent<EnemyBodyCollision>();
        }
    }

    private void Start()
    {
        RoomInstance room = GetComponentInParent<RoomInstance>();
        room?.RegisterEnemy(this);
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        if (!canTakeDamage) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        OnDamaged?.Invoke();

        if (!SuppressHitFeedback)
        {
            PlayHurtSfx();

            EnemySummon.PlayBloodHit(transform.position);

            if (damageFlash != null)
                damageFlash.Flash();
        }

        if (currentHealth <= 0)
            Die();
    }

    /// <summary>Repone la vida con un nuevo maximo (fases de los bosses).</summary>
    public void Refill(float newMaxHealth)
    {
        maxHealth = Mathf.Max(1f, newMaxHealth);
        currentHealth = maxHealth;
    }

    private void Die()
    {
        if (isDead) return;

        if (DeathInterceptor != null && DeathInterceptor())
            return;

        isDead = true;
        if (!WillLeaveCorpse())
            BloodPool.Spawn(transform.position, GetComponentInParent<RoomInstance>()?.transform);
        OnDeath?.Invoke();
        Destroy(gameObject);
    }

    private bool WillLeaveCorpse()
    {
        RatRegeneration regeneration = GetComponent<RatRegeneration>();
        return regeneration != null && regeneration.enabled && regeneration.isRegenerating;
    }

    public void SetCurrentHealth(float value)
    {
        currentHealth = Mathf.Clamp(value, 0f, maxHealth);
    }

    public void SetDamageable(bool value)
    {
        canTakeDamage = value;
    }

    private void PlayHurtSfx()
    {
        if (GetComponent<BossBase>() != null)
        {
            AudioManager.PlayBossHurt();
            return;
        }

        EnemyBehaviour behaviour = GetComponent<EnemyBehaviour>();
        if (behaviour != null)
            AudioManager.PlayEnemyHurt(behaviour.Type);
    }
}
