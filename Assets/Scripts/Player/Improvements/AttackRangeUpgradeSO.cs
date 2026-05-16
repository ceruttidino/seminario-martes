using UnityEngine;

[CreateAssetMenu(menuName = "Upgrades/Attack Range Upgrade", order = 4)]
public class AttackRangeUpgradeSO : UpgradeSO
{
    [SerializeField] private float[] rangeIncreases = { 10f, 7f, 5f };
    [SerializeField] private bool affectQuickAttack = true;
    [SerializeField] private bool affectAreaAttack = true;

    private int timesApplied = 0;

    public override void Apply(PlayerUpgradeManager manager)
    {
        int timesApplied = manager.GetTimesApplied(this);

        if (timesApplied > rangeIncreases.Length - 1)
        {
            Debug.Log("No more range bonuses available.");
            return;
        }

        float increase = rangeIncreases[timesApplied];

        if (affectQuickAttack && manager.quickAttack != null)
            manager.quickAttack.IncreaseRange(increase);

        if (affectAreaAttack && manager.areaAttack != null)
            manager.areaAttack.IncreaseRange(increase);

        Debug.Log($"Attack range increased by {increase}%");
    }

}
