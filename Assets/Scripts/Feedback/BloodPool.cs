using System.Collections;
using UnityEngine;

public class BloodPool : MonoBehaviour
{
    private const float Lifetime = 3f;
    private const float FadeInDuration = 0.6f;
    private const float FadeOutDuration = 0.6f;
    private const string SpritePath = "Effects/Sangre";

    private static readonly Color Tint = new Color(0.95f, 0.07f, 0.07f);

    private static Sprite cachedSprite;
    private static bool loadAttempted;

    [SerializeField] private SpriteRenderer spriteRenderer;

    public static void Spawn(Vector3 worldPosition, Transform roomParent = null)
    {
        Sprite sprite = GetSprite();
        if (sprite == null)
            return;

        GameObject go = new GameObject("BloodPool");
        go.transform.position = worldPosition + Vector3.back * 0.01f;
        go.transform.localScale = Vector3.one * 1.35f;

        if (roomParent == null && DungeonManager.Instance != null && DungeonManager.Instance.CurrentRoom != null)
            roomParent = DungeonManager.Instance.CurrentRoom.transform;
        if (roomParent != null)
            go.transform.SetParent(roomParent, true);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 0;
        renderer.color = new Color(Tint.r, Tint.g, Tint.b, 0f); 

        BloodPool pool = go.AddComponent<BloodPool>();
        pool.spriteRenderer = renderer;
        pool.StartCoroutine(pool.FadeInHoldFadeOut());
    }

    private IEnumerator FadeInHoldFadeOut()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        float hold = Mathf.Max(0f, Lifetime - FadeInDuration - FadeOutDuration);

        yield return FadeAlpha(0f, 1f, FadeInDuration);
        yield return new WaitForSeconds(hold);
        yield return FadeAlpha(1f, 0f, FadeOutDuration);

        Destroy(gameObject);
    }

    private IEnumerator FadeAlpha(float from, float to, float duration)
    {
        Color color = Tint;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (spriteRenderer != null)
            {
                color.a = Mathf.Lerp(from, to, elapsed / duration);
                spriteRenderer.color = color;
            }

            yield return null;
        }

        if (spriteRenderer != null)
        {
            color.a = to;
            spriteRenderer.color = color;
        }
    }

    private static Sprite GetSprite()
    {
        if (loadAttempted)
            return cachedSprite;

        loadAttempted = true;
        cachedSprite = Resources.Load<Sprite>(SpritePath);

        if (cachedSprite == null)
            Debug.LogWarning($"BloodPool: no encontré el sprite en Resources/{SpritePath}");

        return cachedSprite;
    }
}

// COSAS DE ANIMACION DE SPRITES ACA ABAJO

        // Sprite[] frames = GetFrames();
        // if (frames == null || frames.Length == 0)
        //     return;


        //  SpriteSequence sequence = go.AddComponent<SpriteSequence>();
        //  sequence.Play(frames, 8f, true);



    // private static Sprite[] GetFrames()
    // {
    //     if (loadAttempted)
    //         return cachedFrames;

    //     loadAttempted = true;
    //     cachedFrames = LoadSheet("Effects/BloodPool-Sheet");
    //     if (cachedFrames == null || cachedFrames.Length < 2)
    //         cachedFrames = LoadSheet("Effects/Acido-Sheet");

    //     return cachedFrames;
    // }




    //private static Sprite[] LoadSheet(string resourcePath)
    //{
    //    Sprite[] sliced = Resources.LoadAll<Sprite>(resourcePath);
    //    if (sliced != null && sliced.Length > 1)
    //    {
    //        System.Array.Sort(sliced, (a, b) => string.CompareOrdinal(a.name, b.name));
    //        return sliced;
    //    }

    //    Texture2D texture = Resources.Load<Texture2D>(resourcePath);
    //    if (texture == null && sliced != null && sliced.Length == 1 && sliced[0] != null)
    //        texture = sliced[0].texture;

    //    if (texture == null)
    //        return null;

    //    const int count = 7;
    //    int stride = Mathf.Max(1, texture.width / count);
    //    int frameHeight = texture.height;
    //    Sprite[] frames = new Sprite[count];

    //    for (int i = 0; i < count; i++)
    //    {
    //        var rect = new Rect(i * stride, 0f, stride, frameHeight);
    //        frames[i] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect);
    //    }

    //    return frames;
    //}

