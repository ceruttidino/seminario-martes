using TMPro;
using UnityEngine;

/// <summary>
/// Cartel flotante de interaccion: una teclita con la letra "F" que aparece cerca del objeto con el que se puede
/// interactuar (pozo para cavar, puerta con candado de la tienda, item de la tienda) cuando el jugador esta lo
/// bastante cerca.
///
/// Cada objeto crea el suyo con InteractPrompt.Create(...) y solo avisa con SetVisible(true/false). El cartel:
///   - usa la fuente del resto del juego (se configura en Resources/InteractPromptSettings),
///   - aparece / desaparece con un fade y "flota" suave mientras esta visible,
///   - se dibuja por encima de todo (jugador, enemigos, items) y queda siempre derecho aunque el objeto este rotado,
///   - se destruye solo si su objeto desaparece.
/// </summary>
public sealed class InteractPrompt : MonoBehaviour
{
    private const string SettingsResource = "InteractPromptSettings";

    private static InteractPromptSettings cachedSettings;
    private static bool settingsLoaded;
    private static Sprite keySprite;

    private Transform anchor;
    private Vector3 worldOffset;
    private InteractPromptSettings settings;
    private GameObject visualRoot;
    private SpriteRenderer keyRenderer;
    private TextMeshPro label;

    private bool wantVisible;
    private float visibility;
    private float phase;

    /// <summary>true mientras el cartel esta visible o desvaneciendose.</summary>
    public bool IsShowing => wantVisible || visibility > 0.001f;

    /// <summary>
    /// Crea un cartel que sigue a 'anchor' (a 'worldOffset' unidades de mundo de distancia). Nace oculto.
    /// El cartel cuelga de la room (si el anchor esta en una) para desaparecer junto con ella.
    /// </summary>
    public static InteractPrompt Create(Transform anchor, Vector3 worldOffset, string key = "F")
    {
        if (anchor == null)
            return null;

        GameObject go = new GameObject("InteractPrompt");

        RoomInstance room = anchor.GetComponentInParent<RoomInstance>();
        if (room != null)
            go.transform.SetParent(room.transform, true);

        InteractPrompt prompt = go.AddComponent<InteractPrompt>();
        prompt.Build(anchor, worldOffset, key);
        return prompt;
    }

    public void SetVisible(bool visible)
    {
        wantVisible = visible;
    }

    public void SetWorldOffset(Vector3 offset)
    {
        worldOffset = offset;
    }

    // ------------------------------------------------------------------ construccion

    private void Build(Transform target, Vector3 offset, string key)
    {
        anchor = target;
        worldOffset = offset;
        settings = LoadSettings();

        float keySize = Mathf.Max(0.1f, settings.keySize);
        int sortingOrder = settings.sortingOrder;

        visualRoot = new GameObject("Visual");
        visualRoot.transform.SetParent(transform, false);

        GameObject keyObject = new GameObject("Key");
        keyObject.transform.SetParent(visualRoot.transform, false);
        keyRenderer = keyObject.AddComponent<SpriteRenderer>();
        keyRenderer.sprite = GetKeySprite();
        keyRenderer.sortingOrder = sortingOrder;
        float spriteWidth = Mathf.Max(0.01f, keyRenderer.sprite.bounds.size.x);
        keyObject.transform.localScale = Vector3.one * (keySize / spriteWidth);

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(visualRoot.transform, false);
        labelObject.transform.localPosition = new Vector3(settings.textOffset.x, settings.textOffset.y, 0f);

        label = labelObject.AddComponent<TextMeshPro>();
        TMP_FontAsset font = settings.font != null ? settings.font : TMP_Settings.defaultFontAsset;
        if (font != null)
            label.font = font;

        label.text = key;
        label.fontSize = settings.fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.color = settings.textColor;
        label.sortingOrder = sortingOrder + 1;
        label.rectTransform.sizeDelta = new Vector2(keySize, keySize);

        visibility = 0f;
        phase = Random.value * 6.28f;
        Apply(0f);
        visualRoot.SetActive(false);
        UpdatePosition(0f);
    }

    private static InteractPromptSettings LoadSettings()
    {
        if (!settingsLoaded)
        {
            settingsLoaded = true;
            cachedSettings = Resources.Load<InteractPromptSettings>(SettingsResource);
        }

        // Si falta el asset se usan los valores por defecto (con la fuente por defecto de TextMeshPro).
        if (cachedSettings == null)
            cachedSettings = ScriptableObject.CreateInstance<InteractPromptSettings>();

        return cachedSettings;
    }

    // ------------------------------------------------------------------ animacion

    private void LateUpdate()
    {
        if (anchor == null)
        {
            Destroy(gameObject);
            return;
        }

        // Tiempo sin escalar: el fade termina aunque el juego este pausado (ahi el objeto lo oculta enseguida).
        float dt = Time.unscaledDeltaTime;
        float speed = wantVisible ? settings.fadeInSpeed : settings.fadeOutSpeed;
        visibility = Mathf.MoveTowards(visibility, wantVisible ? 1f : 0f, dt * Mathf.Max(0.1f, speed));

        bool show = visibility > 0.001f;
        if (visualRoot.activeSelf != show)
            visualRoot.SetActive(show);

        if (!show)
            return;

        phase += dt * settings.bobSpeed;
        UpdatePosition(Mathf.Sin(phase) * settings.bobHeight);
        Apply(visibility);
    }

    private void UpdatePosition(float bob)
    {
        transform.position = anchor.position + worldOffset + Vector3.up * bob;
        transform.rotation = Quaternion.identity;
    }

    private void Apply(float v)
    {
        float clamped = Mathf.Clamp01(v);

        // "Pop": nace un poco mas chico y se agranda con un pequeno rebote al aparecer.
        float pop = Mathf.Lerp(0.6f, 1f, EaseOutBack(clamped));
        visualRoot.transform.localScale = new Vector3(pop, pop, 1f);

        keyRenderer.color = new Color(1f, 1f, 1f, clamped);

        Color c = settings.textColor;
        c.a *= clamped;
        label.color = c;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }

    // ------------------------------------------------------------------ sprite de la tecla

    private static Sprite GetKeySprite()
    {
        if (keySprite != null)
            return keySprite;

        const int size = 16;
        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 outline = new Color32(12, 10, 22, 255);
        Color32 rim = new Color32(238, 220, 160, 255);
        Color32 faceTop = new Color32(66, 68, 98, 255);
        Color32 faceBottom = new Color32(30, 30, 50, 255);

        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color32 color = clear;

                if (InsideRounded(x, y, size, 0, 3))
                    color = outline;
                if (InsideRounded(x, y, size, 1, 3))
                    color = rim;
                if (InsideRounded(x, y, size, 2, 3))
                    color = Color32.Lerp(faceBottom, faceTop, y / (float)(size - 1));

                pixels[y * size + x] = color;
            }
        }

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(pixels);
        texture.Apply();

        keySprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 32f);
        return keySprite;
    }

    /// <summary>Cuadrado con esquinas redondeadas, achicado 'inset' pixeles por cada lado.</summary>
    private static bool InsideRounded(int x, int y, int size, int inset, int radius)
    {
        int min = inset;
        int max = size - 1 - inset;
        if (x < min || x > max || y < min || y > max)
            return false;

        int r = Mathf.Max(0, radius - inset);
        int cx = x < min + r ? min + r : (x > max - r ? max - r : x);
        int cy = y < min + r ? min + r : (y > max - r ? max - r : y);
        int dx = x - cx;
        int dy = y - cy;
        return dx * dx + dy * dy <= r * r + r;
    }
}
