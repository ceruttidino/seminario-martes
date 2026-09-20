using System.Collections;
using UnityEngine;

public class TrashBurst : MonoBehaviour
{
    public static void ExplodeAllAround(Vector3 origin, Sprite[] sprites, int count)
    {
        Spawn(origin, sprites, count, fullCircle: true, Vector2.up, 360f);
    }

    public static void ExplodeToward(Vector3 origin, Vector2 direction, Sprite[] sprites, int count, float coneAngle = 70f)
    {
        if (direction.sqrMagnitude < 0.01f)
            direction = Vector2.up;

        Spawn(origin, sprites, count, fullCircle: false, direction.normalized, coneAngle);
    }

    private static void Spawn(Vector3 origin, Sprite[] sprites, int count, bool fullCircle, Vector2 direction, float coneAngle)
    {
        if (sprites == null || sprites.Length == 0 || count <= 0)
            return;

        for (int i = 0; i < count; i++)
        {
            Sprite sprite = sprites[Random.Range(0, sprites.Length)];
            if (sprite == null) continue;

            Vector2 dir;
            if (fullCircle)
            {
                float angle = (360f / count) * i + Random.Range(-16f, 16f);
                dir = Quaternion.Euler(0f, 0f, angle) * Vector2.up;
            }
            else
            {
                float t = count == 1 ? 0f : (i / (float)(count - 1)) * 2f - 1f;
                float angle = Vector2.SignedAngle(Vector2.up, direction) + t * (coneAngle * 0.5f) + Random.Range(-6f, 6f);
                dir = Quaternion.Euler(0f, 0f, angle) * Vector2.up;
            }

            float speed = Random.Range(2.4f, 5.1f);
            GameObject piece = new GameObject("TrashDebris");
            piece.transform.position = origin;
            piece.transform.localScale = Vector3.one * Random.Range(0.22f, 0.42f);
            piece.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 6;
            renderer.color = Color.white;

            TrashBurst burst = piece.AddComponent<TrashBurst>();
            burst.StartCoroutine(burst.Fly(dir * speed, Random.Range(0.45f, 0.75f)));
        }
    }

    private IEnumerator Fly(Vector2 velocity, float life)
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        float spin = Random.Range(-540f, 540f);
        float elapsed = 0f;
        Vector2 vel = velocity;

        while (elapsed < life)
        {
            elapsed += Time.deltaTime;
            vel += Vector2.down * 6.5f * Time.deltaTime;
            transform.position += (Vector3)(vel * Time.deltaTime);
            transform.Rotate(0f, 0f, spin * Time.deltaTime);

            if (renderer != null && elapsed > life * 0.55f)
            {
                Color color = renderer.color;
                color.a = Mathf.Lerp(1f, 0f, (elapsed - life * 0.55f) / (life * 0.45f));
                renderer.color = color;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
