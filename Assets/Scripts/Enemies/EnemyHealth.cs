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

    public bool IsDead => isDead;
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool CanTakeDamage => canTakeDamage;

    private void Awake()
    {
        currentHealth = maxHealth;

        if (damageFlash == null)
            damageFlash = GetComponent<DamageFlash>();

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
        PlayHurtSfx();

        EnemySummon.PlayBloodHit(transform.position);

        if (damageFlash != null)
            damageFlash.Flash();

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (isDead) return;

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
