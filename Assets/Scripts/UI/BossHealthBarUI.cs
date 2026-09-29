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
            bossHealth = FindBossHealth();
    }

    // NUEVO: sirve para cualquier boss que herede de BossBase
    private EnemyHealth FindBossHealth()
    {
        // 1) La barra es hija del boss
        BossBase boss = GetComponentInParent<BossBase>();

        // 2) La barra está en la misma room que el boss
        if (boss == null)
        {
            RoomInstance room = GetComponentInParent<RoomInstance>();
            if (room != null)
                boss = room.GetComponentInChildren<BossBase>(true);
        }

        // 3) Último recurso: cualquier boss de la escena
        if (boss == null)
            boss = FindFirstObjectByType<BossBase>();

        return boss != null ? boss.GetComponent<EnemyHealth>() : null;
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

        healthSlider.value = (float)bossHealth.CurrentHealth / bossHealth.MaxHealth;
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
