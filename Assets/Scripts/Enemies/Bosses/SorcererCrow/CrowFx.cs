using System.Collections;
using UnityEngine;

/// <summary>
/// Efectos visuales simples y temporales del Cuervo: portales en el piso y barras de aviso.
/// Se animan solos (aparecen, se quedan y desaparecen) y se destruyen al terminar.
/// </summary>
public class CrowFx : MonoBehaviour
{
    private enum Mode { ScaleIn, FadeBar }

    private Mode mode;
    private SpriteRenderer sr;
    private Vector3 targetScale;
    private Color targetColor;
    private float openTime;
    private float holdTime;
    private float closeTime;

    /// <summary>Portal ovalado en el piso: se abre, se queda y se cierra.</summary>
    public static GameObject Portal(Vector2 position, Transform parent, float openTime, float holdTime, float closeTime, float widthUnits)
    {
        Sprite sprite = CrowArt.Portal();

        GameObject go = new GameObject("CrowPortalFx");
        go.transform.position = position;
        if (parent != null)
            go.transform.SetParent(parent, true);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 1;

        float spriteWidth = Mathf.Max(0.01f, sprite.bounds.size.x);
        float scale = widthUnits / spriteWidth;

        CrowFx fx = go.AddComponent<CrowFx>();
        fx.Setup(Mode.ScaleIn, renderer, new Vector3(scale, scale, 1f), Color.white, openTime, holdTime, closeTime);
        return go;
    }

    /// <summary>Barra horizontal translucida que avisa por donde va a pasar un ataque.</summary>
    public static GameObject Bar(Vector2 center, float width, float height, Color color, float fadeIn, float hold, float fadeOut, Transform parent)
    {
        GameObject go = new GameObject("CrowWarningBar");
        go.transform.position = center;
        go.transform.localScale = new Vector3(width, height, 1f);
        if (parent != null)
            go.transform.SetParent(parent, true);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = CrowArt.Pixel();
        renderer.sortingOrder = 0;
        renderer.color = new Color(color.r, color.g, color.b, 0f);

        CrowFx fx = go.AddComponent<CrowFx>();
        fx.Setup(Mode.FadeBar, renderer, go.transform.localScale, color, fadeIn, hold, fadeOut);
        return go;
    }

    private void Setup(Mode newMode, SpriteRenderer renderer, Vector3 scale, Color color, float open, float hold, float close)
    {
        mode = newMode;
        sr = renderer;
        targetScale = scale;
        targetColor = color;
        openTime = Mathf.Max(0.01f, open);
        holdTime = Mathf.Max(0f, hold);
        closeTime = Mathf.Max(0.01f, close);

        Apply(0f);
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        float t = 0f;
        while (t < openTime)
        {
            t += Time.deltaTime;
            Apply(Mathf.Clamp01(t / openTime));
            yield return null;
        }

        Apply(1f);

        float held = 0f;
        while (held < holdTime)
        {
            held += Time.deltaTime;
            yield return null;
        }

        t = 0f;
        while (t < closeTime)
        {
            t += Time.deltaTime;
            Apply(1f - Mathf.Clamp01(t / closeTime));
            yield return null;
        }

        Destroy(gameObject);
    }

    private void Apply(float k)
    {
        if (mode == Mode.ScaleIn)
        {
            transform.localScale = new Vector3(targetScale.x * k, targetScale.y * k, 1f);
        }
        else if (sr != null)
        {
            sr.color = new Color(targetColor.r, targetColor.g, targetColor.b, targetColor.a * k);
        }
    }
}
