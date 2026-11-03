using UnityEngine;

/// <summary>
/// Explosion del Erizo dibujada por codigo (mismo enfoque que SlimeArt / GooFx / CrowDeathFx): todo son sprites pixel-art
/// generados en runtime, sin assets nuevos. Es SOLO visual: el dano, el radio y el empuje los resuelve
/// ExplosiveHedgehog.Explode(); este efecto se dibuja justo con ese radio.
///
/// Capas (de abajo hacia arriba): humo, onda expansiva (el anillo termina exactamente en el radio de dano), bola de
/// fuego en bandas (blanco, amarillo, naranja, rojo), llamaradas que salen disparadas, fogonazo, brasas que saltan y
/// caen, y puas del erizo que salen volando. Ademas deja una mancha de quemado en el piso (GooFx.Splat).
///
/// Los efectos se animan solos y se destruyen al terminar. No cuentan como enemigos ni tocan la jugabilidad.
/// </summary>
public sealed class BlastFx : MonoBehaviour
{
    public const int BaseSortingOrder = 12;

    private enum Kind { Flash, Fire, FireBit, Ring, Ember, Quill, Smoke }

    private struct Piece
    {
        public Kind kind;
        public SpriteRenderer renderer;
        public float unit;          // ancho del sprite en unidades (para escalar a un diametro)
        public Vector2 origin;
        public Vector2 direction;
        public float speed;         // velocidad inicial (o distancia maxima en las llamaradas)
        public float delay;
        public float life;
        public float size;          // diametro inicial (o largo / lado en puas y brasas)
        public float sizeEnd;
        public float spin;          // grados por segundo
        public float angle;         // angulo inicial en grados
        public float lift;          // impulso vertical (brasas) / deriva hacia arriba (humo)
        public Color colorA;
        public Color colorB;
    }

    private const float EmberGravity = 11f;
    private const float PixelsPerUnitBlob = 48f;

    // El aro del sprite de la onda ocupa 152 de sus 160 pixeles: se compensa para que termine justo en el radio.
    private const float RingFill = 152f / 160f;

    private static Sprite softDiscSprite;
    private static Sprite ringSprite;
    private static Sprite emberSprite;
    private static Sprite quillSprite;
    private static readonly Sprite[] fireSprites = new Sprite[2];
    private static readonly Sprite[] smokeSprites = new Sprite[2];
    private static Material unlitMaterial;

    private Piece[] pieces = new Piece[96];
    private int count;
    private float age;
    private float totalTime;

    /// <summary>Crea la explosion en 'center'. 'radius' es el radio de dano (la onda llega justo hasta ahi).</summary>
    public static BlastFx Explosion(Vector2 center, float radius, Transform parent)
    {
        radius = Mathf.Max(0.5f, radius);

        GameObject go = new GameObject("HedgehogBlast");
        go.transform.position = center;
        if (parent != null)
            go.transform.SetParent(parent, true);

        BlastFx fx = go.AddComponent<BlastFx>();
        fx.Build(center, radius);

        // Mancha de quemado que queda en el piso (se seca despues de unos segundos).
        GooFx.Splat(center, parent, new Color(0.09f, 0.065f, 0.055f, 0.6f), radius * 1.3f, 5f, 0.82f, 1);
        return fx;
    }

    // ================================================================= armado

    private void Build(Vector2 center, float radius)
    {
        float k = radius / 2.5f;   // todo se penso para el radio de 2.5 del erizo; se escala si cambia

        // ---- Humo (debajo de todo) ----
        for (int i = 0; i < 9; i++)
        {
            Vector2 dir = RandomDirection();
            Piece p = NewPiece(Kind.Smoke, SmokeSprite(i), BaseSortingOrder + 1, center + dir * Random.Range(0.05f, 0.45f) * radius * 0.5f);
            p.direction = dir;
            p.speed = Random.Range(0.12f, 0.42f) * radius;
            p.delay = Random.Range(0.05f, 0.24f);
            p.life = Random.Range(0.85f, 1.3f);
            p.size = Random.Range(0.55f, 0.85f) * k;
            p.sizeEnd = Random.Range(1.25f, 1.95f) * k;
            p.spin = Random.Range(-35f, 35f);
            p.angle = Random.Range(0f, 360f);
            p.lift = Random.Range(0.3f, 0.75f) * k;
            p.colorA = new Color(0.20f, 0.18f, 0.17f, 0.62f);
            p.colorB = new Color(0.55f, 0.52f, 0.50f, 0.62f);
            Store(p);
        }

        // ---- Onda expansiva: termina justo en el radio de dano ----
        {
            Piece wave = NewPiece(Kind.Ring, RingSprite(), BaseSortingOrder, center);
            wave.life = 0.42f;
            wave.size = 0.35f * k;
            wave.sizeEnd = radius * 2f / RingFill;
            wave.colorA = new Color(1f, 0.88f, 0.58f, 0.9f);
            wave.colorB = new Color(1f, 0.52f, 0.22f, 0.9f);
            Store(wave);

            Piece inner = NewPiece(Kind.Ring, RingSprite(), BaseSortingOrder, center);
            inner.delay = 0.07f;
            inner.life = 0.42f;
            inner.size = 0.2f * k;
            inner.sizeEnd = radius * 1.55f / RingFill;
            inner.colorA = new Color(1f, 0.95f, 0.75f, 0.55f);
            inner.colorB = new Color(1f, 0.6f, 0.3f, 0.55f);
            Store(inner);
        }

        // ---- Bola de fuego (dos capas con distinta forma) ----
        {
            Piece outer = NewPiece(Kind.Fire, FireSprite(0), BaseSortingOrder + 3, center);
            outer.life = 0.64f;
            outer.size = radius * 1.28f;
            outer.angle = Random.Range(0f, 360f);
            outer.spin = Random.Range(-28f, 28f);
            outer.colorA = Color.white;
            outer.colorB = new Color(0.95f, 0.5f, 0.38f, 1f);
            Store(outer);

            Piece core = NewPiece(Kind.Fire, FireSprite(1), BaseSortingOrder + 4, center);
            core.delay = 0.03f;
            core.life = 0.5f;
            core.size = radius * 0.82f;
            core.angle = Random.Range(0f, 360f);
            core.spin = Random.Range(-45f, 45f);
            core.colorA = Color.white;
            core.colorB = new Color(1f, 0.72f, 0.45f, 1f);
            Store(core);
        }

        // ---- Llamaradas que salen disparadas ----
        for (int i = 0; i < 9; i++)
        {
            float a = (i + Random.Range(-0.3f, 0.3f)) / 9f * Mathf.PI * 2f;
            Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));

            Piece p = NewPiece(Kind.FireBit, FireSprite(i), BaseSortingOrder + 3, center);
            p.direction = dir;
            p.speed = radius * Random.Range(0.42f, 0.78f);
            p.delay = Random.Range(0f, 0.05f);
            p.life = Random.Range(0.3f, 0.48f);
            p.size = Random.Range(0.55f, 0.9f) * k;
            p.sizeEnd = Random.Range(0.1f, 0.25f) * k;
            p.angle = Random.Range(0f, 360f);
            p.spin = Random.Range(-90f, 90f);
            p.colorA = Color.white;
            p.colorB = new Color(0.9f, 0.4f, 0.3f, 1f);
            Store(p);
        }

        // ---- Fogonazo ----
        {
            Piece flash = NewPiece(Kind.Flash, SoftDiscSprite(), BaseSortingOrder + 5, center);
            flash.life = 0.17f;
            flash.size = radius * 1.15f;
            flash.colorA = new Color(1f, 1f, 0.92f, 1f);
            flash.colorB = new Color(1f, 0.8f, 0.4f, 1f);
            Store(flash);
        }

        // ---- Brasas: saltan, caen y se apagan ----
        for (int i = 0; i < 24; i++)
        {
            Vector2 dir = RandomDirection();
            Piece p = NewPiece(Kind.Ember, EmberSprite(), BaseSortingOrder + 7, center + dir * Random.Range(0f, 0.25f));
            p.direction = dir;
            p.speed = Random.Range(2.2f, 6.8f) * k;
            p.lift = Random.Range(1.6f, 4.4f);
            p.delay = Random.Range(0f, 0.06f);
            p.life = Random.Range(0.5f, 0.95f);
            p.size = Random.Range(0.8f, 1.8f);
            p.colorA = Color.Lerp(new Color(1f, 0.92f, 0.45f, 1f), new Color(1f, 0.6f, 0.18f, 1f), Random.value);
            p.colorB = new Color(0.55f, 0.16f, 0.1f, 1f);
            Store(p);
        }

        // ---- Puas del erizo que salen volando ----
        for (int i = 0; i < 12; i++)
        {
            float a = (i + Random.Range(-0.35f, 0.35f)) / 12f * Mathf.PI * 2f;
            Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));

            Piece p = NewPiece(Kind.Quill, QuillSprite(), BaseSortingOrder + 7, center + dir * 0.12f);
            p.direction = dir;
            p.speed = Random.Range(5.2f, 9.5f) * k;
            p.delay = Random.Range(0f, 0.04f);
            p.life = Random.Range(0.5f, 0.72f);
            p.size = Random.Range(0.85f, 1.25f);
            p.angle = a * Mathf.Rad2Deg;
            p.colorA = Color.white;
            p.colorB = new Color(1f, 0.85f, 0.7f, 1f);
            Store(p);
        }

        totalTime = 0f;
        for (int i = 0; i < count; i++)
            totalTime = Mathf.Max(totalTime, pieces[i].delay + pieces[i].life);
        totalTime += 0.05f;

        UpdatePieces();
    }

    private Piece NewPiece(Kind kind, Sprite sprite, int sortingOrder, Vector2 origin)
    {
        GameObject go = new GameObject(kind.ToString());
        go.transform.SetParent(transform, false);
        go.transform.position = origin;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        renderer.enabled = false;

        Material material = UnlitMaterial();
        if (material != null)
            renderer.sharedMaterial = material;

        return new Piece
        {
            kind = kind,
            renderer = renderer,
            unit = Mathf.Max(0.01f, sprite.bounds.size.x),
            origin = origin,
            direction = Vector2.up,
            colorA = Color.white,
            colorB = Color.white
        };
    }

    private void Store(Piece piece)
    {
        if (count >= pieces.Length)
        {
            Destroy(piece.renderer.gameObject);
            return;
        }

        pieces[count++] = piece;
    }

    private static Vector2 RandomDirection()
    {
        float a = Random.value * Mathf.PI * 2f;
        return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
    }

    // ================================================================= animacion

    private void Update()
    {
        age += Time.deltaTime;
        UpdatePieces();

        if (age >= totalTime)
            Destroy(gameObject);
    }

    private void UpdatePieces()
    {
        for (int i = 0; i < count; i++)
        {
            ref Piece p = ref pieces[i];
            if (p.renderer == null)
                continue;

            float t = age - p.delay;
            if (t < 0f || t >= p.life)
            {
                p.renderer.enabled = false;
                continue;
            }

            float u = t / p.life;
            p.renderer.enabled = true;

            switch (p.kind)
            {
                case Kind.Flash: UpdateFlash(ref p, u); break;
                case Kind.Fire: UpdateFire(ref p, t, u); break;
                case Kind.FireBit: UpdateFireBit(ref p, u); break;
                case Kind.Ring: UpdateRing(ref p, u); break;
                case Kind.Ember: UpdateEmber(ref p, t, u); break;
                case Kind.Quill: UpdateQuill(ref p, t, u); break;
                case Kind.Smoke: UpdateSmoke(ref p, u); break;
            }
        }
    }

    private static void UpdateFlash(ref Piece p, float u)
    {
        float e = EaseOut(Mathf.Clamp01(u * 1.7f));
        SetDiameter(ref p, Mathf.Lerp(p.size * 0.45f, p.size, e));

        Color c = Color.Lerp(p.colorA, p.colorB, u);
        c.a = (1f - u) * (1f - u);
        p.renderer.color = c;
    }

    private static void UpdateFire(ref Piece p, float t, float u)
    {
        float grow = EaseOut(Mathf.Clamp01(t / 0.16f));
        float shrink = 1f - 0.24f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.2f) / Mathf.Max(0.05f, p.life - 0.2f)));
        SetDiameter(ref p, p.size * Mathf.Lerp(0.22f, 1f, grow) * shrink);
        p.renderer.transform.rotation = Quaternion.Euler(0f, 0f, p.angle + p.spin * t);

        Color c = Color.Lerp(p.colorA, p.colorB, Smooth(0.1f, 1f, u));
        c.a = 1f - Smooth(0.38f, 1f, u);
        p.renderer.color = c;
    }

    private static void UpdateFireBit(ref Piece p, float u)
    {
        float travel = EaseOut(u) * p.speed;
        p.renderer.transform.position = p.origin + p.direction * travel;
        p.renderer.transform.rotation = Quaternion.Euler(0f, 0f, p.angle + p.spin * u);
        SetDiameter(ref p, Mathf.Lerp(p.size, p.sizeEnd, u));

        Color c = Color.Lerp(p.colorA, p.colorB, u);
        c.a = 1f - Smooth(0.4f, 1f, u);
        p.renderer.color = c;
    }

    private static void UpdateRing(ref Piece p, float u)
    {
        SetDiameter(ref p, Mathf.Lerp(p.size, p.sizeEnd, EaseOut(u)));

        Color c = Color.Lerp(p.colorA, p.colorB, u);
        c.a = p.colorA.a * Mathf.Pow(1f - u, 1.25f);
        p.renderer.color = c;
    }

    private static void UpdateEmber(ref Piece p, float t, float u)
    {
        const float drag = 2.4f;
        float travel = (1f - Mathf.Exp(-drag * t)) / drag;
        float height = Mathf.Max(0f, p.lift * t - 0.5f * EmberGravity * t * t);

        Vector2 position = p.origin + p.direction * (p.speed * travel);
        position.y += height;
        p.renderer.transform.position = position;

        float side = p.size * Mathf.Lerp(1f, 0.55f, u);
        p.renderer.transform.localScale = new Vector3(side, side, 1f);

        Color c = Color.Lerp(p.colorA, p.colorB, Smooth(0.1f, 0.9f, u));
        c.a = 1f - Smooth(0.55f, 1f, u);
        p.renderer.color = c;
    }

    private static void UpdateQuill(ref Piece p, float t, float u)
    {
        const float drag = 3.4f;
        float travel = (1f - Mathf.Exp(-drag * t)) / drag;

        p.renderer.transform.position = p.origin + p.direction * (p.speed * travel);
        p.renderer.transform.rotation = Quaternion.Euler(0f, 0f, p.angle);
        float s = p.size * Mathf.Lerp(1f, 0.8f, u);
        p.renderer.transform.localScale = new Vector3(s, s, 1f);

        Color c = Color.Lerp(p.colorA, p.colorB, u);
        c.a = 1f - Smooth(0.5f, 1f, u);
        p.renderer.color = c;
    }

    private static void UpdateSmoke(ref Piece p, float u)
    {
        float e = EaseOut(u);
        Vector2 position = p.origin + p.direction * (p.speed * e);
        position.y += p.lift * e;
        p.renderer.transform.position = position;
        p.renderer.transform.rotation = Quaternion.Euler(0f, 0f, p.angle + p.spin * u);
        SetDiameter(ref p, Mathf.Lerp(p.size, p.sizeEnd, e));

        Color c = Color.Lerp(p.colorA, p.colorB, e);
        float rise = Mathf.Clamp01(u / 0.15f);
        float fall = Mathf.Pow(1f - Mathf.Clamp01((u - 0.15f) / 0.85f), 1.3f);
        c.a = p.colorA.a * (u < 0.15f ? rise : fall);
        p.renderer.color = c;
    }

    private static void SetDiameter(ref Piece p, float diameter)
    {
        float scale = Mathf.Max(0.001f, diameter) / p.unit;
        p.renderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    /// <summary>Como el smoothstep de los shaders: 0 antes de 'edge0', 1 despues de 'edge1' y suave en el medio.</summary>
    private static float Smooth(float edge0, float edge1, float x)
    {
        float k = Mathf.Clamp01((x - edge0) / Mathf.Max(0.0001f, edge1 - edge0));
        return k * k * (3f - 2f * k);
    }

    private static float EaseOut(float u)
    {
        float inv = 1f - Mathf.Clamp01(u);
        return 1f - inv * inv * inv;
    }

    // ================================================================= sprites generados

    private static Material UnlitMaterial()
    {
        if (unlitMaterial != null)
            return unlitMaterial;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            return null;

        unlitMaterial = new Material(shader) { name = "BlastFx_Unlit" };
        return unlitMaterial;
    }

    private static float Hash(int x, int y, int salt)
    {
        float v = Mathf.Sin(x * 12.9898f + y * 78.233f + salt * 37.719f) * 43758.5453f;
        return v - Mathf.Floor(v);
    }

    private static Sprite FireSprite(int variant)
    {
        int index = Mathf.Abs(variant) % fireSprites.Length;
        if (fireSprites[index] != null)
            return fireSprites[index];

        const int size = 96;
        Color32[] pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        float seed = index * 1.9f + 0.4f;

        Color32 core = new Color32(255, 246, 205, 255);
        Color32 yellow = new Color32(255, 214, 92, 255);
        Color32 orange = new Color32(252, 146, 36, 255);
        Color32 red = new Color32(216, 78, 26, 255);
        Color32 rim = new Color32(122, 36, 26, 255);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Atan2(dy, dx);

                // Borde de llamarada: radio que cambia con el angulo.
                float edge = 0.80f + 0.095f * Mathf.Sin(3f * a + seed) + 0.06f * Mathf.Sin(5f * a + 2.1f * seed)
                             + 0.04f * Mathf.Sin(8f * a + 3.3f * seed) + 0.025f * Mathf.Sin(13f * a + 0.7f * seed);
                if (d > edge)
                    continue;

                // Un poco de ruido para que las bandas no sean aros perfectos.
                float k = Mathf.Clamp01(d / edge + (Hash(x, y, index) - 0.5f) * 0.1f);
                Color32 color = k < 0.26f ? core : k < 0.48f ? yellow : k < 0.70f ? orange : k < 0.88f ? red : rim;
                pixels[y * size + x] = color;
            }
        }

        fireSprites[index] = BuildSprite(pixels, size, size, PixelsPerUnitBlob, FilterMode.Point);
        return fireSprites[index];
    }

    private static Sprite SmokeSprite(int variant)
    {
        int index = Mathf.Abs(variant) % smokeSprites.Length;
        if (smokeSprites[index] != null)
            return smokeSprites[index];

        const int size = 64;
        Color32[] pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        float seed = index * 2.7f + 1.1f;

        Color32 dark = new Color32(70, 64, 62, 255);
        Color32 mid = new Color32(104, 97, 93, 255);
        Color32 light = new Color32(142, 134, 128, 255);
        Color32 high = new Color32(172, 164, 156, 255);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Atan2(dy, dx);

                float edge = 0.74f + 0.1f * Mathf.Sin(3f * a + seed) + 0.07f * Mathf.Sin(5f * a + 1.7f * seed)
                             + 0.04f * Mathf.Sin(9f * a + 0.5f * seed);
                if (d > edge)
                    continue;

                // Luz desde arriba a la izquierda: lo claro se corre hacia ahi.
                float lx = (x - center) / center + 0.22f;
                float ly = (y - center) / center - 0.22f;
                float k = Mathf.Clamp01(Mathf.Sqrt(lx * lx + ly * ly) / edge + (Hash(x, y, index + 7) - 0.5f) * 0.08f);
                pixels[y * size + x] = k < 0.3f ? high : k < 0.55f ? light : k < 0.82f ? mid : dark;
            }
        }

        smokeSprites[index] = BuildSprite(pixels, size, size, PixelsPerUnitBlob, FilterMode.Point);
        return smokeSprites[index];
    }

    private static Sprite SoftDiscSprite()
    {
        if (softDiscSprite != null)
            return softDiscSprite;

        const int size = 64;
        Color32[] pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d >= 1f)
                    continue;

                float a = (1f - d) * (1f - d);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        }

        softDiscSprite = BuildSprite(pixels, size, size, 32f, FilterMode.Bilinear);
        return softDiscSprite;
    }

    private static Sprite RingSprite()
    {
        if (ringSprite != null)
            return ringSprite;

        const int size = 160;
        Color32[] pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        const float radius = 76f;
        const float halfThickness = 2.4f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float a = Mathf.Clamp01(1f - (Mathf.Abs(d - radius) - halfThickness * 0.5f) / (halfThickness * 0.9f));
                if (a <= 0f)
                    continue;

                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        }

        ringSprite = BuildSprite(pixels, size, size, 80f, FilterMode.Bilinear);
        return ringSprite;
    }

    private static Sprite EmberSprite()
    {
        if (emberSprite != null)
            return emberSprite;

        const int size = 4;
        Color32[] pixels = new Color32[size * size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(255, 255, 255, 255);

        emberSprite = BuildSprite(pixels, size, size, PixelsPerUnitBlob, FilterMode.Point);
        return emberSprite;
    }

    private static Sprite QuillSprite()
    {
        if (quillSprite != null)
            return quillSprite;

        // Pua del erizo: base marron, punta naranja. Apunta hacia +X.
        const int width = 22;
        const int height = 6;
        Color32[] pixels = new Color32[width * height];
        Color32 outline = new Color32(58, 38, 28, 255);
        Color32 bodyA = new Color32(132, 104, 82, 255);
        Color32 bodyB = new Color32(104, 80, 62, 255);
        Color32 tip = new Color32(255, 150, 40, 255);
        Color32 tipHot = new Color32(255, 214, 92, 255);

        for (int x = 0; x < width; x++)
        {
            float half = Mathf.Lerp(2.9f, 0.5f, x / (float)(width - 1));
            for (int y = 0; y < height; y++)
            {
                float off = Mathf.Abs(y - 2.5f);
                if (off > half)
                    continue;

                Color32 color;
                if (x >= 19) color = tipHot;
                else if (x >= 15) color = tip;
                else if (off > half - 0.95f) color = outline;
                else color = (x / 3) % 2 == 0 ? bodyA : bodyB;

                pixels[y * width + x] = color;
            }
        }

        quillSprite = BuildSprite(pixels, width, height, PixelsPerUnitBlob, FilterMode.Point);
        return quillSprite;
    }

    private static Sprite BuildSprite(Color32[] pixels, int width, int height, float pixelsPerUnit, FilterMode filter)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = filter;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }
}
