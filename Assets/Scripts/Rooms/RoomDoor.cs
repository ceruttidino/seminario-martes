using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class RoomDoor : MonoBehaviour
{
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
    [Tooltip("Sprite estatico del candado. Solo se usa en puertas que llevan a la Shop.")]
    [SerializeField] private Sprite lockedDoorSprite;
    [Tooltip("Animacion de la ganzua / candado rompiendose. Solo se usa en la Shop.")]
    [SerializeField] private Sprite[] unlockAnimationFrames;
    [SerializeField] private float unlockFrameRate = 14f;

    [Header("Combat Door")]
    [Tooltip("Puerta cerrada sin candado. Se muestra mientras hay enemigos en la room.")]
    [SerializeField] private Sprite combatClosedSprite;
    [Tooltip("Puerta abierta. Se muestra cuando la room esta limpia.")]
    [SerializeField] private Sprite combatOpenSprite;
    [Tooltip("Animacion de abrir (al reves se usa para cerrar). No incluye candado.")]
    [SerializeField] private Sprite[] combatAnimationFrames;
    [SerializeField] private float combatFrameRate = 14f;

    private RoomNode myNode;

    private bool canTrigger = true;
    private bool isAnimating = false;
    private Coroutine doorAnimRoutine;

    private bool playerInRange = false;
    private GameObject currentPlayer;

    public DoorDirection Direction => direction;

    private bool IsShopDoor => currentDoorType == RoomType.Shop;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer.sprite == null && combatOpenSprite != null)
        {
            spriteRenderer.sprite = combatOpenSprite;
        }
        else if (spriteRenderer.sprite == null && normalDoorSprite != null)
        {
            spriteRenderer.sprite = normalDoorSprite;
        }
    }

    private void Update()
    {
        if (playerInRange && isLocked && !isAnimating)
        {
            if (IsShopDoor && Keyboard.current.fKey.wasPressedThisFrame)
            {
                TryUnlock();
            }
        }
    }

    // Si la room se desactiva (ej. al salir hacia otra room) Unity corta la
    // corutina de animacion sin avisar. Sin esto, isAnimating podria quedar
    // trabado en true y la puerta de la Shop no responderia mas a la ganzua.
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

    // Shop bloqueada: candado. Cualquier otra puerta bloqueada: version cerrada
    // de combate (sin candado). Desbloqueada: cartel de Shop o puerta abierta.
    private void UpdateDoorVisual()
    {
        if (spriteRenderer == null) return;

        if (isLocked)
        {
            if (IsShopDoor)
            {
                if (lockedDoorSprite != null) spriteRenderer.sprite = lockedDoorSprite;
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

        if (combatOpenSprite != null)
        {
            spriteRenderer.sprite = combatOpenSprite;
            return;
        }

        switch (currentDoorType)
        {
            case RoomType.Boss:
                if (bossDoorSprite != null) spriteRenderer.sprite = bossDoorSprite;
                break;
            case RoomType.Normal:
            default:
                if (normalDoorSprite != null) spriteRenderer.sprite = normalDoorSprite;
                break;
        }
    }

    // Abre la puerta con animacion. Shop: ganzua / candado. El resto: hoja de
    // combate hacia adelante. Se usa al limpiar la sala y al usar una ganzua.
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

        Sprite[] frames = IsShopDoor ? unlockAnimationFrames : combatAnimationFrames;
        float frameRate = IsShopDoor ? unlockFrameRate : combatFrameRate;

        isAnimating = true;
        doorAnimRoutine = StartCoroutine(PlaySpriteAnimation(frames, frameRate, reverse: false, onComplete));
    }

    // Cierra la puerta con animacion de combate (hoja al reves). La Shop no se
    // anima: queda en el candado de golpe, porque el candado no tiene cierre.
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
        doorAnimRoutine = StartCoroutine(PlaySpriteAnimation(combatAnimationFrames, combatFrameRate, reverse: true, null));
    }

    private IEnumerator PlaySpriteAnimation(Sprite[] frames, float frameRate, bool reverse, Action onComplete)
    {
        if (frames != null && frames.Length > 0 && spriteRenderer != null)
        {
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

        isAnimating = false;
        doorAnimRoutine = null;
        UpdateDoorVisual();
        onComplete?.Invoke();
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

            // El jugador ya esta parado sobre la puerta al usar la ganzua: primero se
            // reproduce la animacion de desbloqueo (candado) y, al terminar, se pasa
            // directo a la siguiente room.
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
