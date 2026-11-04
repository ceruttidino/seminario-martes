using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour, IDamageable, ISourcedDamageable
{
    [Header("regular Health")]
    [SerializeField] int playerHealthCap = 20;
    [SerializeField] int playerMaxHealth = 6;
    [SerializeField] int playerHealth = 6;

    [Header("Other")]
    [SerializeField] bool canGetHurt = true;
    [SerializeField] float invulTime = 1.5f;
    [SerializeField] private DamageFlash damageFlash;

    [SerializeField] private PlayerHurtFeedback hurtFeedback;

    [Header("UI")]
    [SerializeField] GameObject[] hearts;
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Sprite halfHeartSprite;
    [SerializeField] private Sprite emptyHeartSprite;
    [SerializeField] private Color poisonedHeartColor = new Color(0.25f, 0.85f, 0.28f, 1f);

    [Header("UI - corazones envenenados (animados)")]
    [Tooltip("Cuadros del corazon lleno envenenado (hoja CorazonEnvenenado). Si falta alguna de las dos listas, los corazones se tiñen de verde como antes.")]
    [SerializeField] private Sprite[] poisonedFullFrames;
    [Tooltip("Cuadros del medio corazon envenenado (hoja CorazonEnvenenadoMedio).")]
    [SerializeField] private Sprite[] poisonedHalfFrames;
    [SerializeField] private float poisonedHeartFps = 10f;

    [SerializeField] private AudioSource sfxSource;

    private enum HeartKind : byte { Empty, Half, Full }

    private Image[] heartImages;
    private HeartKind[] heartKinds;
    private PlayerPoisonStatus poisonStatus;
    private bool heartsPoisoned;
    private float poisonAnimStart;
    private int lastPoisonFrame = -1;
    private float invulnerableUntil;

    public event Action OnPlayerDeath;

    /// <summary>Último atacante que REALMENTE aplicó daño (los golpes durante i-frames no cuentan). Null si fue sin source.</summary>
    public DamageSourceInfo LastDamageSource { get; private set; }

    void Awake()
    {
        heartImages = new Image[hearts.Length];
        heartKinds = new HeartKind[hearts.Length];
        for (int i = 0; i < hearts.Length; i++)
            heartImages[i] = hearts[i].GetComponent<Image>();

        poisonStatus = GetComponent<PlayerPoisonStatus>();
        if (poisonStatus == null)
            poisonStatus = gameObject.AddComponent<PlayerPoisonStatus>();

        PlayerFeetMarker.EnsureOn(gameObject);
        if (GetComponent<PlayerWalkableClamp>() == null)
            gameObject.AddComponent<PlayerWalkableClamp>();

        if (hurtFeedback == null) // NUEVO: lo busca solo si te olvidaste de asignarlo
            hurtFeedback = GetComponent<PlayerHurtFeedback>();
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
        // Sin source explícito: se usa el de DamageContext si alguien lo pusheó (BossPlayerUtils, etc.).
        TakeDamage(damage, DamageContext.Current);
    }

    public void TakeDamage(float damage, GameObject source)
    {
        if (!CanTakeHit || playerHealth <= 0) return;

        // Se registra DESPUÉS del guard: un golpe bloqueado por invulnerabilidad no te "mata".
        // Un golpe sin source deja null a propósito: si no sabemos quién fue, no mostramos al anterior.
        LastDamageSource = DamageSourceInfo.From(source);

        AudioManager.PlaySfx(GameSfx.PlayerHit, sfxSource != null ? sfxSource.clip : null);

        float iFrames = invulTime;
        if (damageFlash != null)
            iFrames = Mathf.Max(iFrames, damageFlash.TotalFlashDuration);

        canGetHurt = false;
        invulnerableUntil = Time.time + iFrames;
        playerHealth -= Mathf.RoundToInt(damage);
        playerHealth = Mathf.Clamp(playerHealth, 0, playerMaxHealth);
        UpdateHearts(playerHealth);
        GameMetrics.PlayerDamaged(damage, LastDamageSource != null ? LastDamageSource.Name : "unknown", playerHealth, playerMaxHealth);

        if (damageFlash != null)
            damageFlash.Flash(iFrames);

        // CAMBIADO: antes solo era CameraShake.Play(). Ahora, si está el PlayerHurtFeedback,
        // hace hit stop + shake + knockback + parpadeo (y no se sacude la cámara dos veces).
        // Se llama ANTES de Die() para que el feedback de muerte salga antes de desactivar los controles.
        bool lethal = playerHealth <= 0;
        if (hurtFeedback != null)
            hurtFeedback.Play(iFrames, lethal);
        else
            CameraShake.Play();

        CancelInvoke(nameof(DesInvul));
        if (lethal) // CAMBIADO: usa la variable de arriba (mismo resultado que antes)
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
        string source = LastDamageSource != null ? LastDamageSource.Name : "unknown";
        string enemyType = LastDamageSource != null && LastDamageSource.HasEnemyType ? LastDamageSource.EnemyType.ToString() : "";
        GameMetrics.PlayerDied(source, enemyType, playerMaxHealth);
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

    // Con las hojas del corazon envenenado asignadas, los corazones llenos / medios usan esas animaciones (el vacio no
    // cambia). Sin ellas se tiñen de verde, como antes.
    private bool HasPoisonedFrames =>
        poisonedFullFrames != null && poisonedFullFrames.Length > 0 &&
        poisonedHalfFrames != null && poisonedHalfFrames.Length > 0;

    private bool AnimatePoisonedHearts => heartsPoisoned && HasPoisonedFrames;

    private void UpdateHearts(int healthAmount)
    {
        bool animated = AnimatePoisonedHearts;
        int frame = animated ? CurrentPoisonFrame() : 0;

        int remaining = healthAmount;
        for (int i = 0; i < playerMaxHealth / 2; i++)
        {
            if (i >= heartImages.Length) break;

            if (remaining > 1)
            {
                heartKinds[i] = HeartKind.Full;
                heartImages[i].sprite = animated ? PoisonedFrame(poisonedFullFrames, frame, fullHeartSprite) : fullHeartSprite;
                remaining -= 2;
            }
            else if (remaining == 1)
            {
                heartKinds[i] = HeartKind.Half;
                heartImages[i].sprite = animated ? PoisonedFrame(poisonedHalfFrames, frame, halfHeartSprite) : halfHeartSprite;
                remaining -= 1;
            }
            else
            {
                heartKinds[i] = HeartKind.Empty;
                heartImages[i].sprite = emptyHeartSprite;
            }

            // Animados: el sprite ya trae el color. Sin animacion: tinte verde de antes.
            heartImages[i].color = heartsPoisoned && !animated ? poisonedHeartColor : Color.white;
        }

        lastPoisonFrame = frame;
    }

    private void Update()
    {
        if (!AnimatePoisonedHearts || heartImages == null)
            return;

        int frame = CurrentPoisonFrame();
        if (frame == lastPoisonFrame)
            return;

        lastPoisonFrame = frame;
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null || hearts[i] == null || !hearts[i].activeSelf)
                continue;

            if (heartKinds[i] == HeartKind.Full)
                heartImages[i].sprite = PoisonedFrame(poisonedFullFrames, frame, fullHeartSprite);
            else if (heartKinds[i] == HeartKind.Half)
                heartImages[i].sprite = PoisonedFrame(poisonedHalfFrames, frame, halfHeartSprite);
        }
    }

    // Tiempo sin escala: la animacion sigue corriendo con el juego en pausa o con hit stop.
    private int CurrentPoisonFrame()
    {
        return Mathf.Max(0, Mathf.FloorToInt((Time.unscaledTime - poisonAnimStart) * Mathf.Max(1f, poisonedHeartFps)));
    }

    private static Sprite PoisonedFrame(Sprite[] frames, int frame, Sprite fallback)
    {
        Sprite sprite = frames[frame % frames.Length];
        return sprite != null ? sprite : fallback;
    }

    private void HandlePoisonChanged(bool poisoned)
    {
        heartsPoisoned = poisoned;
        poisonAnimStart = Time.unscaledTime;
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
        GameMetrics.PlayerDamaged(1, "poison_tick", playerHealth, playerMaxHealth);

        if (playerHealth <= 1)
            poisonStatus?.Cure();
    }
}