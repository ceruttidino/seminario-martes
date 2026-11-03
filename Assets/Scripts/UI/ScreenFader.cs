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

    private void SetAlpha(float a)
    {
        if (fadeImage == null)
            return;

        Color c = fadeImage.color;
        c.a = Mathf.Clamp01(a);
        fadeImage.color = c;
    }
}
