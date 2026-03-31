using System.Collections.Generic;
using UnityEngine;

public class PlayerUpgradeManager : MonoBehaviour
{
    [Header("Referencias a tus componentes existentes")]
    [SerializeField] public PlayerMovement playerMovement;
    [SerializeField] public PlayerHealth playerHealth;
    [SerializeField] public QuickAttack quickAttack;
    [SerializeField] public AreaAttack areaAttack;

    [Header("Upgrades recolectados")]
    [SerializeField] private List<UpgradeSO> collectedUpgrades = new List<UpgradeSO>();

    private void Awake()
    {
        // Auto-asignación si no están asignados en el inspector
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        if (quickAttack == null) quickAttack = GetComponent<QuickAttack>();
        if (areaAttack == null) areaAttack = GetComponent<AreaAttack>();
    }

    /// <summary>
    /// Se llama cuando el jugador recoge una mejora
    /// </summary>
    public void CollectUpgrade(UpgradeSO upgrade)
    {
        if (upgrade == null) return;

        collectedUpgrades.Add(upgrade);
        upgrade.Apply(this);

        Debug.Log($"¡Mejora recolectada! → {upgrade.upgradeName}");
    }

    public List<UpgradeSO> GetCollectedUpgrades() => collectedUpgrades;
}