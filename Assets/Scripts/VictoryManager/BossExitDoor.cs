using System.Collections;
using UnityEngine;

public class BossExitDoor : MonoBehaviour
{
    private const string SheetResource = "Effects/BossCaveDoor";
    private const int FrameCount = 5;
    private const float PixelsPerUnit = 40f;
    private const float OpenFrameRate = 8f;

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

        doorCollider = GetComponent<Collider2D>();
        if (doorCollider == null)
            doorCollider = gameObject.AddComponent<BoxCollider2D>();

        doorCollider.isTrigger = true;
        doorCollider.enabled = false;

        Sprite closed = GetFrame(0);
        if (closed != null)
        {
            spriteRenderer.sprite = closed;
            FitCollider(closed);
        }
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

        Sprite closed = GetFrame(0);
        if (spriteRenderer != null && closed != null)
        {
            spriteRenderer.sprite = closed;
            spriteRenderer.color = Color.white;
        }

        isOpen = false;
        hasTriggered = false;
        opening = false;
        if (doorCollider != null)
            doorCollider.enabled = false;
    }

    public void OpenAfterBoss()
    {
        if (isOpen || opening)
            return;

        if (!gameObject.activeInHierarchy)
        {
            SetOpenedImmediate();
            return;
        }

        opening = true;
        StartCoroutine(OpenRoutine());
    }

    public void SetOpenedImmediate()
    {
        opening = false;
        isOpen = true;
        Sprite open = GetFrame(FrameCount - 1);
        if (spriteRenderer != null && open != null)
            spriteRenderer.sprite = open;

        if (doorCollider != null)
            doorCollider.enabled = true;
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
                    spriteRenderer.sprite = frames[i];
                yield return new WaitForSeconds(frameDuration);
            }
        }

        SetOpenedImmediate();
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
        if (cachedFrames != null && cachedFrames.Length > 0)
            return cachedFrames;

        Texture2D texture = Resources.Load<Texture2D>(SheetResource);
        if (texture == null)
        {
            Sprite single = Resources.Load<Sprite>(SheetResource);
            if (single != null)
                texture = single.texture;
        }

        if (texture == null)
            return null;

        int count = Mathf.Max(1, FrameCount);
        float stride = texture.width / (float)count;
        cachedFrames = new Sprite[count];

        for (int i = 0; i < count; i++)
        {
            cachedFrames[i] = Sprite.Create(
                texture,
                new Rect(i * stride, 0f, stride, texture.height),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
        }

        return cachedFrames;
    }
}
