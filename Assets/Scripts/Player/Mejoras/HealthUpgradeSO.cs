using UnityEngine;

[CreateAssetMenu(menuName = "Upgrades/Health Upgrade", order = 3)]
public class HealthUpgradeSO : UpgradeSO
{
    [SerializeField] private int heartsToAdd = 1;
    [SerializeField] private bool fillNewHearts = true;

    public override void Apply(PlayerUpgradeManager manager)
    {
        if (manager.playerHealth != null)
        {
            manager.playerHealth.PlayerAddHeart(heartsToAdd, fillNewHearts);
            Debug.Log($"Vida aumentada en +{heartsToAdd} corazones");
        }
    }
}