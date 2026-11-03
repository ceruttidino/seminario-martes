using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Una celda de la grilla de buffs del menu TAB. Con un buff recogido muestra su icono (y la cantidad si es mas de uno);
/// al pasar el mouse por encima se agranda un poco, se ilumina y pide a la grilla que muestre su descripcion.
/// Las celdas vacias se ven apagadas y no reaccionan al mouse.
/// </summary>
public sealed class BuffSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Partes")]
    [Tooltip("Imagen invisible que recibe el mouse (ocupa toda la celda).")]
    [SerializeField] private Image hitArea;
    [SerializeField] private Image border;
    [SerializeField] private Image inner;
    [SerializeField] private Image icon;
    [SerializeField] private Image glow;
    [SerializeField] private TMP_Text count;

    [Header("Colores")]
    [SerializeField] private Color borderColor = new Color(0.55f, 0.43f, 0.27f, 1f);
    [SerializeField] private Color borderHoverColor = new Color(1f, 0.86f, 0.5f, 1f);
    [SerializeField] private Color borderEmptyColor = new Color(0.27f, 0.21f, 0.15f, 1f);
    [SerializeField] private Color innerColor = new Color(0.16f, 0.12f, 0.09f, 0.95f);
    [SerializeField] private Color innerEmptyColor = new Color(0.1f, 0.08f, 0.06f, 0.7f);

    [Header("Hover")]
    [SerializeField] private float hoverScale = 1.14f;
    [Tooltip("Cuanto se ilumina la celda con el mouse encima (0-1).")]
    [SerializeField, Range(0f, 1f)] private float hoverGlow = 0.26f;
    [SerializeField] private float hoverSpeed = 14f;

    private BuffGridUI grid;
    private float hoverAmount;
    private bool hovered;

    public UpgradeSO Upgrade { get; private set; }
    public int Amount { get; private set; }
    public bool HasItem => Upgrade != null;

    /// <summary>La grilla se registra al armarse para recibir los eventos de mouse.</summary>
    public void Bind(BuffGridUI owner)
    {
        grid = owner;
    }

    public void SetItem(UpgradeSO upgrade, int amount)
    {
        Upgrade = upgrade;
        Amount = Mathf.Max(1, amount);

        gameObject.SetActive(true);
        if (hitArea != null) hitArea.raycastTarget = true;

        if (icon != null)
        {
            icon.sprite = upgrade.icon;
            icon.enabled = upgrade.icon != null;
            icon.color = Color.white;
        }

        if (count != null)
        {
            count.text = Amount > 1 ? $"x{Amount}" : string.Empty;
            count.enabled = Amount > 1;
        }

        ApplyColors();
    }

    public void SetEmpty()
    {
        Upgrade = null;
        Amount = 0;

        gameObject.SetActive(true);
        if (hitArea != null) hitArea.raycastTarget = false;
        if (icon != null) icon.enabled = false;
        if (count != null) { count.text = string.Empty; count.enabled = false; }

        SetHovered(false, true);
        ApplyColors();
    }

    public void Hide()
    {
        Upgrade = null;
        Amount = 0;
        SetHovered(false, true);
        gameObject.SetActive(false);
    }

    /// <summary>Apaga el hover sin animacion (al cerrar el menu).</summary>
    public void ResetHover()
    {
        SetHovered(false, true);
        ApplyColors();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!HasItem)
            return;

        SetHovered(true, false);
        if (grid != null)
            grid.ShowTooltip(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHovered(false, false);
        if (grid != null)
            grid.HideTooltip(this);
    }

    private void OnDisable()
    {
        SetHovered(false, true);
        if (grid != null)
            grid.HideTooltip(this);
    }

    private void SetHovered(bool value, bool instant)
    {
        hovered = value;
        if (instant)
        {
            hoverAmount = value ? 1f : 0f;
            ApplyHoverVisuals();
        }
    }

    private void Update()
    {
        float target = hovered ? 1f : 0f;
        if (Mathf.Approximately(hoverAmount, target))
            return;

        hoverAmount = Mathf.MoveTowards(hoverAmount, target, Time.unscaledDeltaTime * hoverSpeed);
        ApplyHoverVisuals();
    }

    private void ApplyColors()
    {
        if (border != null)
            border.color = HasItem ? Color.Lerp(borderColor, borderHoverColor, hoverAmount) : borderEmptyColor;

        if (inner != null)
            inner.color = HasItem ? innerColor : innerEmptyColor;

        ApplyHoverVisuals();
    }

    private void ApplyHoverVisuals()
    {
        float s = Mathf.Lerp(1f, hoverScale, hoverAmount);
        transform.localScale = new Vector3(s, s, 1f);

        if (glow != null)
        {
            Color c = glow.color;
            c.a = hoverGlow * hoverAmount;
            glow.color = c;
        }

        if (border != null && HasItem)
            border.color = Color.Lerp(borderColor, borderHoverColor, hoverAmount);
    }
}
