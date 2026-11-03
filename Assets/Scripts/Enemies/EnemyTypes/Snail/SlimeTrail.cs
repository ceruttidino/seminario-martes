using System.Collections.Generic;
using UnityEngine;

// NUEVO (archivo completo)
// Configuración de la baba. Se edita desde el SlimeTrailSpawner del caracol.
[System.Serializable]
public class SlimeTrailSettings
{
    [Header("Forma")]
    [Tooltip("Grosor de la baba recién salida")]
    public float width = 0.28f;
    [Tooltip("Cuánto varía el grosor a lo largo de la línea (bordes orgánicos). 0 = grosor parejo.")]
    [Range(0f, 0.6f)] public float wobble = 0.25f;
    [Tooltip("Cada cuánta distancia recorrida se agrega un punto. Más chico = curvas más suaves.")]
    public float minPointDistance = 0.12f;
    [Tooltip("Largo máximo de la baba (en puntos). Reemplaza al viejo 'Max Slime Instances'.")]
    public int maxPoints = 80;

    [Header("Tiempo")]
    [Tooltip("Cuánto dura cada tramo de baba antes de desaparecer (igual que el lifetime del SlimeTile viejo)")]
    public float lifetime = 3f;
    [Tooltip("En qué parte de su vida empieza a secarse (afinarse). 0.6 = al 60% del lifetime.")]
    [Range(0f, 1f)] public float dryStart = 0.6f;
    [Tooltip("Tiempo que tarda la baba recién salida en 'desparramarse' hasta su grosor completo")]
    public float spreadTime = 0.12f;

    [Header("Color")]
    public Color bodyColor = new Color(0.55f, 0.85f, 0.35f, 0.75f);
    [Tooltip("Línea fina más clara encima de la baba: es lo que la hace ver húmeda/brillante")]
    public bool shineEnabled = true;
    public Color shineColor = new Color(0.9f, 1f, 0.75f, 0.55f);
    [Range(0.1f, 0.8f)] public float shineWidth = 0.3f;
    public Vector2 shineOffset = new Vector2(0f, 0.025f);

    [Header("Render")]
    [Tooltip("Material del LineRenderer. Usá Sprite-Unlit-Default (o el mismo de los slashes).")]
    public Material material;
    public string sortingLayerName = "Default";
    [Tooltip("Por debajo de los enemigos (que dibujan desde 2) y por encima del piso")]
    public int sortingOrder = 1;

    [Header("Daño")]
    public float damage = 1f;
    [Tooltip("Cada cuánto puede volver a dañar mientras el player sigue parado encima. La invulnerabilidad de PlayerHealth igual manda.")]
    public float damageInterval = 0.5f;
    [Tooltip("Qué parte de su vida es peligrosa. 0.85 = la punta que ya se está secando no daña.")]
    [Range(0.1f, 1f)] public float dangerFraction = 0.85f;
    [Tooltip("Ancho de la zona de daño respecto al grosor visible")]
    [Range(0.3f, 1.5f)] public float hitWidthFraction = 0.8f;
    [Tooltip("Corrección del punto del player que 'pisa' (sus pies). Si el pivot del player está en el centro, bajalo (ej: 0, -0.3).")]
    public Vector2 playerFeetOffset = Vector2.zero;

    [Header("Slow (opcional)")]
    [Tooltip("0 = no ralentiza (como antes). 0.3 = 30% más lento mientras está encima.")]
    [Range(0f, 0.9f)] public float slowPercent = 0f;
    [Tooltip("Cuánto dura el slow después de salir de la baba")]
    public float slowLinger = 0.2f;
}

// NUEVO (archivo completo)
// La baba en sí: una línea continua (LineRenderer) que sigue al caracol.
// Cada tramo tiene su edad: se desparrama al salir, se seca desde la cola y desaparece.
// El daño se calcula midiendo la distancia de los pies del player a la línea
// (no usa colliders, así no hay que configurar capas ni triggers).
// Vive aparte del caracol: si el caracol muere, la baba que dejó se termina de secar sola.
public class SlimeTrail : MonoBehaviour
{
    private struct TrailPoint
    {
        public Vector2 position;
        public float age;
        public float seed; // valor fijo por punto: da los bultitos de grosor
    }

    private SlimeTrailSettings settings;

    private readonly List<TrailPoint> points = new List<TrailPoint>();    // del más viejo al más nuevo
    private readonly List<TrailPoint> renderList = new List<TrailPoint>(); // points + la "cabeza" pegada al caracol
    private Vector3[] positionBuffer = new Vector3[0];
    private Keyframe[] widthKeys = new Keyframe[0];

    private LineRenderer body;
    private LineRenderer shine;

    private Vector2 headPosition;
    private bool hasHead;
    private float headSeed;
    private bool released;

    private Transform player;
    private IDamageable playerDamageable;
    private PlayerSlowStatus playerSlow;
    private float nextDamageTime;

    private static Material fallbackMaterial;

    /// <summary>Crea una baba nueva. La llama SlimeTrailSpawner.</summary>
    public static SlimeTrail Create(SlimeTrailSettings settings, Transform parent, GameObject owner)
    {
        GameObject go = new GameObject($"SlimeTrail_{owner.name}");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        // Igual que antes con los SlimeTile: el daño se le atribuye al caracol que la dejó
        DamageOwner.Assign(go, owner);

        SlimeTrail trail = go.AddComponent<SlimeTrail>();
        trail.Init(settings);
        return trail;
    }

    private void Init(SlimeTrailSettings s)
    {
        settings = s;
        headSeed = Random.value;

        body = CreateLine("Body", settings.sortingOrder);
        SetupGradient(body, settings.bodyColor);

        if (settings.shineEnabled)
        {
            shine = CreateLine("Shine", settings.sortingOrder + 1);
            SetupGradient(shine, settings.shineColor);
        }
    }

    private LineRenderer CreateLine(string lineName, int order)
    {
        GameObject go = new GameObject(lineName);
        go.transform.SetParent(transform, false);

        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.alignment = LineAlignment.TransformZ;
        line.textureMode = LineTextureMode.Stretch;
        line.numCapVertices = 4;    // puntas redondeadas
        line.numCornerVertices = 4; // curvas suaves
        line.sortingLayerName = settings.sortingLayerName;
        line.sortingOrder = order;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sharedMaterial = settings.material != null ? settings.material : GetFallbackMaterial();
        line.positionCount = 0;
        return line;
    }

    // La cola (inicio de la línea) se desvanece; el resto queda con el color pleno
    private static void SetupGradient(LineRenderer line, Color color)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(color.a, 0.15f), new GradientAlphaKey(color.a, 1f) });
        line.colorGradient = gradient;
    }

    private static Material GetFallbackMaterial()
    {
        if (fallbackMaterial == null)
        {
            Debug.LogWarning("[SlimeTrail] No hay material asignado: usando Sprites/Default. Asigná Sprite-Unlit-Default en el SlimeTrailSpawner del caracol.");
            fallbackMaterial = new Material(Shader.Find("Sprites/Default"));
        }
        return fallbackMaterial;
    }

    // ---------------- API para el spawner ----------------

    /// <summary>Le pasa la posición actual del caracol. Agrega puntos a medida que avanza.</summary>
    public void Feed(Vector2 position)
    {
        headPosition = position;
        hasHead = true;

        bool farEnough = points.Count == 0 ||
                         (position - points[points.Count - 1].position).sqrMagnitude >=
                         settings.minPointDistance * settings.minPointDistance;

        if (!farEnough) return;

        points.Add(new TrailPoint { position = position, age = 0f, seed = Random.value });

        while (points.Count > Mathf.Max(2, settings.maxPoints))
            points.RemoveAt(0);
    }

    /// <summary>El caracol murió o se destruyó: la baba deja de seguirlo y se seca sola.</summary>
    public void Release()
    {
        if (released) return;
        released = true;

        if (hasHead)
        {
            points.Add(new TrailPoint { position = headPosition, age = 0f, seed = headSeed });
            hasHead = false;
        }
    }

    // ---------------- Update ----------------

    private void Update()
    {
        if (settings == null) return;

        AgePoints(Time.deltaTime);

        if (released && points.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        BuildRenderList();
        UpdateLines();
        CheckPlayer();
    }

    private void AgePoints(float dt)
    {
        for (int i = 0; i < points.Count; i++)
        {
            TrailPoint p = points[i];
            p.age += dt;
            points[i] = p;
        }

        // Los más viejos están al principio
        while (points.Count > 0 && points[0].age >= settings.lifetime)
            points.RemoveAt(0);
    }

    private void BuildRenderList()
    {
        renderList.Clear();
        renderList.AddRange(points);

        // La cabeza conecta el último punto con el caracol, así la baba nunca queda "despegada"
        if (hasHead && !released &&
            (points.Count == 0 || (headPosition - points[points.Count - 1].position).sqrMagnitude > 0.0001f))
        {
            renderList.Add(new TrailPoint { position = headPosition, age = 0f, seed = headSeed });
        }
    }

    private void UpdateLines()
    {
        int count = renderList.Count;

        if (count < 2)
        {
            body.positionCount = 0;
            if (shine != null) shine.positionCount = 0;
            return;
        }

        if (positionBuffer.Length != count) positionBuffer = new Vector3[count];
        if (widthKeys.Length != count) widthKeys = new Keyframe[count];

        for (int i = 0; i < count; i++)
        {
            TrailPoint p = renderList[i];
            positionBuffer[i] = new Vector3(p.position.x, p.position.y, 0f);
            widthKeys[i] = new Keyframe(i / (float)(count - 1), WidthFactor(p));
        }

        AnimationCurve curve = new AnimationCurve(widthKeys);

        body.positionCount = count;
        body.SetPositions(positionBuffer);
        body.widthCurve = curve;
        body.widthMultiplier = settings.width;

        if (shine == null) return;

        Vector3 offset = settings.shineOffset;
        for (int i = 0; i < count; i++)
            positionBuffer[i] += offset;

        shine.positionCount = count;
        shine.SetPositions(positionBuffer);
        shine.widthCurve = curve;
        shine.widthMultiplier = settings.width * settings.shineWidth;
    }

    /// <summary>Grosor relativo de un punto según su edad: se desparrama al salir y se afina al secarse.</summary>
    private float WidthFactor(TrailPoint p)
    {
        float spread = settings.spreadTime > 0f
            ? Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(p.age / settings.spreadTime))
            : 1f;

        float dryStartTime = settings.lifetime * settings.dryStart;
        float dry = 1f;
        if (p.age > dryStartTime)
        {
            float dryDuration = Mathf.Max(0.01f, settings.lifetime - dryStartTime);
            dry = 1f - Mathf.Clamp01((p.age - dryStartTime) / dryDuration);
            dry = Mathf.SmoothStep(0f, 1f, dry);
        }

        float wobble = 1f + (p.seed * 2f - 1f) * settings.wobble;

        return spread * dry * wobble;
    }

    // ---------------- Daño ----------------

    private void CheckPlayer()
    {
        if (GamePause.IsGameplayFrozen) return;
        if (renderList.Count < 2) return;
        if (!EnsurePlayer()) return;

        Vector2 feet = (Vector2)player.position + settings.playerFeetOffset;
        if (!TouchesTrail(feet)) return;

        // Slow: se renueva mientras siga encima (y dura un poquito más al salir)
        if (settings.slowPercent > 0f && playerSlow != null)
            playerSlow.ApplySlow(settings.slowPercent, settings.slowLinger);

        if (Time.time < nextDamageTime) return;
        nextDamageTime = Time.time + settings.damageInterval;

        // Mismo llamado que el SlimeTile viejo: este objeto tiene el DamageOwner del caracol
        playerDamageable.TakeDamageFrom(settings.damage, gameObject);
    }

    private bool TouchesTrail(Vector2 point)
    {
        float dangerAge = settings.lifetime * settings.dangerFraction;
        float baseRadius = settings.width * 0.5f * settings.hitWidthFraction;

        for (int i = 0; i < renderList.Count - 1; i++)
        {
            TrailPoint a = renderList[i];
            TrailPoint b = renderList[i + 1];

            // La parte que ya se está secando no daña
            if (a.age >= dangerAge && b.age >= dangerAge) continue;

            float radius = baseRadius * Mathf.Max(WidthFactor(a), WidthFactor(b));
            if (DistanceToSegment(point, a.position, b.position) <= radius)
                return true;
        }

        return false;
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lengthSq = ab.sqrMagnitude;
        if (lengthSq < 0.000001f) return Vector2.Distance(p, a);

        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq);
        return Vector2.Distance(p, a + ab * t);
    }

    private bool EnsurePlayer()
    {
        if (player != null && playerDamageable != null) return true;

        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go == null) return false;

        player = go.transform;
        playerDamageable = go.GetComponent<IDamageable>();
        playerSlow = go.GetComponent<PlayerSlowStatus>();
        return playerDamageable != null;
    }
}
