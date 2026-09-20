using System.Collections;
using UnityEngine;

public class BloodPool : MonoBehaviour
{
    private const float Lifetime = 3f;
    private const float FadeDuration = 1f;

    private static Sprite[] cachedFrames;
    private static bool loadAttempted;

    [SerializeField] private SpriteRenderer spriteRenderer;

    public static void Spawn(Vector3 worldPosition)
    {
        Sprite[] frames = GetFrames();
        if (frames == null || frames.Length == 0)
            return;

        GameObject go = new GameObject("BloodPool");
        go.transform.position = worldPosition + Vector3.back * 0.01f;
        go.transform.localScale = Vector3.one * 1.35f;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 1;
        renderer.color = new Color(0.95f, 0.07f, 0.07f, 1f);

        SpriteSequence sequence = go.AddComponent<SpriteSequence>();
        sequence.Play(frames, 8f, true);

        BloodPool pool = go.AddComponent<BloodPool>();
        pool.spriteRenderer = renderer;
        pool.StartCoroutine(pool.FadeAndDestroy());
    }

    private IEnumerator FadeAndDestroy()
    {
        float hold = Mathf.Max(0.1f, Lifetime - FadeDuration);
        yield return new WaitForSeconds(hold);

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        float elapsed = 0f;
        Color start = spriteRenderer != null ? spriteRenderer.color : Color.white;
        while (elapsed < FadeDuration)
        {
            elapsed += Time.deltaTime;
            if (spriteRenderer != null)
            {
                Color color = start;
                color.a = Mathf.Lerp(start.a, 0f, elapsed / FadeDuration);
                spriteRenderer.color = color;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private static Sprite[] GetFrames()
    {
        if (loadAttempted)
            return cachedFrames;

        loadAttempted = true;
        cachedFrames = LoadSheet("Effects/BloodPool-Sheet");
        if (cachedFrames == null || cachedFrames.Length < 2)
            cachedFrames = LoadSheet("Effects/Acido-Sheet");

        return cachedFrames;
    }

    private static Sprite[] LoadSheet(string resourcePath)
    {
        Sprite[] sliced = Resources.LoadAll<Sprite>(resourcePath);
        if (sliced != null && sliced.Length > 1)
        {
            System.Array.Sort(sliced, (a, b) => string.CompareOrdinal(a.name, b.name));
            return sliced;
        }

        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null && sliced != null && sliced.Length == 1 && sliced[0] != null)
            texture = sliced[0].texture;

        if (texture == null)
            return null;

        const int count = 7;
        int stride = Mathf.Max(1, texture.width / count);
        int frameHeight = texture.height;
        Sprite[] frames = new Sprite[count];

        for (int i = 0; i < count; i++)
        {
            var rect = new Rect(i * stride, 0f, stride, frameHeight);
            frames[i] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect);
        }

        return frames;
    }
}
