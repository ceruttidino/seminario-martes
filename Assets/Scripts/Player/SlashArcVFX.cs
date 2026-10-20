using UnityEngine;

// NUEVO (archivo completo)
// Slash en forma de semicírculo dibujado por código (LineRenderer).
// Barre el arco rápido, después se "cierra" desde la cola y se desvanece.
// El arco mira hacia +X local: QuickAttack ya lo rota hacia la dirección del ataque
// y lo espeja en los golpes alternados.
[RequireComponent(typeof(LineRenderer))]
public class SlashArcVfx : MonoBehaviour
{
    [Header("Forma")]
    [Tooltip("Qué tan lejos del centro del player se dibuja el arco")]
    [SerializeField] private float radius = 0.65f;
    [Tooltip("180 = semicírculo. Menos = arco más cerrado")]
    [SerializeField, Range(30f, 300f)] private float arcAngle = 180f;
    [Tooltip("Grosor máximo del filo")]
    [SerializeField] private float maxWidth = 0.18f;
    [SerializeField, Range(8, 64)] private int segments = 24;

    [Header("Animación")]
    [Tooltip("Cuánto tarda en barrer el arco completo")]
    [SerializeField] private float sweepTime = 0.06f;
    [Tooltip("Cuánto tarda en cerrarse y desaparecer después del barrido")]
    [SerializeField] private float fadeTime = 0.12f;
    [SerializeField] private Color color = Color.white;

    [Header("Render")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 20;

    private LineRenderer line;
    private float timer;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = false;      // así sigue la rotación/espejado que le da QuickAttack
        line.positionCount = segments + 1;
        line.numCapVertices = 0;
        line.numCornerVertices = 2;
        line.alignment = LineAlignment.TransformZ;
        line.sortingLayerName = sortingLayerName;
        line.sortingOrder = sortingOrder;

        // Forma de medialuna: fino en la cola, grueso cerca de la punta, fino en la punta
        line.widthCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.65f, 1f),
            new Keyframe(1f, 0.15f));
    }

    private void OnEnable()
    {
        timer = 0f;
        if (line != null) line.enabled = true;
        UpdateArc();
    }

    private void Update()
    {
        // Tiempo escalado a propósito: durante el hit stop el slash queda congelado
        timer += Time.deltaTime;
        UpdateArc();
    }

    private void UpdateArc()
    {
        float sweep = sweepTime > 0f ? Mathf.Clamp01(timer / sweepTime) : 1f;
        float fade = fadeTime > 0f ? Mathf.Clamp01((timer - sweepTime) / fadeTime) : 1f;

        // La punta sale disparada (ease out) y la cola la persigue al final (ease in)
        float headT = 1f - Mathf.Pow(1f - sweep, 3f);
        float tailT = fade * fade;

        float start = -arcAngle * 0.5f;
        float head = start + arcAngle * headT;
        float tail = Mathf.Lerp(start, head, tailT);

        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.Lerp(tail, head, i / (float)segments) * Mathf.Deg2Rad;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
        }

        line.widthMultiplier = maxWidth * (1f - fade * 0.5f);

        float alpha = color.a * (1f - fade);
        line.startColor = new Color(color.r, color.g, color.b, alpha * 0.25f); // cola más transparente
        line.endColor = new Color(color.r, color.g, color.b, alpha);

        if (fade >= 1f)
            line.enabled = false;
    }
}