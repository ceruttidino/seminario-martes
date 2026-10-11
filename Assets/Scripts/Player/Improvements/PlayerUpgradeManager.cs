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
        UpgradeSO real = UpgradeRegistry.Resolve(upgrade);
        return collectedUpgrades.FindAll(u => u == real).Count;
    }

    /// <summary>True si el player ya tiene esta mejora (o su version duplicada).</summary>
    public bool HasUpgrade(UpgradeSO upgrade)
    {
        UpgradeSO real = UpgradeRegistry.Resolve(upgrade);
        return real != null && collectedUpgrades.Contains(real);
    }

    /// <summary>
    /// Aplica la mejora. Cada mejora se puede tener UNA sola vez: si ya la tiene devuelve false y no hace nada.
    /// </summary>
    public bool CollectUpgrade(UpgradeSO upgrade)
    {
        UpgradeSO real = UpgradeRegistry.Resolve(upgrade);
        if (real == null) return false;
        if (HasUpgrade(real)) return false;

        collectedUpgrades.Add(real);
        UpgradeRegistry.MarkOwned(real);
        real.Apply(this);

        // Animacion de agarre: el mapache se queda quieto mostrando el objeto que acaba de conseguir.
        PlayerBuffPose pose = GetComponent<PlayerBuffPose>();
        if (pose == null)
            pose = gameObject.AddComponent<PlayerBuffPose>();
        pose.Play(real.icon, real.aColor);

        if (UpgradePopupUI.Instance != null)
            UpgradePopupUI.Instance.Show(real);
        else if (TabMenuUI.Instance != null)
            TabMenuUI.Instance.ShowPickup(real.icon, real.upgradeName, 1);

        return true;
    }

    public List<UpgradeSO> GetCollectedUpgrades() => collectedUpgrades;
}
