using UnityEngine;

[CreateAssetMenu(menuName = "Upgrades/Attack Range Upgrade", order = 4)]
public class AttackRangeUpgradeSO : UpgradeSO
{
    [SerializeField] private float[] rangeIncreases = { 10f, 7f, 5f };
    [SerializeField] private bool affectQuickAttack = true;
    [SerializeField] private bool affectAreaAttack = true;

    public override void Apply(PlayerUpgradeManager manager)
    {
        // GetTimesApplied ya cuenta este pickup porque CollectUpgrade lo agrega antes de llamar Apply
        int timesApplied = manager.GetTimesApplied(this);

        if (timesApplied < 1 || timesApplied > rangeIncreases.Length)
            return;

        float increase = rangeIncreases[timesApplied - 1];

        if (affectQuickAttack && manager.quickAttack != null)
            manager.quickAttack.IncreaseRange(increase);

        if (affectAreaAttack && manager.areaAttack != null)
            manager.areaAttack.IncreaseRange(increase);
    }
}
