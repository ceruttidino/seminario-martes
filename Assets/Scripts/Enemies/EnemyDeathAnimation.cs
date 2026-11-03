using UnityEngine;

/// <summary>
/// Va en el root de un enemigo. Cuando muere (EnemyHealth.OnDeath) reproduce su animacion de muerte con un
/// EnemyDeathFx aparte, porque el enemigo se destruye en el mismo frame.
///
/// - Usa el material del enemigo EN REPOSO (el que tenia al aparecer): al recibir el golpe final el enemigo ya tiene
///   puesto el material de flash blanco de HitReaction y la animacion saldria como una silueta blanca.
/// - Si el enemigo tiene una sombra ("Shadow"), la copia y la apaga durante la animacion.
/// - La Hormiga y la Serpiente tienen sus propios efectos de muerte; la Tortuga no tiene animacion.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyDeathAnimation : MonoBehaviour
{
    [Header("Animacion")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fps = 14f;
    [Tooltip("Primer cuadro de la hoja que se muestra al morir.")]
    [SerializeField] private int startFrame;
    [Tooltip("Segundos que el ultimo cuadro se queda y se desvanece (0 = desaparece al terminar la hoja).")]
    [SerializeField] private float lastFrameFade;

    [Header("Dibujo")]
    [Tooltip("Sprite del enemigo. Vacio = el SpriteRenderer de este objeto.")]
    [SerializeField] private SpriteRenderer source;
    [Tooltip("Sombra del enemigo: se copia y se apaga con la animacion. Vacio = hijo llamado 'Shadow' (si existe).")]
    [SerializeField] private Transform shadow;
    [Tooltip("Se suma al orden de dibujo del enemigo (para dibujarse por encima de otros efectos).")]
    [SerializeField] private int sortingBoost;
    [Tooltip("Espeja la animacion si el enemigo estaba mirando hacia el otro lado.")]
    [SerializeField] private bool copyFlip = true;
    [Tooltip("true: usa la escala que tiene el enemigo al morir (ej: erizo hinchado). false: la escala de reposo.")]
    [SerializeField] private bool useLiveScale;

    private EnemyHealth health;
    private Material restMaterial;
    private Vector3 restScale = Vector3.one;

    private SpriteRenderer shadowRenderer;
    private Sprite shadowSprite;
    private Material shadowMaterial;
    private Color shadowColor;

    /// <summary>Cuadros de la hoja de muerte (los usa tambien el Erizo para la previa de la explosion).</summary>
    public Sprite[] Frames => frames;

    public float Fps => fps;
    public int StartFrame => startFrame;

    public bool HasFrames => frames != null && frames.Length > 0;

    /// <summary>Segundos que dura la animacion (desde el cuadro inicial).</summary>
    public float Duration => HasFrames
        ? Mathf.Max(0, frames.Length - Mathf.Clamp(startFrame, 0, frames.Length - 1)) / Mathf.Max(1f, fps) + lastFrameFade
        : 0f;

    /// <summary>Si alguien lo asigna antes de que muera el enemigo, esa escala se usa (una sola vez) en lugar de la de reposo.</summary>
    public Vector3? ScaleOverride { get; set; }

    /// <summary>Ultima animacion creada (para pruebas).</summary>
    public EnemyDeathFx LastFx { get; private set; }

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        if (source == null)
            source = GetComponent<SpriteRenderer>();

        if (shadow == null)
            shadow = transform.Find("Shadow");

        restScale = transform.lossyScale;

        // Se guarda ahora, antes de que cualquier golpe cambie material / color.
        if (source != null)
            restMaterial = source.sharedMaterial;

        if (shadow != null)
        {
            shadowRenderer = shadow.GetComponent<SpriteRenderer>();
            if (shadowRenderer != null)
            {
                shadowSprite = shadowRenderer.sprite;
                shadowMaterial = shadowRenderer.sharedMaterial;
                shadowColor = shadowRenderer.color;
            }
        }

        if (health != null)
            health.OnDeath += Play;
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnDeath -= Play;
    }

    private void Play()
    {
        if (!HasFrames || source == null)
            return;

        Vector3 scale = restScale;
        if (ScaleOverride.HasValue)
            scale = ScaleOverride.Value;
        else if (useLiveScale)
            scale = transform.lossyScale;
        ScaleOverride = null;

        RoomInstance room = GetComponentInParent<RoomInstance>();
        Transform parent = room != null ? room.transform : transform.parent;

        EnemyDeathFx.ShadowInfo? shadowInfo = null;
        if (shadowRenderer != null && shadowSprite != null)
        {
            shadowInfo = new EnemyDeathFx.ShadowInfo
            {
                sprite = shadowSprite,
                material = shadowMaterial,
                color = shadowColor,
                localPosition = shadow.localPosition,
                localScale = shadow.localScale,
                sortingLayerID = shadowRenderer.sortingLayerID,
                sortingOrder = shadowRenderer.sortingOrder
            };
        }

        LastFx = EnemyDeathFx.Spawn(frames, fps, source, restMaterial, scale, parent, startFrame, lastFrameFade,
            sortingBoost, copyFlip && source.flipX, shadowInfo);
    }
}
