using UnityEngine;

[CreateAssetMenu(menuName = "Upgrades/Damage Upgrade", order = 2)]
public class DamageUpgradeSO : UpgradeSO
{
    [SerializeField] private float damageIncrease = 5f;
    [SerializeField] private bool affectQuickAttack = true;
    [SerializeField] private bool affectAreaAttack = true;

    public override void Apply(PlayerUpgradeManager manager)
    {
        if (affectQuickAttack && manager.quickAttack != null)
            manager.quickAttack.IncreaseDamage(damageIncrease);

        if (affectAreaAttack && manager.areaAttack != null)
            manager.areaAttack.IncreaseDamage(damageIncrease);

        Debug.Log($"Daño aumentado en +{damageIncrease}");
    }
}