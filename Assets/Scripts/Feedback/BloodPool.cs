using System.Collections;
using UnityEngine;

public class BloodPool : MonoBehaviour
{
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private float fadeDuration = 0.8f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    public static void Spawn(Vector3 worldPosition)
    {
        Sprite[] frames = LoadFrames();
        if (frames == null || frames.Length == 0)
            return;

        GameObject go = new GameObject("BloodPool");
        go.transform.position = worldPosition;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = -1;
        renderer.color = Color.white;

        SpriteSequence sequence = go.AddComponent<SpriteSequence>();
        sequence.Play(frames, 8f, true);

        BloodPool pool = go.AddComponent<BloodPool>();
        pool.spriteRenderer = renderer;
        pool.StartCoroutine(pool.FadeAndDestroy());
    }

    private IEnumerator FadeAndDestroy()
    {
        float hold = Mathf.Max(0.1f, lifetime - fadeDuration);
        yield return new WaitForSeconds(hold);

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        float elapsed = 0f;
        Color start = spriteRenderer != null ? spriteRenderer.color : Color.white;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            if (spriteRenderer != null)
            {
                Color color = start;
                color.a = Mathf.Lerp(start.a, 0f, elapsed / fadeDuration);
                spriteRenderer.color = color;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private static Sprite[] LoadFrames()
    {
        Sprite[] sliced = Resources.LoadAll<Sprite>("Effects/BloodPool-Sheet");
        if (sliced != null && sliced.Length > 1)
        {
            System.Array.Sort(sliced, (a, b) => string.CompareOrdinal(a.name, b.name));
            return sliced;
        }

        Texture2D texture = Resources.Load<Texture2D>("Effects/BloodPool-Sheet");
        if (texture == null)
            return sliced;

        const int count = 7;
        int stride = Mathf.Max(1, texture.width / count);
        Sprite[] frames = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            var rect = new Rect(i * stride + 1f, 7f, 62f, 47f);
            frames[i] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f);
        }

        return frames;
    }
}
