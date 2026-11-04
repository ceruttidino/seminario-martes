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
    private bool spinning;
    private float spinAngle;
    private Transform follow;
    private const int Segments = 24;

    public static void Spawn(Vector3 origin, Vector2 direction, float radius, float width = 0.16f)
    {
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.right;

        GameObject go = new GameObject("EnemyMeleeSlash");
        go.transform.position = origin;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        EnemyMeleeSlash slash = go.AddComponent<EnemyMeleeSlash>();
        slash.radius = Mathf.Max(0.2f, radius);
        slash.maxWidth = Mathf.Max(0.04f, width);
        slash.ShowImmediate();
    }

    /// <summary>Arco rojo que gira alrededor del enemigo hasta que se llama End.</summary>
    public static EnemyMeleeSlash BeginRing(Transform follow, float radius)
    {
        GameObject go = new GameObject("EnemySpinSlash");
        go.transform.position = follow != null ? follow.position : Vector3.zero;

        EnemyMeleeSlash slash = go.AddComponent<EnemyMeleeSlash>();
        slash.follow = follow;
        slash.radius = Mathf.Max(0.2f, radius);
        slash.maxWidth = 0.22f;
        slash.arcAngle = 320f;
        slash.spinning = true;
        slash.ShowImmediate();
        return slash;
    }

    private void ShowImmediate()
    {
        if (line == null)
            return;

        if (spinning)
        {
            DrawArc(-arcAngle * 0.5f + spinAngle, arcAngle, 1f);
            return;
        }

        // Nace ya abierto. Si arranca en cero, el daño se ve antes que el arco.
        timer = sweepTime;
        DrawArc(-arcAngle * 0.5f, arcAngle, 1f);
    }

    public void End()
    {
        if (!spinning)
            return;

        spinning = false;
        timer = sweepTime;
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
        if (spinning)
        {
            if (follow != null)
                transform.position = follow.position;

            spinAngle += 720f * Time.deltaTime;
            DrawArc(-arcAngle * 0.5f + spinAngle, arcAngle, 1f);
            return;
        }

        timer += Time.deltaTime;

        float sweep = sweepTime > 0f ? Mathf.Clamp01(timer / sweepTime) : 1f;
        float fade = fadeTime > 0f ? Mathf.Clamp01((timer - sweepTime) / fadeTime) : 1f;

        float headT = 1f - Mathf.Pow(1f - sweep, 3f);
        float tailT = fade * fade;

        float start = -arcAngle * 0.5f;
        float head = start + arcAngle * headT;
        float tail = Mathf.Lerp(start, head, tailT);
        DrawArc(tail, head - tail, 1f - fade);

        if (fade >= 1f)
            Destroy(gameObject);
    }

    private void DrawArc(float startDeg, float spanDeg, float alphaScale)
    {
        for (int i = 0; i <= Segments; i++)
        {
            float angle = (startDeg + spanDeg * (i / (float)Segments)) * Mathf.Deg2Rad;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
        }

        line.widthMultiplier = maxWidth * (1f - (1f - alphaScale) * 0.5f);
        float alpha = color.a * alphaScale;
        line.startColor = new Color(color.r, color.g, color.b, alpha * 0.25f);
        line.endColor = new Color(color.r, color.g, color.b, alpha);
    }
}
