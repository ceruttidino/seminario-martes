using System.Collections;
using UnityEngine;

/// <summary>
/// Pose de "agarre de buff": cuando el jugador consigue un objeto (buff / mejora) el mapache hace la animacion Raccoon_TakeBuff,
/// se queda QUIETO mientras dura (PlayerMovement.TryPlayTakeBuff bloquea movimiento, ataques y dash) y el objeto que acaba de
/// recoger aparece flotando sobre el, como si lo mostrara con los brazos en alto.
/// Se agrega solo al jugador la primera vez que consigue un objeto (ver PlayerUpgradeManager.CollectUpgrade).
/// </summary>
public class PlayerBuffPose : MonoBehaviour
{
    // Alto del objeto mostrado (unidades de mundo) y altura sobre el centro del mapache (por encima de las manos levantadas).
    private const float IconHeight = 0.9f;
    private const float HoverHeight = 1.2f;

    // La animacion levanta los brazos recien alrededor de 0.3 s (frame 4 de la hoja): el objeto aparece justo ahi.
    private const float AppearDelay = 0.3f;
    private const float AppearTime = 0.22f;
    private const float FadeOutTime = 0.25f;
    private const float MinVisibleTime = 1.0f;
    private const float MaxVisibleTime = 3.5f;

    // Si el dash esta en curso (o el juego pausado) cuando se recoge el objeto, se reintenta arrancar la pose este tiempo.
    private const float StartRetryTime = 1f;

    private static int activeIcons;
    private static Sprite glowSprite;

    private PlayerMovement movement;
    private SpriteRenderer playerRenderer;

    /// <summary>Arranca la pose y muestra el objeto (icono) sobre el mapache.</summary>
    public void Play(Sprite icon, Color glowColor)
    {
        if (movement == null)
            movement = GetComponent<PlayerMovement>();

        if (playerRenderer == null)
            playerRenderer = GetComponent<SpriteRenderer>();
        if (playerRenderer == null)
            playerRenderer = GetComponentInChildren<SpriteRenderer>();

        if (movement == null)
            return;

        StartCoroutine(Routine(icon, glowColor));
    }

    private IEnumerator Routine(Sprite icon, Color glowColor)
    {
        // 1) Arranca la pose (si ya esta posando por otro objeto simplemente se suma el icono).
        float waited = 0f;
        while (!movement.IsPosing && waited < StartRetryTime)
        {
            if (movement.TryPlayTakeBuff())
                break;

            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!movement.IsPosing)
            yield break;

        if (icon == null)
            yield break;

        // 2) El objeto flotando sobre el mapache.
        int slot = activeIcons++;
        float xOffset = IconOffsetX(slot);

        GameObject root = new GameObject("BuffPoseIcon");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = new Vector3(xOffset, HoverHeight, 0f);
        root.transform.localScale = Vector3.zero;

        int order = (playerRenderer != null ? playerRenderer.sortingOrder : 5) + 6;

        float iconScale = IconHeight / Mathf.Max(0.01f, Mathf.Max(icon.bounds.size.x, icon.bounds.size.y));

        GameObject glowObject = new GameObject("Glow");
        glowObject.transform.SetParent(root.transform, false);
        glowObject.transform.localScale = Vector3.one * (IconHeight * 1.9f / Mathf.Max(0.01f, GlowSprite().bounds.size.x));
        SpriteRenderer glow = glowObject.AddComponent<SpriteRenderer>();
        glow.sprite = GlowSprite();
        glow.sortingOrder = order - 1;
        Color glowBase = glowColor.a > 0.01f && glowColor.maxColorComponent > 0.05f
            ? new Color(glowColor.r, glowColor.g, glowColor.b, 1f)
            : new Color(1f, 0.85f, 0.35f, 1f);

        GameObject iconObject = new GameObject("Icon");
        iconObject.transform.SetParent(root.transform, false);
        iconObject.transform.localScale = Vector3.one * iconScale;
        SpriteRenderer iconRenderer = iconObject.AddComponent<SpriteRenderer>();
        iconRenderer.sprite = icon;
        iconRenderer.sortingOrder = order;

        float t = 0f;
        float fadeT = -1f;

        while (root != null)
        {
            t += Time.deltaTime;

            bool poseOver = !movement.IsPosing && t >= MinVisibleTime;
            if ((poseOver || t >= MaxVisibleTime) && fadeT < 0f)
                fadeT = 0f;

            float appear = Mathf.Clamp01((t - AppearDelay) / AppearTime);
            float pop = appear <= 0f ? 0f : EaseOutBack(appear);
            float alpha = 1f;

            if (fadeT >= 0f)
            {
                fadeT += Time.deltaTime;
                alpha = 1f - Mathf.Clamp01(fadeT / FadeOutTime);
                if (alpha <= 0f)
                    break;
            }

            float bob = Mathf.Sin(t * 5.5f) * 0.05f;
            root.transform.localPosition = new Vector3(xOffset, HoverHeight + bob, 0f);
            root.transform.localScale = Vector3.one * pop;

            iconRenderer.color = new Color(1f, 1f, 1f, alpha);
            float glowPulse = 0.55f + Mathf.Sin(t * 7f) * 0.15f;
            glow.color = new Color(glowBase.r, glowBase.g, glowBase.b, glowPulse * alpha * appear);

            yield return null;
        }

        activeIcons = Mathf.Max(0, activeIcons - 1);
        if (root != null)
            Destroy(root);
    }

    private void OnDisable()
    {
        // Si el jugador se desactiva a mitad de la pose (cambio de sala, muerte) no queda contado el icono.
        StopAllCoroutines();
        activeIcons = 0;

        Transform leftover = transform.Find("BuffPoseIcon");
        if (leftover != null)
            Destroy(leftover.gameObject);
    }

    private static float IconOffsetX(int slot)
    {
        if (slot <= 0)
            return 0f;

        // Si recoge varios objetos seguidos se acomodan en fila: 0, +, -, ++, --...
        int ring = (slot + 1) / 2;
        return (slot % 2 == 1 ? 1f : -1f) * ring * 0.95f;
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float p = x - 1f;
        return 1f + c3 * p * p * p + c1 * p * p;
    }

    private static Sprite GlowSprite()
    {
        if (glowSprite != null)
            return glowSprite;

        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / (size * 0.5f);
                float a = Mathf.Clamp01(1f - d);
                a = a * a;
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        glowSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 32f);
        return glowSprite;
    }
}
