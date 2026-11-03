using UnityEngine;

/// <summary>
/// Efectos de salpicadura para baba y veneno (impacto del veneno de la Serpiente, aterrizajes y muerte del jefe Slime):
/// gotas que salen disparadas y caen, anillos que se expanden en el piso, charquitos que se secan y el "derretido"
/// de un sprite. Todo se dibuja con sprites generados por codigo (sin assets nuevos): los efectos se animan solos y
/// se destruyen al terminar. No cuentan como enemigos ni tocan la jugabilidad.
/// </summary>
public sealed class GooFx : MonoBehaviour
{
    private enum Mode { Droplets, Ring, Splat, Melt }

    public const int DefaultSortingOrder = 14;

    private static Sprite dropSprite;
    private static Sprite ringSprite;
    private static Sprite splatSprite;

    private Mode mode;
    private float age;
    private float duration;

    // Gotas
    private SpriteRenderer[] drops;
    private Vector2[] startPos;
    private Vector2[] velocity;
    private float[] delay;
    private float[] life;
    private float[] size;
    private Color[] tint;
    private float gravity;

    // Anillo / charquito / derretido (un solo renderer)
    private SpriteRenderer single;
    private Color baseColor;
    private float startDiameter;
    private float endDiameter;
    private float flatten = 1f;

    // Derretido
    private Vector3 meltBaseScale;
    private float meltWiden;

    // ================================================================= fabricas

    /// <summary>Rafaga de gotas que salen de 'origin' hacia arriba y los costados, caen con gravedad y se desvanecen.</summary>
    public static void Droplets(Vector2 origin, Transform parent, int count, float minSpeed, float maxSpeed,
        float lifetime, float gravity, Color colorA, Color colorB, float minSize, float maxSize,
        int sortingOrder = DefaultSortingOrder)
    {
        if (count <= 0)
            return;

        GooFx fx = Create("GooDroplets", origin, parent);
        fx.mode = Mode.Droplets;
        fx.duration = lifetime + 0.35f;
        fx.gravity = gravity;
        fx.AllocateDrops(count, sortingOrder);

        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0.15f, Mathf.PI - 0.15f);
            float speed = Random.Range(minSpeed, maxSpeed);

            fx.startPos[i] = origin + new Vector2(Random.Range(-0.15f, 0.15f), Random.Range(-0.05f, 0.1f));
            fx.velocity[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            fx.delay[i] = Random.Range(0f, 0.08f);
            fx.life[i] = lifetime * Random.Range(0.65f, 1f);
            fx.size[i] = Random.Range(minSize, maxSize);
            fx.tint[i] = Color.Lerp(colorA, colorB, Random.value);
        }
    }

    /// <summary>Gotas que salen en TODAS las direcciones (impacto de un proyectil).</summary>
    public static void Burst(Vector2 origin, Transform parent, int count, float minSpeed, float maxSpeed,
        float lifetime, Color colorA, Color colorB, float minSize, float maxSize, int sortingOrder = DefaultSortingOrder)
    {
        if (count <= 0)
            return;

        GooFx fx = Create("GooBurst", origin, parent);
        fx.mode = Mode.Droplets;
        fx.duration = lifetime + 0.3f;
        fx.gravity = 0f;
        fx.AllocateDrops(count, sortingOrder);

        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float speed = Random.Range(minSpeed, maxSpeed);

            fx.startPos[i] = origin;
            fx.velocity[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            fx.delay[i] = 0f;
            fx.life[i] = lifetime * Random.Range(0.6f, 1f);
            fx.size[i] = Random.Range(minSize, maxSize);
            fx.tint[i] = Color.Lerp(colorA, colorB, Random.value);
        }
    }

    /// <summary>Una gota que se desprende y cae (baba chorreando).</summary>
    public static void Drip(Vector2 origin, Transform parent, Color color, float size, float lifetime = 0.4f,
        int sortingOrder = DefaultSortingOrder)
    {
        GooFx fx = Create("GooDrip", origin, parent);
        fx.mode = Mode.Droplets;
        fx.duration = lifetime + 0.1f;
        fx.gravity = 6f;
        fx.AllocateDrops(1, sortingOrder);

        fx.startPos[0] = origin;
        fx.velocity[0] = new Vector2(Random.Range(-0.12f, 0.12f), -0.35f);
        fx.delay[0] = 0f;
        fx.life[0] = lifetime;
        fx.size[0] = size;
        fx.tint[0] = color;
    }

    /// <summary>Anillo que crece en el piso (aplastado en vertical con 'flatten') mientras se desvanece.</summary>
    public static void Ring(Vector2 center, Transform parent, Color color, float startDiameter, float endDiameter,
        float time, float flatten = 0.55f, int sortingOrder = DefaultSortingOrder - 2)
    {
        GooFx fx = Create("GooRing", center, parent);
        fx.mode = Mode.Ring;
        fx.duration = Mathf.Max(0.1f, time);
        fx.startDiameter = startDiameter;
        fx.endDiameter = endDiameter;
        fx.flatten = Mathf.Clamp(flatten, 0.1f, 1f);
        fx.baseColor = color;

        fx.single = fx.gameObject.AddComponent<SpriteRenderer>();
        fx.single.sprite = RingSprite();
        fx.single.sortingOrder = sortingOrder;
        fx.single.color = new Color(color.r, color.g, color.b, 0f);
    }

    /// <summary>Mancha en el piso: aparece de golpe, se estira un poco y se seca (desvanece) en 'time' segundos.</summary>
    public static void Splat(Vector2 center, Transform parent, Color color, float diameter, float time,
        float flatten = 0.6f, int sortingOrder = 0)
    {
        GooFx fx = Create("GooSplat", center, parent);
        fx.mode = Mode.Splat;
        fx.duration = Mathf.Max(0.2f, time);
        fx.startDiameter = diameter * 0.55f;
        fx.endDiameter = diameter;
        fx.flatten = Mathf.Clamp(flatten, 0.1f, 1f);
        fx.baseColor = color;

        fx.single = fx.gameObject.AddComponent<SpriteRenderer>();
        fx.single.sprite = SplatSprite();
        fx.single.sortingOrder = sortingOrder;
        fx.single.color = color;
        fx.single.flipX = Random.value < 0.5f;
        fx.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-25f, 25f));
    }

    /// <summary>
    /// Derrite 'sprite' hasta dejarlo hecho un charco: se aplasta, se ensancha y se desvanece mientras suelta gotas.
    /// Pensado para el jefe Slime, que se destruye apenas muere.
    /// </summary>
    public static void Melt(Sprite sprite, Vector3 position, Vector3 scale, bool flipX, Color tint,
        int sortingOrder, Transform parent, float time, float widen = 1.45f)
    {
        if (sprite == null)
            return;

        GooFx fx = Create("GooMelt", position, parent);
        fx.mode = Mode.Melt;
        fx.duration = Mathf.Max(0.3f, time);
        fx.meltBaseScale = scale;
        fx.meltWiden = widen;
        fx.baseColor = tint;
        fx.transform.localScale = scale;

        fx.single = fx.gameObject.AddComponent<SpriteRenderer>();
        fx.single.sprite = sprite;
        fx.single.flipX = flipX;
        fx.single.color = tint;
        fx.single.sortingOrder = sortingOrder;
    }

    private static GooFx Create(string objectName, Vector2 position, Transform parent)
    {
        GameObject go = new GameObject(objectName);
        go.transform.position = position;
        if (parent != null)
            go.transform.SetParent(parent, true);

        return go.AddComponent<GooFx>();
    }

    private void AllocateDrops(int count, int sortingOrder)
    {
        drops = new SpriteRenderer[count];
        startPos = new Vector2[count];
        velocity = new Vector2[count];
        delay = new float[count];
        life = new float[count];
        size = new float[count];
        tint = new Color[count];

        Sprite sprite = DropSprite();
        for (int i = 0; i < count; i++)
        {
            GameObject drop = new GameObject("d" + i);
            drop.transform.SetParent(transform, false);

            SpriteRenderer renderer = drop.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.enabled = false;
            drops[i] = renderer;
        }
    }

    // ================================================================= animacion

    private void Update()
    {
        age += Time.deltaTime;

        switch (mode)
        {
            case Mode.Droplets: UpdateDroplets(); break;
            case Mode.Ring: UpdateRing(); break;
            case Mode.Splat: UpdateSplat(); break;
            case Mode.Melt: UpdateMelt(); break;
        }

        if (age >= duration)
            Destroy(gameObject);
    }

    private void UpdateDroplets()
    {
        const float drag = 2.2f;

        for (int i = 0; i < drops.Length; i++)
        {
            SpriteRenderer renderer = drops[i];
            float t = age - delay[i];
            if (t < 0f || t > life[i])
            {
                renderer.enabled = false;
                continue;
            }

            float u = t / life[i];
            float travel = (1f - Mathf.Exp(-drag * t)) / drag;
            Vector2 position = startPos[i] + velocity[i] * travel + 0.5f * gravity * t * t * Vector2.down;

            renderer.enabled = true;
            renderer.transform.position = position;

            // Se estiran un poco en la direccion en la que viajan y se achican al final.
            Vector2 heading = velocity[i] * Mathf.Exp(-drag * t) + gravity * t * Vector2.down;
            float angle = heading.sqrMagnitude > 0.0001f ? Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg : 0f;
            float s = size[i] * (1f - 0.55f * u);
            renderer.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            renderer.transform.localScale = new Vector3(s * 1.25f, s, 1f);

            Color color = tint[i];
            color.a = Mathf.Clamp01(1f - u * u * u);
            renderer.color = color;
        }
    }

    private void UpdateRing()
    {
        float u = Mathf.Clamp01(age / duration);
        float eased = 1f - (1f - u) * (1f - u) * (1f - u);
        float diameter = Mathf.Lerp(startDiameter, endDiameter, eased);

        float spriteSize = Mathf.Max(0.01f, single.sprite.bounds.size.x);
        float scale = diameter / spriteSize;
        transform.localScale = new Vector3(scale, scale * flatten, 1f);

        single.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * Mathf.Pow(1f - u, 1.4f));
    }

    private void UpdateSplat()
    {
        float u = Mathf.Clamp01(age / duration);
        float grow = 1f - Mathf.Pow(1f - Mathf.Clamp01(age / 0.18f), 3f);
        float diameter = Mathf.Lerp(startDiameter, endDiameter, grow);

        float spriteSize = Mathf.Max(0.01f, single.sprite.bounds.size.x);
        float scale = diameter / spriteSize;
        transform.localScale = new Vector3(scale, scale * flatten, 1f);

        // Se mantiene un rato y se "seca" hacia el final.
        float fade = 1f - Mathf.SmoothStep(0.45f, 1f, u);
        single.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * fade);
    }

    private void UpdateMelt()
    {
        float u = Mathf.Clamp01(age / duration);
        float eased = 1f - (1f - u) * (1f - u);

        float scaleY = Mathf.Lerp(1f, 0.1f, eased);
        float scaleX = Mathf.Lerp(1f, meltWiden, eased);
        transform.localScale = new Vector3(meltBaseScale.x * scaleX, meltBaseScale.y * scaleY, meltBaseScale.z);

        // Se vuelve mas translucido y claro a medida que se derrite.
        float alpha = baseColor.a * (1f - Mathf.SmoothStep(0.35f, 1f, u));
        Color lighter = Color.Lerp(baseColor, new Color(0.75f, 1f, 0.55f, 1f), eased * 0.5f);
        single.color = new Color(lighter.r, lighter.g, lighter.b, alpha);
    }

    // ================================================================= sprites

    private static Sprite DropSprite()
    {
        if (dropSprite != null)
            return dropSprite;

        const int size = 12;
        Color32[] pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / (size * 0.5f);
                float dy = (y - center) / (size * 0.5f);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > 1f)
                    continue;

                // Gris claro abajo (se tiñe mas oscuro) y blanco arriba a la izquierda: un brillo de gota.
                byte shade = (byte)Mathf.RoundToInt(Mathf.Lerp(205f, 255f, Mathf.Clamp01(0.5f + dy * 0.6f - dx * 0.25f)));
                pixels[y * size + x] = new Color32(shade, shade, shade, 255);
            }
        }

        dropSprite = BuildSprite(pixels, size, size, 32f);
        return dropSprite;
    }

    private static Sprite RingSprite()
    {
        if (ringSprite != null)
            return ringSprite;

        const int size = 64;
        Color32[] pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        const float outer = 30f;
        const float thickness = 5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float a = Mathf.Clamp01(1f - Mathf.Abs(d - (outer - thickness * 0.5f)) / (thickness * 0.5f));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        }

        ringSprite = BuildSprite(pixels, size, size, 32f);
        ringSprite.texture.filterMode = FilterMode.Bilinear;
        return ringSprite;
    }

    private static Sprite SplatSprite()
    {
        if (splatSprite != null)
            return splatSprite;

        const int size = 48;
        Color32[] pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx);

                // Borde irregular de mancha: radio que cambia con el angulo.
                float radius = 0.72f + 0.14f * Mathf.Sin(angle * 3f + 0.6f) + 0.09f * Mathf.Sin(angle * 7f + 1.9f)
                               + 0.05f * Mathf.Sin(angle * 12f);
                if (d > radius)
                    continue;

                // Centro mas claro y borde mas oscuro (se tiñe con el color pedido).
                float k = Mathf.Clamp01(d / radius);
                byte shade = (byte)Mathf.RoundToInt(Mathf.Lerp(255f, 190f, k * k));
                pixels[y * size + x] = new Color32(shade, shade, shade, 255);
            }
        }

        splatSprite = BuildSprite(pixels, size, size, 32f);
        return splatSprite;
    }

    private static Sprite BuildSprite(Color32[] pixels, int width, int height, float pixelsPerUnit)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }
}
