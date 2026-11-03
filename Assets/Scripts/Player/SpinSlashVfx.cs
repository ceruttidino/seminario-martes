using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SpinSlashVfx : MonoBehaviour
{
    [Header("Forma de la hoja")]
    [Tooltip("Radio por defecto. AreaAttack lo reemplaza por su radio real al spawnear.")]
    [SerializeField] private float radius = 2f;
    [Tooltip("Qué tan lejos del centro va la hoja, en proporción al radio del ataque. 1 = justo en el borde del área de daño.")]
    [SerializeField, Range(0.3f, 1.2f)] private float bladeRadiusMultiplier = 0.8f;
    [Tooltip("Grosor máximo de la hoja")]
    [SerializeField] private float maxWidth = 0.3f;
    [Tooltip("Largo de la estela detrás de la punta, en grados")]
    [SerializeField, Range(45f, 360f)] private float trailAngle = 270f;
    [SerializeField, Range(16, 96)] private int segments = 48;
    [Tooltip("Sentido del giro")]
    [SerializeField] private bool clockwise = true;

    [Header("Animación de la hoja")]
    [Tooltip("Cuánto tarda en dar la vuelta completa")]
    [SerializeField] private float spinTime = 0.14f;
    [Tooltip("Grados que sigue girando mientras se cierra la estela (le da inercia al final)")]
    [SerializeField] private float overshootAngle = 60f;
    [Tooltip("Cuánto tarda en cerrarse y desaparecer después de la vuelta")]
    [SerializeField] private float fadeTime = 0.12f;
    [SerializeField] private Color color = Color.white;

    [Header("Onda expansiva (opcional)")]
    [SerializeField] private bool shockwaveEnabled = true;
    [Tooltip("Hasta dónde llega la onda, en proporción al radio. 1 = borde exacto del área de daño.")]
    [SerializeField, Range(0.5f, 1.5f)] private float shockwaveRadiusMultiplier = 1f;
    [SerializeField] private float shockwaveWidth = 0.06f;
    [SerializeField] private float shockwaveTime = 0.22f;
    [SerializeField] private Color shockwaveColor = new Color(1f, 1f, 1f, 0.6f);

    [Header("Render")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 20;
    [Tooltip("Ignora la escala del player (el squash del ataque deformaría el círculo en un óvalo)")]
    [SerializeField] private bool ignoreParentScale = true;

    private LineRenderer blade;
    private LineRenderer ring;
    private float timer;
    private float startAngle;

    public void Setup(float attackRadius, Vector2 startDirection)
    {
        radius = Mathf.Max(0.1f, attackRadius);

        if (startDirection.sqrMagnitude > 0.0001f)
            startAngle = Mathf.Atan2(startDirection.y, startDirection.x) * Mathf.Rad2Deg;

        timer = 0f;
        UpdateBlade();
        UpdateRing();
    }

    private void Awake()
    {
        blade = GetComponent<LineRenderer>();
        ConfigureLine(blade);
        blade.loop = false;
        blade.positionCount = segments + 1;

        blade.widthCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.75f, 1f),
            new Keyframe(1f, 0.2f));

        if (shockwaveEnabled)
            CreateRing();
    }

    private void ConfigureLine(LineRenderer line)
    {
        line.useWorldSpace = false;
        line.numCapVertices = 0;
        line.numCornerVertices = 2;
        line.alignment = LineAlignment.TransformZ;
        line.sortingLayerName = sortingLayerName;
        line.sortingOrder = sortingOrder;
    }

    private void CreateRing()
    {
        GameObject go = new GameObject("Shockwave");
        go.transform.SetParent(transform, false);

        ring = go.AddComponent<LineRenderer>();
        ConfigureLine(ring);
        ring.sortingOrder = sortingOrder - 1;
        ring.sharedMaterial = blade.sharedMaterial;
        ring.loop = true;
        ring.positionCount = segments;
        ring.widthMultiplier = shockwaveWidth;
    }

    private void OnEnable()
    {
        timer = 0f;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        UpdateBlade();
        UpdateRing();
    }

    private void LateUpdate()
    {
        if (!ignoreParentScale || transform.parent == null) return;

        Vector3 ps = transform.parent.lossyScale;
        transform.localScale = new Vector3(
            ps.x != 0f ? 1f / ps.x : 1f,
            ps.y != 0f ? 1f / ps.y : 1f,
            1f);
    }

    private void UpdateBlade()
    {
        if (blade == null) return;

        float spin = spinTime > 0f ? Mathf.Clamp01(timer / spinTime) : 1f;
        float fade = fadeTime > 0f ? Mathf.Clamp01((timer - spinTime) / fadeTime) : (spin >= 1f ? 1f : 0f);

        float headDeg = 360f * EaseOutCubic(spin);
        if (spin >= 1f)
            headDeg += overshootAngle * EaseOutCubic(fade);

        float trail = Mathf.Min(trailAngle, headDeg);
        if (spin >= 1f)
            trail = Mathf.Lerp(trailAngle, 0f, fade * fade);

        float tailDeg = headDeg - trail;
        float dir = clockwise ? -1f : 1f;
        float bladeRadius = radius * bladeRadiusMultiplier;

        for (int i = 0; i <= segments; i++)
        {
            float deg = startAngle + dir * Mathf.Lerp(tailDeg, headDeg, i / (float)segments);
            float rad = deg * Mathf.Deg2Rad;
            blade.SetPosition(i, new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * bladeRadius);
        }

        blade.widthMultiplier = maxWidth * (1f - fade * 0.5f);

        float alpha = color.a * (1f - fade);
        blade.startColor = new Color(color.r, color.g, color.b, alpha * 0.15f);
        blade.endColor = new Color(color.r, color.g, color.b, alpha);

        blade.enabled = fade < 1f;
    }

    private void UpdateRing()
    {
        if (ring == null) return;

        float t = shockwaveTime > 0f ? Mathf.Clamp01(timer / shockwaveTime) : 1f;

        float r = Mathf.Lerp(radius * 0.3f, radius * shockwaveRadiusMultiplier, EaseOutCubic(t));

        for (int i = 0; i < segments; i++)
        {
            float rad = (i / (float)segments) * Mathf.PI * 2f;
            ring.SetPosition(i, new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * r);
        }

        float alpha = shockwaveColor.a * (1f - t);
        Color c = new Color(shockwaveColor.r, shockwaveColor.g, shockwaveColor.b, alpha);
        ring.startColor = c;
        ring.endColor = c;
        ring.widthMultiplier = shockwaveWidth * (1f - t * 0.5f);

        ring.enabled = t < 1f;
    }

    private static float EaseOutCubic(float x)
    {
        return 1f - Mathf.Pow(1f - x, 3f);
    }
}
