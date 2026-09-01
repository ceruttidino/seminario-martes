using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class RoomDoor : MonoBehaviour
{
    private const int BossFrameWidth = 138;
    private const int BossFrameHeight = 92;
    private const float BossPixelsPerUnit = 128f;

    [SerializeField] private DoorDirection direction;
    [SerializeField] private bool isLocked;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Debugging")]
    [SerializeField] public RoomType currentDoorType;

    [Header("Door Sprites")]
    [SerializeField] private Sprite normalDoorSprite;
    [SerializeField] private Sprite shopDoorSprite;
    [SerializeField] private Sprite bossDoorSprite;

    [Header("Shop Locked Door (Candado)")]
    [SerializeField] private Sprite lockedDoorSprite;
    [SerializeField] private Sprite[] unlockAnimationFrames;
    [SerializeField] private float unlockFrameRate = 14f;

    [Header("Combat Door")]
    [SerializeField] private Sprite combatClosedSprite;
    [SerializeField] private Sprite combatOpenSprite;
    [SerializeField] private Sprite[] combatAnimationFrames;
    [SerializeField] private float combatFrameRate = 14f;

    [Header("Boss Door")]
    [Tooltip("Tira PUERTAV2jefe anim. Frame 0 = cerrada, ultimo = abierta con calavera.")]
    [SerializeField] private Sprite[] bossAnimationFrames;
    [SerializeField] private Texture2D bossAnimSheet;
    [SerializeField] private float bossFrameRate = 16f;

    private Sprite[] runtimeBossFrames;

    private RoomNode myNode;

    private bool canTrigger = true;
    private bool isAnimating = false;
    private Coroutine doorAnimRoutine;

    private bool playerInRange = false;
    private GameObject currentPlayer;

    public DoorDirection Direction => direction;
    public bool IsLocked => isLocked;

    private bool IsShopDoor => currentDoorType == RoomType.Shop;
    private bool IsBossDoor => currentDoorType == RoomType.Boss;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        EnsureBossFrames();
    }

    private void Update()
    {
        if (GamePause.IsGameplayFrozen) return;

        if (playerInRange && isLocked && !isAnimating)
        {
            if (IsShopDoor && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                TryUnlock();
            }
        }
    }

    private void OnDisable()
    {
        isAnimating = false;
        doorAnimRoutine = null;
    }

    public void Initialize(RoomNode node, DoorDirection newDirection)
    {
        myNode = node;
        direction = newDirection;
        canTrigger = true;
        RotateDoorVisuals(direction);
        EnsureBossFrames();
    }

    private void RotateDoorVisuals(DoorDirection dir)
    {
        switch (dir)
        {
            case DoorDirection.Up:
                transform.rotation = Quaternion.Euler(0, 0, 0);
                break;
            case DoorDirection.Right:
                transform.rotation = Quaternion.Euler(0, 0, -90);
                break;
            case DoorDirection.Down:
                transform.rotation = Quaternion.Euler(0, 0, 180);
                break;
            case DoorDirection.Left:
                transform.rotation = Quaternion.Euler(0, 0, 90);
                break;
        }
    }

    public void SetLocked(bool locked)
    {
        CancelDoorAnimation();
        isLocked = locked;
        UpdateDoorVisual();
    }

    public void SetDoorType(RoomType type, RoomNode node)
    {
        if (currentDoorType == RoomType.Shop && type == RoomType.Normal) return;
        if (currentDoorType == RoomType.Boss && type == RoomType.Normal) return;

        if (spriteRenderer == null) return;

        spriteRenderer.color = Color.white;
        myNode = node;
        currentDoorType = type;

        if (type == RoomType.Shop)
        {
            isLocked = !node.isShopUnlocked;
        }

        CancelDoorAnimation();
        UpdateDoorVisual();
    }

    private Sprite[] GetBossFrames()
    {
        EnsureBossFrames();

        if (HasSerializedBossFrames())
            return bossAnimationFrames;

        return runtimeBossFrames;
    }

    private bool HasSerializedBossFrames()
    {
        return bossAnimationFrames != null
            && bossAnimationFrames.Length > 1
            && bossAnimationFrames[0] != null
            && bossAnimationFrames[bossAnimationFrames.Length - 1] != null;
    }

    private void EnsureBossFrames()
    {
        if (HasSerializedBossFrames())
            return;

        if (runtimeBossFrames != null && runtimeBossFrames.Length > 0)
            return;

        if (bossAnimSheet == null) return;

        int count = Mathf.Max(1, bossAnimSheet.width / BossFrameWidth);
        runtimeBossFrames = new Sprite[count];

        for (int i = 0; i < count; i++)
        {
            runtimeBossFrames[i] = Sprite.Create(
                bossAnimSheet,
                new Rect(i * BossFrameWidth, 0f, BossFrameWidth, BossFrameHeight),
                new Vector2(0.5f, 0.5f),
                BossPixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
        }
    }

    private Sprite GetBossClosedSprite()
    {
        Sprite[] frames = GetBossFrames();
        if (frames != null && frames.Length > 0 && frames[0] != null)
            return frames[0];

        return combatClosedSprite;
    }

    private Sprite GetBossOpenSprite()
    {
        Sprite[] frames = GetBossFrames();
        if (frames != null && frames.Length > 0)
        {
            Sprite last = frames[frames.Length - 1];
            if (last != null) return last;
        }

        return bossDoorSprite != null ? bossDoorSprite : combatOpenSprite;
    }

    private void UpdateDoorVisual()
    {
        if (spriteRenderer == null) return;

        if (isLocked)
        {
            if (IsShopDoor)
            {
                if (lockedDoorSprite != null) spriteRenderer.sprite = lockedDoorSprite;
            }
            else if (IsBossDoor)
            {
                Sprite closed = GetBossClosedSprite();
                if (closed != null) spriteRenderer.sprite = closed;
            }
            else if (combatClosedSprite != null)
            {
                spriteRenderer.sprite = combatClosedSprite;
            }
            return;
        }

        if (IsShopDoor)
        {
            if (shopDoorSprite != null) spriteRenderer.sprite = shopDoorSprite;
            return;
        }

        if (IsBossDoor)
        {
            Sprite open = GetBossOpenSprite();
            if (open != null) spriteRenderer.sprite = open;
            return;
        }

        if (combatOpenSprite != null)
        {
            spriteRenderer.sprite = combatOpenSprite;
            return;
        }

        if (normalDoorSprite != null) spriteRenderer.sprite = normalDoorSprite;
    }

    public void PlayUnlockAnimation(Action onComplete = null)
    {
        if (!isLocked)
        {
            UpdateDoorVisual();
            onComplete?.Invoke();
            return;
        }

        if (isAnimating) return;

        isLocked = false;

        if (!gameObject.activeInHierarchy)
        {
            UpdateDoorVisual();
            onComplete?.Invoke();
            return;
        }

        isAnimating = true;
        doorAnimRoutine = StartCoroutine(UnlockAnimationRoutine(onComplete));
    }

    private IEnumerator UnlockAnimationRoutine(Action onComplete)
    {
        if (IsShopDoor)
        {
            yield return PlaySpriteFrames(unlockAnimationFrames, unlockFrameRate, reverse: false);
        }
        else if (IsBossDoor)
        {
            yield return PlaySpriteFrames(GetBossFrames(), bossFrameRate, reverse: false);
        }
        else
        {
            yield return PlaySpriteFrames(combatAnimationFrames, combatFrameRate, reverse: false);
        }

        isAnimating = false;
        doorAnimRoutine = null;
        UpdateDoorVisual();
        onComplete?.Invoke();
    }

    public void PlayLockAnimation()
    {
        if (IsShopDoor)
        {
            SetLocked(true);
            return;
        }

        if (isLocked && !isAnimating)
        {
            UpdateDoorVisual();
            return;
        }

        CancelDoorAnimation();
        isLocked = true;

        if (!gameObject.activeInHierarchy)
        {
            UpdateDoorVisual();
            return;
        }

        isAnimating = true;
        doorAnimRoutine = StartCoroutine(LockAnimationRoutine());
    }

    private IEnumerator LockAnimationRoutine()
    {
        if (IsBossDoor)
            yield return PlaySpriteFrames(GetBossFrames(), bossFrameRate, reverse: true);
        else
            yield return PlaySpriteFrames(combatAnimationFrames, combatFrameRate, reverse: true);

        isAnimating = false;
        doorAnimRoutine = null;
        UpdateDoorVisual();
    }

    private IEnumerator PlaySpriteFrames(Sprite[] frames, float frameRate, bool reverse)
    {
        if (frames == null || frames.Length == 0 || spriteRenderer == null)
            yield break;

        float frameDuration = 1f / Mathf.Max(1f, frameRate);

        if (reverse)
        {
            for (int i = frames.Length - 1; i >= 0; i--)
            {
                if (frames[i] != null) spriteRenderer.sprite = frames[i];
                yield return new WaitForSeconds(frameDuration);
            }
        }
        else
        {
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null) spriteRenderer.sprite = frames[i];
                yield return new WaitForSeconds(frameDuration);
            }
        }
    }

    private void CancelDoorAnimation()
    {
        if (doorAnimRoutine != null)
        {
            StopCoroutine(doorAnimRoutine);
            doorAnimRoutine = null;
        }
        isAnimating = false;
    }

    public void RefreshFromNeighbor(RoomNode neighbor)
    {
        if (neighbor == null) return;

        SetDoorType(neighbor.information.type, neighbor);
    }

    private void TryUnlock()
    {
        if (currentPlayer == null) return;

        PlayerKeys keys = currentPlayer.GetComponent<PlayerKeys>();
        if (keys != null && keys.UseKey())
        {
            if (myNode != null)
            {
                myNode.isShopUnlocked = true;
            }

            PlayUnlockAnimation(() =>
            {
                if (playerInRange && canTrigger)
                {
                    canTrigger = false;
                    DungeonManager.Instance.TryMoveToNextRoom(direction);
                }
            });
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        currentPlayer = other.gameObject;

        if (GamePause.IsGameplayFrozen) return;

        if (!canTrigger || isLocked)
        {
            return;
        }

        canTrigger = false;
        DungeonManager.Instance.TryMoveToNextRoom(direction);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        currentPlayer = null;
        canTrigger = true;
    }
}
