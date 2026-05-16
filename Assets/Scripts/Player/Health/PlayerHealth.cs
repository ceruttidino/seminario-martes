using System;
using UnityEngine;
using UnityEngine.InputSystem;
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

    private Image[] heartImages;

    public event Action OnPlayerDeath;

    void Awake()
    {
        heartImages = new Image[hearts.Length];
        for (int i = 0; i < hearts.Length; i++)
            heartImages[i] = hearts[i].GetComponent<Image>();
    }

    void Start()
    {
        UpdateMaxHearts();
        UpdateHearts(playerHealth);
    }

    public void TakeDamage(float damage)
    {
        if (!canGetHurt || playerHealth <= 0) return;

        canGetHurt = false;
        playerHealth -= Mathf.RoundToInt(damage);
        playerHealth = Mathf.Clamp(playerHealth, 0, playerMaxHealth);
        UpdateHearts(playerHealth);

        if (damageFlash != null)
            damageFlash.Flash();

        if (playerHealth <= 0)
        {
            Die();
        }
        else
        {
            Invoke(nameof(DesInvul), invulTime);
        }
    }

    private void Die()
    {
        CancelInvoke(nameof(DesInvul));
        canGetHurt = false;
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
    }

    // teclas de debug para testear en el editor
    public void Heal(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        PlayerHeal();
    }

    public void Hurt(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        PlayerGetHurt();
    }

    public void AddHeart(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        PlayerAddHeart(false);
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
    }

    public void PlayerHeal()
    {
        PlayerHeal(1);
    }

    public void PlayerHeal(int heal)
    {
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
                heartImages[i].color = Color.white;
                remaining -= 2;
            }
            else if (remaining == 1)
            {
                heartImages[i].color = Color.gray;
                remaining -= 1;
            }
            else
            {
                heartImages[i].color = Color.black;
            }
        }
    }

    public int CurrentHealth => playerHealth;
    public bool IsDead => playerHealth <= 0;
}
