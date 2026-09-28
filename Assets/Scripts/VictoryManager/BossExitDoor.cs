using System.Collections;
using UnityEngine;

public class BossExitDoor : MonoBehaviour
{
    private const string SheetResource = "Effects/BossCaveDoor";
    private const int FrameCount = 5;
    private const float PixelsPerUnit = 40f;
    private const float OpenFrameRate = 8f;
    private const float OpenHoldBeforeInteract = 0.2f;

    private static Sprite[] cachedFrames;

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
        Sprite closed = GetFrame(0);
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            if (closed != null)
            {
                spriteRenderer.sprite = closed;
                FitCollider(closed);
            }
        }

        if (doorCollider != null)
            doorCollider.enabled = false;
    }

    private void ShowOpenAndEnable()
    {
        opening = false;
        isOpen = true;

        Sprite open = GetFrame(FrameCount - 1);
        if (spriteRenderer != null && open != null)
        {
            spriteRenderer.sprite = open;
            FitCollider(open);
        }

        if (doorCollider != null)
            doorCollider.enabled = true;
    }

    private IEnumerator OpenRoutine()
    {
        Sprite[] frames = GetFrames();
        if (frames != null && frames.Length > 0 && spriteRenderer != null)
        {
            spriteRenderer.sprite = frames[0];

            float frameDuration = 1f / Mathf.Max(1f, OpenFrameRate);
            for (int i = 1; i < frames.Length; i++)
            {
                yield return new WaitForSeconds(frameDuration);
                if (frames[i] != null)
                    spriteRenderer.sprite = frames[i];
            }

            Sprite last = frames[frames.Length - 1];
            if (last != null)
                spriteRenderer.sprite = last;

            yield return new WaitForSeconds(OpenHoldBeforeInteract);
        }

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

        int count = FrameCount;
        int frameWidth = Mathf.Max(1, texture.width / count);
        cachedFrames = new Sprite[count];

        for (int i = 0; i < count; i++)
        {
            int x = i * frameWidth;
            int width = i == count - 1 ? texture.width - x : frameWidth;
            cachedFrames[i] = Sprite.Create(
                texture,
                new Rect(x, 0f, width, texture.height),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
        }

        return cachedFrames;
    }
}
