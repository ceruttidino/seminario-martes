using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Grilla de buffs del menu TAB: una celda por buff recogido (en el orden en que se juntaron) y, al pasar el mouse por
/// una celda, un cartel chico con el nombre y la descripcion del buff. Siempre hay como minimo 'minVisibleSlots' celdas
/// (las que no tienen buff se ven apagadas) y se agregan filas de a 'columns' celdas cuando hacen falta.
/// </summary>
public sealed class BuffGridUI : MonoBehaviour
{
    [Header("Celdas")]
    [SerializeField] private BuffSlotUI[] slots;
    [SerializeField] private int columns = 4;
    [Tooltip("Celdas que se muestran siempre (aunque no haya buffs): 2 filas de 4.")]
    [SerializeField] private int minVisibleSlots = 8;

    [Header("Cartel de descripcion")]
    [SerializeField] private RectTransform tooltip;
    [SerializeField] private TMP_Text tooltipTitle;
    [SerializeField] private TMP_Text tooltipBody;
    [SerializeField] private Vector2 tooltipPadding = new Vector2(16f, 12f);
    [SerializeField] private float tooltipLineGap = 4f;
    [Tooltip("Separacion entre la celda y el cartel.")]
    [SerializeField] private float tooltipGap = 14f;

    private BuffSlotUI hoveredSlot;

    private static readonly Dictionary<UpgradeSO, int> countBuffer = new Dictionary<UpgradeSO, int>();
    private static readonly List<UpgradeSO> orderBuffer = new List<UpgradeSO>();

    private void Awake()
    {
        if (slots != null)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                    slots[i].Bind(this);
            }
        }

        // Estado inicial: las celdas minimas, vacias (TabMenuUI la rellena con los buffs al abrir).
        Refresh(null);
    }

    /// <summary>Vuelve a armar la grilla con los buffs recogidos (la lista trae repetidos si se juntaron varias veces).</summary>
    public void Refresh(IReadOnlyList<UpgradeSO> collected)
    {
        countBuffer.Clear();
        orderBuffer.Clear();

        if (collected != null)
        {
            for (int i = 0; i < collected.Count; i++)
            {
                UpgradeSO upgrade = collected[i];
                if (upgrade == null)
                    continue;

                if (countBuffer.ContainsKey(upgrade))
                {
                    countBuffer[upgrade]++;
                }
                else
                {
                    countBuffer[upgrade] = 1;
                    orderBuffer.Add(upgrade);
                }
            }
        }

        if (slots == null)
            return;

        int cols = Mathf.Max(1, columns);
        int rowsNeeded = Mathf.CeilToInt(orderBuffer.Count / (float)cols);
        int visible = Mathf.Min(slots.Length, Mathf.Max(minVisibleSlots, rowsNeeded * cols));

        for (int i = 0; i < slots.Length; i++)
        {
            BuffSlotUI slot = slots[i];
            if (slot == null)
                continue;

            if (i < orderBuffer.Count && i < slots.Length)
                slot.SetItem(orderBuffer[i], countBuffer[orderBuffer[i]]);
            else if (i < visible)
                slot.SetEmpty();
            else
                slot.Hide();
        }

        HideTooltipNow();
    }

    /// <summary>Apaga el hover y el cartel (al cerrar el menu).</summary>
    public void ResetHover()
    {
        if (slots != null)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                    slots[i].ResetHover();
            }
        }

        HideTooltipNow();
    }

    public void ShowTooltip(BuffSlotUI slot)
    {
        if (slot == null || !slot.HasItem || tooltip == null || tooltipTitle == null || tooltipBody == null)
            return;

        hoveredSlot = slot;

        UpgradeSO upgrade = slot.Upgrade;
        string title = TabMenuUI.EnglishItemName(upgrade.upgradeName);
        if (slot.Amount > 1)
            title += $"  x{slot.Amount}";

        tooltipTitle.text = title;
        tooltipBody.text = ColorizeDescription(upgrade.description);

        LayoutTooltip();
        PlaceTooltip(slot);
        tooltip.gameObject.SetActive(true);
    }

    public void HideTooltip(BuffSlotUI slot)
    {
        // Solo se apaga si el cartel es de esa celda (al pasar de una celda a otra llega primero el Enter de la nueva).
        if (slot != null && hoveredSlot != null && hoveredSlot != slot)
            return;

        HideTooltipNow();
    }

    private void HideTooltipNow()
    {
        hoveredSlot = null;
        if (tooltip != null)
            tooltip.gameObject.SetActive(false);
    }

    // "Damage +" / "Damage + Range -": el + en verde y el - en rojo para que se lea de un vistazo.
    private static string ColorizeDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return string.Empty;

        string text = description.Trim().Trim('\'');
        text = text.Replace("+", "<color=#7CFF8A>+</color>");
        text = text.Replace(" -", " <color=#FF7A7A>-</color>");
        return text;
    }

    private void LayoutTooltip()
    {
        Vector2 titleSize = tooltipTitle.GetPreferredValues(tooltipTitle.text);
        Vector2 bodySize = tooltipBody.GetPreferredValues(tooltipBody.text);

        float width = Mathf.Max(titleSize.x, bodySize.x) + tooltipPadding.x * 2f;
        float height = titleSize.y + tooltipLineGap + bodySize.y + tooltipPadding.y * 2f;
        tooltip.sizeDelta = new Vector2(width, height);

        SetTopLeft(tooltipTitle.rectTransform, tooltipPadding.x, -tooltipPadding.y, width - tooltipPadding.x * 2f, titleSize.y);
        SetTopLeft(tooltipBody.rectTransform, tooltipPadding.x, -(tooltipPadding.y + titleSize.y + tooltipLineGap), width - tooltipPadding.x * 2f, bodySize.y);
    }

    private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    // El cartel aparece a la izquierda de la celda (la grilla esta pegada al borde derecho de la pantalla), con su borde de
    // arriba alineado al de la celda: asi el de la primera fila no tapa el boton TAB.
    private void PlaceTooltip(BuffSlotUI slot)
    {
        RectTransform slotRect = slot.transform as RectTransform;
        if (slotRect == null || tooltip.parent != slotRect.parent)
            return;

        Vector2 slotSize = slotRect.sizeDelta;
        Vector2 pivot = slotRect.pivot;
        Vector2 center = slotRect.anchoredPosition + new Vector2(slotSize.x * (0.5f - pivot.x), slotSize.y * (0.5f - pivot.y));
        float halfWidth = slotSize.x * 0.5f * 1.14f;
        float halfHeight = slotSize.y * 0.5f;

        tooltip.anchorMin = slotRect.anchorMin;
        tooltip.anchorMax = slotRect.anchorMax;
        tooltip.pivot = new Vector2(1f, 1f);
        tooltip.anchoredPosition = new Vector2(center.x - halfWidth - tooltipGap, center.y + halfHeight);
    }
}
