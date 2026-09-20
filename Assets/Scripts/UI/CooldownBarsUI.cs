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

        ConfigureFill(dashFill);
        ConfigureFill(areaAttackFill);
    }

    private void Update()
    {
        UpdateDashBar();
        UpdateAreaAttackBar();
    }

    private static void ConfigureFill(Image fill)
    {
        if (fill == null) return;

        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Vertical;
        fill.fillOrigin = (int)Image.OriginVertical.Top;

        Transform parent = fill.transform.parent;
        if (parent == null) return;

        Image well = parent.GetComponent<Image>();
        if (well != null && well != fill)
            well.enabled = false;

        Transform iconTf = parent.Find("Icon");
        if (iconTf == null) return;

        Image icon = iconTf.GetComponent<Image>();
        if (icon == null) return;

        icon.type = Image.Type.Simple;
        icon.preserveAspect = true;
        icon.enabled = true;
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
