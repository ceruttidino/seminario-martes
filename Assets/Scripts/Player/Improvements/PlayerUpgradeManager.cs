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

    private Dictionary<UpgradeSO, int> upgradeCounts = new Dictionary<UpgradeSO, int>();

    private void Awake()
    {
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        if (quickAttack == null) quickAttack = GetComponent<QuickAttack>();
        if (areaAttack == null) areaAttack = GetComponent<AreaAttack>();
    }

    public int GetTimesApplied(UpgradeSO upgrade)
    {
        return collectedUpgrades.FindAll(u => u == upgrade).Count;
    }

    public void CollectUpgrade(UpgradeSO upgrade)
    {
        if (upgrade == null) return;

        collectedUpgrades.Add(upgrade);
        upgrade.Apply(this);
    }

    public List<UpgradeSO> GetCollectedUpgrades() => collectedUpgrades;
}
