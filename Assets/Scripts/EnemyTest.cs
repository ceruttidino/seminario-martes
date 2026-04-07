using UnityEngine;

public class EnemyTest : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 30f;

    private float currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"Enemigo recibio {damage} de daño. Vida Restante: {currentHealth}");

        if (currentHealth <= 0) 
        {
            Destroy(gameObject);
        }
    }
}
