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

    [Header("UI")]
    [SerializeField] GameObject[] hearts;

    // Evento para Game Over
    public event Action OnPlayerDeath;

    void Start()
    {
        UpdateMaxHearts();
        UpdateHearts(playerHealth);
    }

    void Update()
    {
        if (playerHealth > playerMaxHealth)
            playerHealth = playerMaxHealth;
    }

    // ====================== DAÑO ======================
    public void TakeDamage(float damage)
    {
        if (!canGetHurt || playerHealth <= 0) return;

        canGetHurt = false;
        playerHealth -= Mathf.RoundToInt(damage);
        UpdateHearts(playerHealth);

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
        Debug.Log("¡EL MAPACHE HA MUERTO!");
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
            rb.isKinematic = true;
        }
    }

    private void DesInvul()
    {
        canGetHurt = true;
    }

    // ====================== CHEATS ======================
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

    // ====================== CURACIÓN Y UPGRADES ======================
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
        if (playerMaxHealth > playerHealthCap) playerMaxHealth = playerHealthCap;

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
        if (playerHealth < playerMaxHealth)
        {
            playerHealth += heal;
            if (playerHealth > playerMaxHealth) playerHealth = playerMaxHealth;
            UpdateHearts(playerHealth);
        }
    }

    // ====================== UI ======================
    private void UpdateMaxHearts()
    {
        for (int i = 0; i < playerMaxHealth / 2; i++)
        {
            if (i < hearts.Length)
                hearts[i].SetActive(true);
        }
    }

    private void UpdateHearts(int healthAmmount)
    {
        int remaining = healthAmmount;
        for (int i = 0; i < playerMaxHealth / 2; i++)
        {
            if (i >= hearts.Length) break;

            Image heartImage = hearts[i].GetComponent<Image>();

            if (remaining > 1)
            {
                heartImage.color = Color.green;
                remaining -= 2;
            }
            else if (remaining == 1)
            {
                heartImage.color = Color.red;
                remaining -= 1;
            }
            else
            {
                heartImage.color = Color.black;
            }
        }
    }

    // Propiedades útiles
    public int CurrentHealth => playerHealth;
    public bool IsDead => playerHealth <= 0;
}