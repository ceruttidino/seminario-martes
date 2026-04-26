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
    [SerializeField] private TMP_Text dashText;

    [Header("Area Attack UI")]
    [SerializeField] private Image areaAttackFill;
    [SerializeField] private TMP_Text areaAttackText;

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

        if (dashText != null)
            dashText.text = playerDash.CanDash ? "DASH" : playerDash.CooldownRemaining.ToString("0.0");
    }

    private void UpdateAreaAttackBar()
    {
        if (areaAttack == null || areaAttackFill == null) return;

        areaAttackFill.fillAmount = areaAttack.CooldownNormalized;

        if (areaAttackText != null)
            areaAttackText.text = areaAttack.CanExecute() ? "HEAVY" : areaAttack.CooldownRemaining.ToString("0.0");
    }
}
