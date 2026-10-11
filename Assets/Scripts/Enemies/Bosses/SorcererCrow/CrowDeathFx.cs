using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Efectos visuales del cambio de fase del Cuervo (muerte de la primera fase y resurreccion en la segunda): rafaga de plumas,
/// energia violeta que se junta sobre el cuerpo, ondas expansivas y un destello de pantalla. Son efectos simples y
/// temporales dibujados con sprites (nada de assets nuevos): se animan solos y se destruyen al terminar.
/// </summary>
public class CrowDeathFx : MonoBehaviour
{
    private enum Mode { Burst, Converge, Ring, Flash }

    private const int ParticleSortingOrder = 14;
    private const float BurstDrag = 1.8f;

    private static Sprite ringSprite;

    private Mode mode;
    private float age;
    private float duration;

    // Particulas (Burst / Converge)
    private SpriteRenderer[] sprites;
    private Vector2[] startPos;
    private Vector2[] velocity;
    private float[] delay;
    private float[] life;
    private float[] spin;
    private float[] size;
    private float[] aspect;
    private Color[] tint;
    private float gravity;
    private Vector2 center;
    private float swirl;

    // Onda expansiva
    private SpriteRenderer ringRenderer;
    private Color ringColor;
    private float ringStart;
    private float ringEnd;

    // Destello de pantalla
    private Image flashImage;
    private Color flashColor;
    private float flashPeak;
    private float flashIn;
    private float flashOut;

    // ================================================================= fabricas

    /// <summary>Rafaga de plumas / chispas que sale del 'origin' hacia todos lados, frena y se desvanece.</summary>
    public static void FeatherBurst(Vector2 origin, Transform parent, int count, float minSpeed, float maxSpeed,
        float lifetime, float gravity, Color colorA, Color colorB)
    {
        if (count <= 0)
            return;

        CrowDeathFx fx = Create("CrowFeatherBurst", origin, parent);
        fx.mode = Mode.Burst;
        fx.duration = lifetime + 0.4f;
        fx.center = origin;
        fx.gravity = gravity;
        fx.AllocateParticles(count);

        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float speed = Random.Range(minSpeed, maxSpeed);

            fx.startPos[i] = origin + Random.insideUnitCircle * 0.25f;
            // Un poco sesgadas hacia arriba (salen "despedidas" del cuerpo).
            fx.velocity[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.85f + 0.3f) * speed;
            fx.delay[i] = Random.Range(0f, 0.12f);
            fx.life[i] = lifetime * Random.Range(0.7f, 1f);
            fx.spin[i] = Random.Range(-420f, 420f);
            fx.size[i] = Random.Range(0.1f, 0.2f);
            fx.aspect[i] = Random.Range(1.6f, 2.8f);
            fx.tint[i] = Color.Lerp(colorA, colorB, Random.value);
        }
    }

    /// <summary>Motas de energia que arrancan lejos y se van juntando (en espiral) sobre 'centerPoint'. Terminan de llegar justo al final.</summary>
    public static void Converge(Vector2 centerPoint, Transform parent, int count, float radius, float totalTime,
        Color colorA, Color colorB)
    {
        if (count <= 0)
            return;

        CrowDeathFx fx = Create("CrowEnergyGather", centerPoint, parent);
        fx.mode = Mode.Converge;
        fx.duration = Mathf.Max(0.2f, totalTime);
        fx.center = centerPoint;
        fx.swirl = 1.3f;
        fx.AllocateParticles(count);

        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float r = radius * Random.Range(0.55f, 1f);

            fx.startPos[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
            fx.delay[i] = Random.Range(0f, fx.duration * 0.55f);
            fx.size[i] = Random.Range(0.12f, 0.24f);
            fx.aspect[i] = Random.Range(1.2f, 2.2f);
            fx.tint[i] = Color.Lerp(colorA, colorB, Random.value);
        }
    }

    /// <summary>Onda expansiva (anillo) que crece de 'startDiameter' a 'endDiameter' mientras se desvanece.</summary>
    public static void Ring(Vector2 centerPoint, Transform parent, Color color, float startDiameter, float endDiameter, float time)
    {
        CrowDeathFx fx = Create("CrowShockwave", centerPoint, parent);
        fx.mode = Mode.Ring;
        fx.duration = Mathf.Max(0.1f, time);
        fx.ringStart = startDiameter;
        fx.ringEnd = endDiameter;
        fx.ringColor = color;

        fx.ringRenderer = fx.gameObject.AddComponent<SpriteRenderer>();
        fx.ringRenderer.sprite = RingSprite();
        fx.ringRenderer.sortingOrder = ParticleSortingOrder - 1;
        fx.ringRenderer.color = new Color(color.r, color.g, color.b, 0f);
    }

    /// <summary>Destello de toda la pantalla (sube rapido hasta 'peakAlpha' y baja suave). Usa tiempo real: no se traba si el juego se pausa.</summary>
    public static void ScreenFlash(Color color, float peakAlpha, float inTime, float outTime)
    {
        GameObject go = new GameObject("CrowScreenFlash");

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        GameObject imageObject = new GameObject("Flash");
        imageObject.transform.SetParent(go.transform, false);

        Image image = imageObject.AddComponent<Image>();
        image.raycastTarget = false;
        image.color = new Color(color.r, color.g, color.b, 0f);

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        CrowDeathFx fx = go.AddComponent<CrowDeathFx>();
        fx.mode = Mode.Flash;
        fx.flashImage = image;
        fx.flashColor = color;
        fx.flashPeak = Mathf.Clamp01(peakAlpha);
        fx.flashIn = Mathf.Max(0.01f, inTime);
        fx.flashOut = Mathf.Max(0.01f, outTime);
        fx.duration = fx.flashIn + fx.flashOut;
    }

    private static CrowDeathFx Create(string objectName, Vector2 position, Transform parent)
    {
        GameObject go = new GameObject(objectName);
        go.transform.position = position;
        if (parent != null)
            go.transform.SetParent(parent, true);

        return go.AddComponent<CrowDeathFx>();
    }

    private void AllocateParticles(int count)
    {
        sprites = new SpriteRenderer[count];
        startPos = new Vector2[count];
        velocity = new Vector2[count];
        delay = new float[count];
        life = new float[count];
        spin = new float[count];
        size = new float[count];
        aspect = new float[count];
        tint = new Color[count];

        Sprite pixel = CrowArt.Pixel();
        for (int i = 0; i < count; i++)
        {
            GameObject particle = new GameObject("p" + i);
            particle.transform.SetParent(transform, false);

            SpriteRenderer renderer = particle.AddComponent<SpriteRenderer>();
            renderer.sprite = pixel;
            renderer.sortingOrder = ParticleSortingOrder;
            renderer.enabled = false;
            sprites[i] = renderer;
        }
    }

    // ================================================================= animacion

    private void Update()
    {
        age += mode == Mode.Flash ? Time.unscaledDeltaTime : Time.deltaTime;

        switch (mode)
        {
            case Mode.Burst: UpdateBurst(); break;
            case Mode.Converge: UpdateConverge(); break;
            case Mode.Ring: UpdateRing(); break;
            case Mode.Flash: UpdateFlash(); break;
        }

        if (age >= duration)
            Destroy(gameObject);
    }

    private void UpdateBurst()
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            SpriteRenderer renderer = sprites[i];
            float t = age - delay[i];
            if (t < 0f || t > life[i])
            {
                renderer.enabled = false;
                continue;
            }

            float u = t / life[i];
            float travel = (1f - Mathf.Exp(-BurstDrag * t)) / BurstDrag;
            Vector2 position = startPos[i] + velocity[i] * travel + 0.5f * gravity * t * t * Vector2.down;

            renderer.enabled = true;
            renderer.transform.position = position;
            renderer.transform.rotation = Quaternion.Euler(0f, 0f, spin[i] * t);

            float scale = size[i] * (1f - 0.5f * u);
            renderer.transform.localScale = new Vector3(scale * aspect[i], scale, 1f);

            Color color = tint[i];
            color.a = 1f - u * u;
            renderer.color = color;
        }
    }

    private void UpdateConverge()
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            SpriteRenderer renderer = sprites[i];
            float travelTime = Mathf.Max(0.05f, duration - delay[i]);
            float u = (age - delay[i]) / travelTime;
            if (u < 0f || u >= 1f)
            {
                renderer.enabled = false;
                continue;
            }

            float eased = u * u;
            float angle = swirl * u;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            Vector2 offset = startPos[i] * (1f - eased);
            Vector2 rotated = new Vector2(offset.x * cos - offset.y * sin, offset.x * sin + offset.y * cos);

            renderer.enabled = true;
            renderer.transform.position = center + rotated;

            // Se estira en la direccion en la que viaja (hacia el centro).
            Vector2 toCenter = -rotated;
            float heading = toCenter.sqrMagnitude > 0.0001f ? Mathf.Atan2(toCenter.y, toCenter.x) * Mathf.Rad2Deg : 0f;
            renderer.transform.rotation = Quaternion.Euler(0f, 0f, heading);

            float scale = size[i] * (0.8f + 0.9f * eased);
            renderer.transform.localScale = new Vector3(scale * aspect[i], scale, 1f);

            Color color = tint[i];
            color.a = Mathf.Clamp01(u * 6f) * (1f - Mathf.SmoothStep(0.88f, 1f, u));
            renderer.color = color;
        }
    }

    private void UpdateRing()
    {
        if (ringRenderer == null)
            return;

        float u = Mathf.Clamp01(age / duration);
        float eased = 1f - (1f - u) * (1f - u) * (1f - u);
        float diameter = Mathf.Lerp(ringStart, ringEnd, eased);

        float spriteSize = Mathf.Max(0.01f, ringRenderer.sprite.bounds.size.x);
        float scale = diameter / spriteSize;
        ringRenderer.transform.localScale = new Vector3(scale, scale, 1f);

        float alpha = ringColor.a * Mathf.Pow(1f - u, 1.5f);
        ringRenderer.color = new Color(ringColor.r, ringColor.g, ringColor.b, alpha);
    }

    private void UpdateFlash()
    {
        if (flashImage == null)
            return;

        float alpha;
        if (age < flashIn)
            alpha = flashPeak * (age / flashIn);
        else
            alpha = flashPeak * (1f - Mathf.SmoothStep(0f, 1f, (age - flashIn) / flashOut));

        flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, Mathf.Clamp01(alpha));
    }

    // ================================================================= sprites

    private static Sprite RingSprite()
    {
        if (ringSprite != null)
            return ringSprite;

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        const float outer = 30f;
        const float thickness = 5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                // Anillo con bordes suaves: opaco en el centro del grosor, transparente hacia los costados.
                float a = 1f - Mathf.Abs(d - (outer - thickness * 0.5f)) / (thickness * 0.5f);
                a = Mathf.Clamp01(a);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        // 64 px a 32 ppu = 2 unidades de diametro "natural".
        ringSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 32f);
        return ringSprite;
    }
}
