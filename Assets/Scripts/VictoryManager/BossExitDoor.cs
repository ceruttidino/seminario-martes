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
    }

    private void ShowOpenAndEnable()
    {
        opening = false;
        isOpen = true;
        ApplySprite(GetOpenSprite());
        if (doorCollider != null)
            doorCollider.enabled = true;
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
