using UnityEngine;

/// <summary>
/// Arco de golpe igual al del jugador, en rojo. Solo lo disparan los ataques cuerpo a cuerpo.
/// </summary>
public class EnemyMeleeSlash : MonoBehaviour
{
    private LineRenderer line;
    private float timer;
    private float radius = 0.7f;
    private float arcAngle = 180f;
    private float maxWidth = 0.16f;
    private float sweepTime = 0.06f;
    private float fadeTime = 0.12f;
    private Color color = new Color(0.92f, 0.08f, 0.1f, 1f);
    private const int Segments = 24;

    public static void Spawn(Vector3 origin, Vector2 direction, float radius)
    {
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.right;

        GameObject go = new GameObject("EnemyMeleeSlash");
        go.transform.position = origin;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        EnemyMeleeSlash slash = go.AddComponent<EnemyMeleeSlash>();
        slash.radius = Mathf.Max(0.2f, radius);
    }

    private void Awake()
    {
        line = gameObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = Segments + 1;
        line.numCapVertices = 0;
        line.numCornerVertices = 2;
        line.alignment = LineAlignment.TransformZ;
        line.sortingLayerName = "Default";
        line.sortingOrder = 25;
        line.widthCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.65f, 1f),
            new Keyframe(1f, 0.15f));

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader != null)
            line.material = new Material(shader);
    }

    private void Update()
    {
        timer += Time.deltaTime;

        float sweep = sweepTime > 0f ? Mathf.Clamp01(timer / sweepTime) : 1f;
        float fade = fadeTime > 0f ? Mathf.Clamp01((timer - sweepTime) / fadeTime) : 1f;

        float headT = 1f - Mathf.Pow(1f - sweep, 3f);
        float tailT = fade * fade;

        float start = -arcAngle * 0.5f;
        float head = start + arcAngle * headT;
        float tail = Mathf.Lerp(start, head, tailT);

        for (int i = 0; i <= Segments; i++)
        {
            float angle = Mathf.Lerp(tail, head, i / (float)Segments) * Mathf.Deg2Rad;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
        }

        line.widthMultiplier = maxWidth * (1f - fade * 0.5f);
        float alpha = color.a * (1f - fade);
        line.startColor = new Color(color.r, color.g, color.b, alpha * 0.25f);
        line.endColor = new Color(color.r, color.g, color.b, alpha);

        if (fade >= 1f)
            Destroy(gameObject);
    }
}
