using UnityEngine;

[CreateAssetMenu(menuName = "Upgrades/Speed Upgrade", order = 1)]
public class SpeedUpgradeSO : UpgradeSO
{
    [SerializeField] private float speedIncrease = 1.5f;

    public override void Apply(PlayerUpgradeManager manager)
    {
        if (manager.playerMovement != null)
        {
            manager.playerMovement.IncreaseMoveSpeed(speedIncrease);
        }

        Debug.Log($"Velocidad aumentada en +{speedIncrease}");
    }
}