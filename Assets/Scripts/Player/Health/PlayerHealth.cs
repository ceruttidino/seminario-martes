using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("regular Health")]
    [SerializeField] int playerHealthCap = 20;
    [SerializeField] int playerMaxHealth = 6;
    [SerializeField] int playerHealth = 6;

    [Header("Other")]
    [SerializeField] bool canGetHurt = true;
    [SerializeField] float invulTime = 1.5f;
    [SerializeField] private DamageFlash damageFlash;

    [Header("UI")]
    [SerializeField] GameObject[] hearts;
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Sprite halfHeartSprite;
    [SerializeField] private Sprite emptyHeartSprite;
    [SerializeField] private Color poisonedHeartColor = new Color(0.25f, 0.85f, 0.28f, 1f);

    [SerializeField] private AudioSource sfxSource;

    private Image[] heartImages;
    private PlayerPoisonStatus poisonStatus;
    private bool heartsPoisoned;
    private float invulnerableUntil;

    public event Action OnPlayerDeath;

    void Awake()
    {
        heartImages = new Image[hearts.Length];
        for (int i = 0; i < hearts.Length; i++)
            heartImages[i] = hearts[i].GetComponent<Image>();

        poisonStatus = GetComponent<PlayerPoisonStatus>();
        if (poisonStatus == null)
            poisonStatus = gameObject.AddComponent<PlayerPoisonStatus>();

        PlayerFeetMarker.EnsureOn(gameObject);
        if (GetComponent<PlayerWalkableClamp>() == null)
            gameObject.AddComponent<PlayerWalkableClamp>();
    }

    void Start()
    {
        UpdateMaxHearts();
        UpdateHearts(playerHealth);

        if (poisonStatus != null)
            poisonStatus.PoisonChanged += HandlePoisonChanged;
    }

    private void OnDestroy()
    {
        if (poisonStatus != null)
            poisonStatus.PoisonChanged -= HandlePoisonChanged;
    }

    public void TakeDamage(float damage)
    {
        if (!CanTakeHit || playerHealth <= 0) return;

        AudioManager.PlaySfx(GameSfx.PlayerHit, sfxSource != null ? sfxSource.clip : null);

        float iFrames = invulTime;
        if (damageFlash != null)
            iFrames = Mathf.Max(iFrames, damageFlash.TotalFlashDuration);

        canGetHurt = false;
        invulnerableUntil = Time.time + iFrames;
        playerHealth -= Mathf.RoundToInt(damage);
        playerHealth = Mathf.Clamp(playerHealth, 0, playerMaxHealth);
        UpdateHearts(playerHealth);

        if (damageFlash != null)
            damageFlash.Flash(iFrames);

        CameraShake.Play();

        CancelInvoke(nameof(DesInvul));
        if (playerHealth <= 0)
        {
            Die();
        }
        else
        {
            Invoke(nameof(DesInvul), iFrames);
        }
    }

    private bool CanTakeHit => canGetHurt && Time.time >= invulnerableUntil && (damageFlash == null || !damageFlash.IsFlashing);

    private void Die()
    {
        CancelInvoke(nameof(DesInvul));
        canGetHurt = false;
        invulnerableUntil = float.PositiveInfinity;
        DisablePlayerControls();
        OnPlayerDeath?.Invoke();
    }

    private void DisablePlayerControls()
    {
        if (TryGetComponent<PlayerMovement>(out var movement)) movement.enabled = false;
        if (TryGetComponent<PlayerAttack>(out var attack)) attack.enabled = false;
        if (TryGetComponent<PlayerDash>(out var dash)) dash.enabled = false;

        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    private void DesInvul()
    {
        canGetHurt = true;
        invulnerableUntil = 0f;
    }
    public void PlayerGetHurt()
    {
        TakeDamage(1);
    }

    public void PlayerGetHurt(int hurt)
    {
        TakeDamage(hurt);
    }

    public void PlayerAddHeart(bool isFull)
    {
        PlayerAddHeart(1, isFull);
    }

    public void PlayerAddHeart(int heartsToAdd, bool fillNewHearts = true)
    {
        playerMaxHealth += heartsToAdd * 2;
        playerMaxHealth = Mathf.Clamp(playerMaxHealth, 0, playerHealthCap);

        if (fillNewHearts)
            playerHealth = playerMaxHealth;

        UpdateMaxHearts();
        UpdateHearts(playerHealth);
        if (heartsToAdd > 0)
            poisonStatus?.Cure();
    }

    public void PlayerHeal()
    {
        PlayerHeal(1);
    }

    public void PlayerHeal(int heal)
    {
        poisonStatus?.Cure();
        if (playerHealth >= playerMaxHealth) return;

        playerHealth = Mathf.Clamp(playerHealth + heal, 0, playerMaxHealth);
        UpdateHearts(playerHealth);
    }

    private void UpdateMaxHearts()
    {
        for (int i = 0; i < hearts.Length; i++)
            hearts[i].SetActive(i < playerMaxHealth / 2);
    }

    private void UpdateHearts(int healthAmount)
    {
        int remaining = healthAmount;
        for (int i = 0; i < playerMaxHealth / 2; i++)
        {
            if (i >= heartImages.Length) break;

            if (remaining > 1)
            {
                heartImages[i].sprite = fullHeartSprite;
                remaining -= 2;
            }
            else if (remaining == 1)
            {
                heartImages[i].sprite = halfHeartSprite;
                remaining -= 1;
            }
            else
            {
                heartImages[i].sprite = emptyHeartSprite;
            }

            heartImages[i].color = heartsPoisoned ? poisonedHeartColor : Color.white;
        }
    }

    private void HandlePoisonChanged(bool poisoned)
    {
        heartsPoisoned = poisoned;
        UpdateHearts(playerHealth);
    }
    public int CurrentHealth => playerHealth;
    public int MaxHealth => playerMaxHealth;
    public bool IsDead => playerHealth <= 0;
    public bool IsHealthFull => playerHealth >= playerMaxHealth;

    public void TakePoisonTick()
    {
        if (playerHealth <= 1)
        {
            poisonStatus?.Cure();
            return;
        }

        playerHealth = Mathf.Max(1, playerHealth - 1);
        UpdateHearts(playerHealth);

        if (playerHealth <= 1)
            poisonStatus?.Cure();
    }
}
