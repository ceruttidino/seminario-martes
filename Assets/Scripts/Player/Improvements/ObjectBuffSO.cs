using UnityEngine;

[CreateAssetMenu(fileName = "NewObjectBuff", menuName = "Buffs/Object Buff")]
public class ObjectBuffSO : UpgradeSO
{
    [Header("HP")]
    [SerializeField] private int heartsToAdd = 0;

    [Header("Speed (%)")]
    [Tooltip("Porcentaje: 10 = +10%, -5 = -5%")]
    [SerializeField] private float speedPercentChange = 0f;

    [Header("Damage (%)")]
    [Tooltip("Porcentaje: 15 = +15%, -7 = -7%")]
    [SerializeField] private float damagePercentChange = 0f;

    [Header("Range (%)")]
    [Tooltip("Porcentaje: 10 = +10%, -7 = -7%")]
    [SerializeField] private float rangePercentChange = 0f;

    public override void Apply(PlayerUpgradeManager manager)
    {
        if (heartsToAdd != 0 && manager.playerHealth != null)
            manager.playerHealth.PlayerAddHeart(heartsToAdd, heartsToAdd > 0);

        if (speedPercentChange != 0f && manager.playerMovement != null)
            manager.playerMovement.IncreaseMoveSpeedPercent(speedPercentChange);

        if (damagePercentChange != 0f)
        {
            if (manager.quickAttack != null)
                manager.quickAttack.IncreaseDamagePercent(damagePercentChange);
            if (manager.areaAttack != null)
                manager.areaAttack.IncreaseDamagePercent(damagePercentChange);
        }

        if (rangePercentChange != 0f)
        {
            if (manager.quickAttack != null)
                manager.quickAttack.IncreaseRange(rangePercentChange);
            if (manager.areaAttack != null)
                manager.areaAttack.IncreaseRange(rangePercentChange);
        }
    }
}
