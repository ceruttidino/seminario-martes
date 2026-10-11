using System.Collections;
using UnityEngine;

public class BossExitDoor : MonoBehaviour
{
    private const string ClosedResource = "Effects/BossCaveDoorClosed";
    private const string OpenResource = "Effects/BossCaveDoorOpen";
    private const string SheetResource = "Effects/BossCaveDoorAnim";
    private const int FrameCount = 6;
    private const int FrameWidth = 181;
    private const int FrameHeight = 90;
    private const float PixelsPerUnit = 40f;
    private const float OpenFrameRate = 14f;
    private const float OpenHoldBeforeInteract = 0.15f;

    private static Sprite cachedClosed;
    private static Sprite cachedOpen;
    private static Sprite[] cachedFrames;
    private static bool closedLoadAttempted;
    private static bool openLoadAttempted;

    private SpriteRenderer spriteRenderer;
    private Collider2D doorCollider;
    private bool isOpen;
    private bool hasTriggered;
    private bool opening;

    // Emblema del final: la puerta que aparece al vencer al ultimo jefe lleva un logo "extraño" (GDD) que avisa que
    // esa es la salida definitiva (pasar por ahi dispara la pantalla de victoria).
    private const float EmblemWorldSize = 1.8f;
    private static Sprite cachedEmblem;
    private Transform emblem;
    private SpriteRenderer emblemRenderer;
    private Coroutine emblemRoutine;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        spriteRenderer.sortingOrder = 3;
        spriteRenderer.drawMode = SpriteDrawMode.Simple;

        doorCollider = GetComponent<Collider2D>();
        if (doorCollider == null)
            doorCollider = gameObject.AddComponent<BoxCollider2D>();

        doorCollider.isTrigger = true;
        doorCollider.enabled = false;

        ShowClosed();
    }

    public void Place(Vector3 worldPosition, DoorDirection wall, Vector3 roomCenter)
    {
        Vector3 inward = roomCenter - worldPosition;
        if (inward.sqrMagnitude < 0.01f)
            inward = -DirectionVector(wall);

        transform.position = worldPosition + inward.normalized * 0.45f;
        transform.rotation = Quaternion.Euler(0f, 0f, WallRotation(wall));
        float scale = wall == DoorDirection.Left || wall == DoorDirection.Right ? 0.72f : 1.1f;
        transform.localScale = Vector3.one * scale;

        isOpen = false;
        hasTriggered = false;
        opening = false;
        if (doorCollider != null)
            doorCollider.enabled = false;

        ShowClosed();
    }

    public void OpenAfterBoss()
    {
        if (isOpen || opening)
            return;

        if (!gameObject.activeInHierarchy)
        {
            ShowOpenAndEnable();
            return;
        }

        opening = true;
        if (doorCollider != null)
            doorCollider.enabled = false;

        StartCoroutine(OpenRoutine());
    }

    public void SetOpenedImmediate()
    {
        ShowOpenAndEnable();
    }

    private void ShowClosed()
    {
        ApplySprite(GetClosedSprite());
        if (doorCollider != null)
            doorCollider.enabled = false;

        HideEmblem();
    }

    private void ShowOpenAndEnable()
    {
        opening = false;
        isOpen = true;
        ApplySprite(GetOpenSprite());
        if (doorCollider != null)
            doorCollider.enabled = true;

        ShowEmblem();
    }

    // ------------------------------------------------------------------ emblema de la puerta final

    private static bool IsFinalDoor()
    {
        return DungeonManager.Instance != null && DungeonManager.Instance.IsFinalFloor;
    }

    private void ShowEmblem()
    {
        if (!IsFinalDoor())
            return;

        if (emblem == null)
        {
            GameObject go = new GameObject("FinalDoorEmblem");
            go.transform.SetParent(transform, false);
            emblem = go.transform;
            emblemRenderer = go.AddComponent<SpriteRenderer>();
            emblemRenderer.sprite = GetEmblemSprite();
        }

        if (spriteRenderer != null && emblemRenderer != null)
            emblemRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;

        emblem.gameObject.SetActive(true);

        if (emblemRoutine != null)
            StopCoroutine(emblemRoutine);
        emblemRoutine = StartCoroutine(EmblemRoutine());
    }

    private void HideEmblem()
    {
        if (emblemRoutine != null)
        {
            StopCoroutine(emblemRoutine);
            emblemRoutine = null;
        }

        if (emblem != null)
            emblem.gameObject.SetActive(false);
    }

    /// <summary>Aparece con un fade y despues "late" con un brillo suave. Siempre queda derecho (la puerta puede estar rotada por la pared).</summary>
    private IEnumerator EmblemRoutine()
    {
        float appear = 0f;
        while (emblem != null)
        {
            appear = Mathf.Min(1f, appear + Time.unscaledDeltaTime / 0.8f);

            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 2.4f) * 0.07f;
            float parentScale = Mathf.Max(0.01f, transform.lossyScale.x);
            float natural = emblemRenderer != null && emblemRenderer.sprite != null
                ? Mathf.Max(0.01f, emblemRenderer.sprite.bounds.size.x)
                : 1f;
            float size = (EmblemWorldSize / natural) * pulse * Mathf.SmoothStep(0.6f, 1f, appear) / parentScale;

            emblem.position = transform.position;
            emblem.rotation = Quaternion.identity;
            emblem.localScale = new Vector3(size, size, 1f);

            if (emblemRenderer != null)
            {
                float glow = 0.82f + Mathf.Sin(Time.unscaledTime * 2.4f) * 0.18f;
                emblemRenderer.color = new Color(1f, 1f, 1f, glow * appear);
            }

            yield return null;
        }
    }

    private static Sprite GetEmblemSprite()
    {
        if (cachedEmblem != null)
            return cachedEmblem;

        const int size = 48;
        Color32[] pixels = new Color32[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

        Color32 gold = new Color32(255, 214, 92, 255);
        Color32 goldDark = new Color32(190, 120, 40, 255);
        Color32 disc = new Color32(34, 10, 58, 235);
        Color32 eyeWhite = new Color32(248, 240, 222, 255);
        Color32 iris = new Color32(236, 70, 232, 255);
        Color32 outline = new Color32(10, 4, 18, 255);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x, y) - center;
                float d = p.magnitude;
                float angle = Mathf.Atan2(p.y, p.x);
                Color32 color = new Color32(0, 0, 0, 0);

                // Anillo exterior dorado con "dientes" (8 rayos).
                float ray = Mathf.Abs(Mathf.Cos(angle * 4f));
                if (d <= 23.2f && d >= 19.2f)
                    color = ((x + y) & 3) < 2 ? gold : goldDark;
                else if (d > 23.2f && d <= 23.8f)
                    color = outline;
                else if (d > 19.2f - 0.1f && d < 19.2f)
                    color = outline;
                else if (d < 19.2f)
                    color = disc;

                // Rayos hacia afuera: pequeñas puntas doradas sobre el anillo.
                if (d > 21f && d <= 23.8f && ray > 0.93f)
                    color = gold;

                // Ojo (almendra) en el centro.
                float eyeHalfWidth = 14f;
                if (d < 19.2f && Mathf.Abs(p.x) <= eyeHalfWidth)
                {
                    float k = p.x / eyeHalfWidth;
                    float halfHeight = 8.5f * (1f - k * k);
                    if (Mathf.Abs(p.y) <= halfHeight)
                        color = eyeWhite;
                    else if (Mathf.Abs(p.y) <= halfHeight + 1.2f)
                        color = outline;
                }

                // Iris y pupila vertical.
                if (d < 19.2f)
                {
                    if (p.magnitude <= 6.2f && Mathf.Abs(p.y) <= 8f * (1f - (p.x / 14f) * (p.x / 14f)))
                        color = iris;
                    if (Mathf.Abs(p.x) <= 1.6f && Mathf.Abs(p.y) <= 6f)
                        color = outline;
                    if (Mathf.Abs(p.x + 2.4f) <= 0.9f && Mathf.Abs(p.y - 2.4f) <= 0.9f)
                        color = eyeWhite;
                }

                pixels[y * size + x] = color;
            }
        }

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(pixels);
        texture.Apply();

        // 48 px a 32 ppu = 1.5 unidades de lado "natural" (el tamaño final en mundo se ajusta por codigo).
        cachedEmblem = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 32f);
        return cachedEmblem;
    }

    private void ApplySprite(Sprite sprite)
    {
        if (spriteRenderer == null || sprite == null)
            return;

        spriteRenderer.color = Color.white;
        spriteRenderer.sprite = sprite;
        FitCollider(sprite);
    }

    private IEnumerator OpenRoutine()
    {
        Sprite[] frames = GetFrames();
        if (frames != null && frames.Length > 0 && spriteRenderer != null)
        {
            float frameDuration = 1f / Mathf.Max(1f, OpenFrameRate);
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null)
                {
                    spriteRenderer.color = Color.white;
                    spriteRenderer.sprite = frames[i];
                }
                yield return new WaitForSeconds(frameDuration);
            }
        }

        ApplySprite(GetOpenSprite());
        yield return new WaitForSeconds(OpenHoldBeforeInteract);
        ShowOpenAndEnable();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isOpen || hasTriggered || opening)
            return;

        if (!other.CompareTag("Player"))
            return;

        hasTriggered = true;

        VictoryManager victoryManager = FindFirstObjectByType<VictoryManager>();
        if (victoryManager != null)
            victoryManager.TriggerVictory();
        else
            DungeonManager.Instance?.StartNextFloor();
    }

    private void FitCollider(Sprite sprite)
    {
        if (doorCollider is BoxCollider2D box && sprite != null)
            box.size = sprite.bounds.size * 0.72f;
    }

    private static float WallRotation(DoorDirection wall)
    {
        switch (wall)
        {
            case DoorDirection.Right: return -90f;
            case DoorDirection.Down: return 180f;
            case DoorDirection.Left: return 90f;
            default: return 0f;
        }
    }

    private static Vector3 DirectionVector(DoorDirection wall)
    {
        switch (wall)
        {
            case DoorDirection.Up: return Vector3.up;
            case DoorDirection.Down: return Vector3.down;
            case DoorDirection.Left: return Vector3.left;
            default: return Vector3.right;
        }
    }

    private static Sprite GetClosedSprite()
    {
        if (!closedLoadAttempted)
        {
            closedLoadAttempted = true;
            cachedClosed = LoadSingleSprite(ClosedResource);
        }

        if (cachedClosed != null)
            return cachedClosed;

        return GetFrame(0);
    }

    private static Sprite GetOpenSprite()
    {
        if (!openLoadAttempted)
        {
            openLoadAttempted = true;
            cachedOpen = LoadSingleSprite(OpenResource);
        }

        if (cachedOpen != null)
            return cachedOpen;

        Sprite[] frames = GetFrames();
        if (frames != null && frames.Length > 0)
            return frames[frames.Length - 1];

        return null;
    }

    private static Sprite LoadSingleSprite(string resourcePath)
    {
        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
            return sprite;

        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
            return null;

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            PixelsPerUnit,
            0,
            SpriteMeshType.FullRect);
    }

    private static Sprite GetFrame(int index)
    {
        Sprite[] frames = GetFrames();
        if (frames == null || frames.Length == 0)
            return null;

        index = Mathf.Clamp(index, 0, frames.Length - 1);
        return frames[index];
    }

    private static Sprite[] GetFrames()
    {
        if (cachedFrames != null && cachedFrames.Length > 1)
            return cachedFrames;

        Sprite[] sliced = Resources.LoadAll<Sprite>(SheetResource);
        if (sliced != null && sliced.Length > 1)
        {
            System.Array.Sort(sliced, (a, b) => string.CompareOrdinal(a.name, b.name));
            cachedFrames = sliced;
            return cachedFrames;
        }

        Texture2D texture = Resources.Load<Texture2D>(SheetResource);
        if (texture == null && sliced != null && sliced.Length == 1 && sliced[0] != null)
            texture = sliced[0].texture;

        if (texture == null)
            return cachedFrames;

        int count = Mathf.Max(1, texture.width / FrameWidth);
        if (count < 2)
            count = FrameCount;

        cachedFrames = new Sprite[count];

        for (int i = 0; i < count; i++)
        {
            int x = i * FrameWidth;
            int width = Mathf.Min(FrameWidth, texture.width - x);
            int height = texture.height > 0 ? texture.height : FrameHeight;
            cachedFrames[i] = Sprite.Create(
                texture,
                new Rect(x, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit,
                0,
                SpriteMeshType.Tight);
        }

        return cachedFrames;
    }
}
