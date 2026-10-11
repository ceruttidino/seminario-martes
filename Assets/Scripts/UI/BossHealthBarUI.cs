using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de vida del jefe. Toma TODO del jefe de la sala (BossBase): el nombre (BarTitle), el color del relleno
/// (BarFillColor), la opacidad (BarOpacity, para los fades) y, si hace falta, un valor de relleno forzado
/// (BarFillOverride, para "llenar" la barra nueva del Cuervo). Si el jefe no define nada se ve como siempre: el texto y el
/// color que trae el prefab y la vida real.
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyHealth bossHealth;
    [SerializeField] private Slider healthSlider;
    [Tooltip("Texto con el nombre del jefe. Si queda vacio se busca solo (el TMP hijo).")]
    [SerializeField] private TMP_Text nameText;

    // Los nombres largos ("The Possessed Crow") no entran en la caja original de 300 px: se ensancha y se evita el salto de linea.
    private const float MinNameWidth = 700f;

    private BossBase boss;
    private CanvasGroup canvasGroup;
    private Image fillImage;
    private Color authoredFillColor = Color.white;
    private string shownTitle;

    private void Awake()
    {
        if (healthSlider == null)
            healthSlider = GetComponentInChildren<Slider>();

        if (nameText == null)
            nameText = GetComponentInChildren<TMP_Text>(true);

        boss = FindBoss();

        if (bossHealth == null && boss != null)
            bossHealth = boss.GetComponent<EnemyHealth>();

        if (boss == null && bossHealth != null)
            boss = bossHealth.GetComponent<BossBase>();

        if (healthSlider != null && healthSlider.fillRect != null)
        {
            fillImage = healthSlider.fillRect.GetComponent<Image>();
            if (fillImage != null)
                authoredFillColor = fillImage.color;
        }

        // Para poder apagar/prender toda la barra con un fade sin tocar cada imagen.
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        PrepareNameText();
    }

    // Sirve para cualquier boss que herede de BossBase
    private BossBase FindBoss()
    {
        // 1) La barra es hija del boss
        BossBase found = GetComponentInParent<BossBase>();

        // 2) La barra está en la misma room que el boss
        if (found == null)
        {
            RoomInstance room = GetComponentInParent<RoomInstance>();
            if (room != null)
                found = room.GetComponentInChildren<BossBase>(true);
        }

        // 3) Último recurso: cualquier boss de la escena
        if (found == null)
            found = FindFirstObjectByType<BossBase>();

        return found;
    }

    private void PrepareNameText()
    {
        if (nameText == null)
            return;

        RectTransform rect = nameText.rectTransform;
        Vector2 size = rect.sizeDelta;
        if (size.x < MinNameWidth)
        {
            size.x = MinNameWidth;
            rect.sizeDelta = size;
        }

        nameText.textWrappingMode = TextWrappingModes.NoWrap;
        nameText.overflowMode = TextOverflowModes.Overflow;
    }

    private void OnEnable()
    {
        if (bossHealth != null)
            bossHealth.OnDeath += Hide;
    }

    private void OnDisable()
    {
        if (bossHealth != null)
            bossHealth.OnDeath -= Hide;
    }

    private void Update()
    {
        if (bossHealth == null || healthSlider == null)
        {
            gameObject.SetActive(false);
            return;
        }

        float fill = bossHealth.MaxHealth > 0f ? bossHealth.CurrentHealth / bossHealth.MaxHealth : 0f;

        if (boss != null)
        {
            ApplyTitle(boss.BarTitle);
            ApplyFillColor(boss.BarFillColor);

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Clamp01(boss.BarOpacity);

            float forced = boss.BarFillOverride;
            if (forced >= 0f)
                fill = forced;
        }

        healthSlider.value = Mathf.Clamp01(fill);
    }

    private void ApplyTitle(string title)
    {
        if (nameText == null || string.IsNullOrEmpty(title) || title == shownTitle)
            return;

        nameText.text = title;
        shownTitle = title;
    }

    private void ApplyFillColor(Color? color)
    {
        if (fillImage == null)
            return;

        Color target = color ?? authoredFillColor;
        if (fillImage.color != target)
            fillImage.color = target;
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
