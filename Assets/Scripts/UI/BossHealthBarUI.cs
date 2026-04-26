using UnityEngine;
using UnityEngine.UI;

public class BossHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyHealth bossHealth;
    [SerializeField] private Slider healthSlider;

    private void Awake()
    {
        if (healthSlider == null)
            healthSlider = GetComponentInChildren<Slider>();

        if (bossHealth == null)
            bossHealth = FindFirstObjectByType<MutantSpiderBoss>()?.GetComponent<EnemyHealth>();
    }

    private void OnEnable()
    {
        if (bossHealth != null)
            bossHealth.OnDeath += Hide;
    }

    private void OnDisable()
    {
        if (bossHealth != null)
            bossHealth.OnDeath -= Hide;
    }

    private void Update()
    {
        if (bossHealth == null)
        {
            gameObject.SetActive(false);
            return;
        }

        healthSlider.value = bossHealth.CurrentHealth / bossHealth.MaxHealth;
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
