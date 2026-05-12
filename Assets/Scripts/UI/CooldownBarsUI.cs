using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CooldownBarsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerDash playerDash;
    [SerializeField] private AreaAttack areaAttack;

    [Header("Dash UI")]
    [SerializeField] private Image dashFill;

    [Header("Area Attack UI")]
    [SerializeField] private Image areaAttackFill;

    private void Awake()
    {
        if (playerDash == null)
            playerDash = FindFirstObjectByType<PlayerDash>();

        if (areaAttack == null)
            areaAttack = FindFirstObjectByType<AreaAttack>();
    }

    private void Update()
    {
        UpdateDashBar();
        UpdateAreaAttackBar();
    }

    private void UpdateDashBar()
    {
        if (playerDash == null || dashFill == null) return;

        dashFill.fillAmount = playerDash.CooldownNormalized;
    }

    private void UpdateAreaAttackBar()
    {
        if (areaAttack == null || areaAttackFill == null) return;

        areaAttackFill.fillAmount = areaAttack.CooldownNormalized;

    }
}
