using UnityEngine;
using UnityEngine.UI;

// NUEVO (archivo completo)
// Viñeta: los BORDES de la pantalla se tiñen de un color y se desvanecen. La usa PlayerHurtFeedback cuando
// el player recibe daño: es la señal más clara de "me pegaron" aunque el mapache esté tapado por enemigos.
//
// NO hay que ponerlo en ninguna escena ni armar ningún Canvas: se crea solo la primera vez que se usa
// (aparece en la Hierarchy, dentro de "DontDestroyOnLoad", como "[ScreenVignetteFlash]").
// La textura del degradé se genera por código, así que tampoco necesita assets.
public class ScreenVignetteFlash : MonoBehaviour
{
    private const int CanvasSortingOrder = 400; // por encima del HUD, por debajo de un menú de pausa con orden mayor
    private const int TextureSize = 128;

    private static ScreenVignetteFlash instance;
    private static bool quitting;

    private RawImage image;
    private Color color;
    private float peakAlpha;
    private float duration;
    private float timer = -1f;

    // Por si tenés desactivado el "Domain Reload": limpia lo estático en cada Play
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        quitting = false;
    }

    /// <summary>Tiñe los bordes de la pantalla con 'color' a 'alpha' y lo desvanece en 'time' segundos (tiempo real).</summary>
    public static void Play(Color tint, float alpha, float time)
    {
        ScreenVignetteFlash vignette = GetOrCreate();
        if (vignette != null)
            vignette.Begin(tint, alpha, time);
    }

    private static ScreenVignetteFlash GetOrCreate()
    {
        if (instance != null) return instance;
        if (quitting) return null;

        GameObject root = new GameObject("[ScreenVignetteFlash]");
        DontDestroyOnLoad(root);

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CanvasSortingOrder;

        GameObject imageObject = new GameObject("Vignette");
        imageObject.transform.SetParent(root.transform, false);

        RectTransform rect = imageObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        RawImage raw = imageObject.AddComponent<RawImage>();
        raw.texture = BuildTexture();
        raw.raycastTarget = false; // no bloquea clicks de la UI
        raw.color = Color.clear;
        raw.enabled = false;

        instance = root.AddComponent<ScreenVignetteFlash>();
        instance.image = raw;
        return instance;
    }

    private void Begin(Color tint, float alpha, float time)
    {
        // Si ya hay una más fuerte en curso, no la achica
        float current = timer >= 0f ? CurrentAlpha() : 0f;

        color = tint;
        peakAlpha = Mathf.Max(Mathf.Clamp01(alpha), current);
        duration = Mathf.Max(0.05f, time);
        timer = 0f;

        image.enabled = true;
        Apply();
    }

    private void Update()
    {
        if (timer < 0f) return;

        // Tiempo real: tiene que verse durante el hit stop y aunque se abra la pausa
        timer += Time.unscaledDeltaTime;

        if (timer >= duration)
        {
            timer = -1f;
            image.color = Color.clear;
            image.enabled = false; // apagada no dibuja nada (no cuesta rendimiento)
            return;
        }

        Apply();
    }

    private float CurrentAlpha()
    {
        float u = Mathf.Clamp01(timer / duration);
        return peakAlpha * (1f - u) * (1f - u); // arranca fuerte y se va rápido
    }

    private void Apply()
    {
        image.color = new Color(color.r, color.g, color.b, CurrentAlpha());
    }

    private void OnApplicationQuit()
    {
        quitting = true;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // Degradé blanco: transparente en el centro y opaco en los bordes. El color lo pone RawImage.color.
    private static Texture2D BuildTexture()
    {
        Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[TextureSize * TextureSize];
        float center = (TextureSize - 1) * 0.5f;

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float d = Mathf.Sqrt(dx * dx + dy * dy); // 0 en el centro, 1 en los bordes, 1.41 en las esquinas

                float a = Mathf.Clamp01((d - 0.55f) / 0.6f);
                a = Mathf.Pow(a, 1.4f);

                pixels[y * TextureSize + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }
}
