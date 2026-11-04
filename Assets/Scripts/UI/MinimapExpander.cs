using UnityEngine;

/// <summary>
/// Va en el contenedor del minimapa (MinimapRoot). El minimapa esta SIEMPRE visible, chico, arriba a la derecha (practicamente
/// sobre el muro); con TAB se expande a un tamano bastante mas grande y vuelve a achicarse al cerrar.
///
/// Solo cambia el tamano del contenedor (los marcos, el boton TAB y la grilla de buffs cuelgan de el con anclas
/// proporcionales, asi que acompanan solos) y el zoom del contenido del mapa (MinimapUI.SetViewScale).
/// Usa tiempo sin escala: la transicion corre igual con el juego en pausa o con hit stop.
/// </summary>
[DisallowMultipleComponent]
public sealed class MinimapExpander : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Contenedor que cambia de tamano. Vacio = el RectTransform de este objeto.")]
    [SerializeField] private RectTransform root;
    [SerializeField] private MinimapUI minimap;

    [Header("Tamano (pixeles de referencia 1920x1080)")]
    [SerializeField] private Vector2 compactSize = new Vector2(204f, 152f);
    [SerializeField] private Vector2 expandedSize = new Vector2(432f, 322f);

    [Header("Zoom del contenido del mapa")]
    [Tooltip("Menor a 1: entran mas salas en el minimapa chico.")]
    [SerializeField] private float compactContentScale = 0.6f;
    [SerializeField] private float expandedContentScale = 0.9f;

    [Header("Transicion")]
    [SerializeField] private float transitionTime = 0.2f;

    private float blend;        // 0 = chico, 1 = expandido
    private bool expanded;

    public bool IsExpanded => expanded;

    private void Awake()
    {
        if (root == null)
            root = transform as RectTransform;

        if (minimap == null)
            minimap = GetComponentInChildren<MinimapUI>(true);

        Apply(0f);
    }

    /// <summary>Expande o achica el minimapa. 'instant' salta la animacion.</summary>
    public void SetExpanded(bool value, bool instant = false)
    {
        expanded = value;

        if (instant)
        {
            blend = value ? 1f : 0f;
            Apply(blend);
        }
    }

    private void Update()
    {
        float target = expanded ? 1f : 0f;
        if (Mathf.Approximately(blend, target))
            return;

        float step = Time.unscaledDeltaTime / Mathf.Max(0.01f, transitionTime);
        blend = Mathf.MoveTowards(blend, target, step);
        Apply(blend);
    }

    private void Apply(float amount)
    {
        float k = amount * amount * (3f - 2f * amount);

        if (root != null)
            root.sizeDelta = Vector2.Lerp(compactSize, expandedSize, k);

        if (minimap != null)
            minimap.SetViewScale(Mathf.Lerp(compactContentScale, expandedContentScale, k));
    }
}
