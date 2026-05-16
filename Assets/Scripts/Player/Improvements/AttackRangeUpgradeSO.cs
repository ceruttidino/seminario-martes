using UnityEngine;

[CreateAssetMenu(menuName = "Upgrades/Attack Range Upgrade", order = 4)]
public class AttackRangeUpgradeSO : UpgradeSO
{
    [SerializeField] private float[] rangeIncreases = { 10f, 7f, 5f };
    [SerializeField] private bool affectQuickAttack = true;
    [SerializeField] private bool affectAreaAttack = true;

    public override void Apply(PlayerUpgradeManager manager)
    {
        int timesApplied = manager.GetTimesApplied(this);

        if (timesApplied > rangeIncreases.Length - 1)
            return;

        float increase = rangeIncreases[timesApplied];

        if (affectQuickAttack && manager.quickAttack != null)
            manager.quickAttack.IncreaseRange(increase);

        if (affectAreaAttack && manager.areaAttack != null)
            manager.areaAttack.IncreaseRange(increase);
    }
}
