using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChallengeIntroOverlay : MonoBehaviour
{
    private const string TitleResource = "UI/ProtectTheContainer";

    public static IEnumerator Play(MonoBehaviour _)
    {
        GameObject go = new GameObject("ChallengeIntroOverlay");
        ChallengeIntroOverlay overlay = go.AddComponent<ChallengeIntroOverlay>();
        yield return overlay.Run();
        if (go != null)
            Destroy(go);
    }

    private IEnumerator Run()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 520;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        Image title = CreateImage("Title");
        Sprite titleSprite = Resources.Load<Sprite>(TitleResource);
        if (titleSprite == null)
        {
            Texture2D texture = Resources.Load<Texture2D>(TitleResource);
            if (texture != null)
            {
                titleSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }
        }

        if (titleSprite != null)
        {
            title.sprite = titleSprite;
            title.preserveAspect = true;
            title.rectTransform.anchorMin = new Vector2(0.04f, 0.08f);
            title.rectTransform.anchorMax = new Vector2(0.96f, 0.92f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;
        }
        else
        {
            title.enabled = false;
        }

        TextMeshProUGUI countdown = CreateLabel("Countdown");
        countdown.text = "";
        countdown.alpha = 0f;

        yield return FadeGraphic(title, 0f, 1f, 0.35f);
        yield return new WaitForSeconds(1.35f);
        yield return FadeGraphic(title, 1f, 0f, 0.3f);
        title.enabled = false;

        for (int n = 5; n >= 1; n--)
        {
            countdown.text = n.ToString();
            yield return FadeGraphic(countdown, 0f, 1f, 0.12f);
            yield return new WaitForSeconds(0.42f);
            yield return FadeGraphic(countdown, 1f, 0f, 0.12f);
        }
    }

    private Image CreateImage(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        Image image = go.AddComponent<Image>();
        image.raycastTarget = false;
        image.color = new Color(1f, 1f, 1f, 0f);
        return image;
    }

    private TextMeshProUGUI CreateLabel(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(800f, 400f);

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 220f;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(0.92f, 0.62f, 0.16f, 0f);
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }

    private static IEnumerator FadeGraphic(Graphic graphic, float from, float to, float duration)
    {
        if (graphic == null)
            yield break;

        Color color = graphic.color;
        color.a = from;
        graphic.color = color;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            graphic.color = color;
            yield return null;
        }

        color.a = to;
        graphic.color = color;
    }
}
