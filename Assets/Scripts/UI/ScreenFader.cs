using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeSpeed = 8f;

    public IEnumerator FadeOut()
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.unscaledDeltaTime * fadeSpeed;
            SetAlpha(t);
            yield return null;
        }

        SetAlpha(1f);
    }

    public IEnumerator FadeIn()
    {
        float t = 1;
        while (t > 0)
        {
            t -= Time.unscaledDeltaTime * fadeSpeed;
            SetAlpha(t);
            yield return null;
        }

        SetAlpha(0f);
    }

    /// <summary>Fundido lento a negro. Lo usa el cambio de piso, no las puertas entre rooms.</summary>
    public IEnumerator FadeOutOver(float seconds)
    {
        yield return FadeTowards(1f, seconds);
    }

    /// <summary>Fundido lento desde negro. Lo usa el cambio de piso.</summary>
    public IEnumerator FadeInOver(float seconds)
    {
        yield return FadeTowards(0f, seconds);
    }

    /// <summary>Se queda en negro un momento, para que el cambio de piso se sienta como un cambio de escena.</summary>
    public IEnumerator HoldBlack(float seconds)
    {
        SetAlpha(1f);

        float t = 0f;
        float duration = Mathf.Max(0f, seconds);
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator FadeTowards(float targetAlpha, float seconds)
    {
        float duration = Mathf.Max(0.05f, seconds);
        float from = fadeImage != null ? fadeImage.color.a : (targetAlpha > 0.5f ? 0f : 1f);
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            SetAlpha(Mathf.Lerp(from, targetAlpha, u));
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    private void SetAlpha(float a)
    {
        if (fadeImage == null)
            return;

        Color c = fadeImage.color;
        c.a = Mathf.Clamp01(a);
        fadeImage.color = c;
    }
}
