using UnityEngine;

/// <summary>
/// Charco de sangre que deja un enemigo al morir: aparece con fade, se queda un rato y desaparece con fade
/// (3 segundos en total, segun el GDD).
///
/// El ciclo de vida se calcula con la hora de spawn (Update), NO con una corrutina: las corrutinas se
/// cortan cuando se desactiva la room (al irse a otra habitacion) y nunca retomaban, asi que el charco
/// quedaba congelado para siempre. Ademas, si la room se desactiva el charco se destruye, de modo que
/// nunca queda ni "viaja" entre habitaciones.
/// </summary>
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

    private float spawnTime;

    public static void Spawn(Vector3 worldPosition, Transform roomParent = null)
    {
        Sprite sprite = GetSprite();
        if (sprite == null)
            return;

        if (roomParent == null && DungeonManager.Instance != null && DungeonManager.Instance.CurrentRoom != null)
            roomParent = DungeonManager.Instance.CurrentRoom.transform;

        // Si la room ya no esta activa no tiene sentido dejar un charco (nadie lo veria y no se limpiaria).
        if (roomParent != null && !roomParent.gameObject.activeInHierarchy)
            return;

        GameObject go = new GameObject("BloodPool");
        go.transform.position = worldPosition + Vector3.back * 0.01f;
        go.transform.localScale = Vector3.one * 1.35f;

        if (roomParent != null)
            go.transform.SetParent(roomParent, true);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 0;
        renderer.color = new Color(Tint.r, Tint.g, Tint.b, 0f);

        BloodPool pool = go.AddComponent<BloodPool>();
        pool.spriteRenderer = renderer;
        pool.spawnTime = Time.time;
    }

    private void Awake()
    {
        if (spawnTime <= 0f)
            spawnTime = Time.time;
    }

    private void Update()
    {
        float age = Time.time - spawnTime;

        if (age >= Lifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            return;

        float alpha = 1f;
        if (age < FadeInDuration)
            alpha = age / FadeInDuration;
        else if (age > Lifetime - FadeOutDuration)
            alpha = (Lifetime - age) / FadeOutDuration;

        Color color = Tint;
        color.a = Mathf.Clamp01(alpha);
        spriteRenderer.color = color;
    }

    private void OnDisable()
    {
        // La room se desactivo (el player se fue): el charco ya no tiene razon de existir.
        Destroy(gameObject);
    }

    private static Sprite GetSprite()
    {
        if (loadAttempted && cachedSprite != null)
            return cachedSprite;

        loadAttempted = true;
        cachedSprite = Resources.Load<Sprite>(SpritePath);

        if (cachedSprite == null)
            Debug.LogWarning($"BloodPool: no encontre el sprite en Resources/{SpritePath}");

        return cachedSprite;
    }
}
