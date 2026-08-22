using System;
using System.Collections;
using System.Xml.XPath;
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

    [Header("Locked Door (Candado)")]
    [Tooltip("Sprite estatico mostrado mientras la puerta esta bloqueada (encadenada).")]
    [SerializeField] private Sprite lockedDoorSprite;
    [Tooltip("Secuencia de frames que se reproduce al desbloquear la puerta (cadena rompiendose y puerta abriendose).")]
    [SerializeField] private Sprite[] unlockAnimationFrames;
    [SerializeField] private float unlockFrameRate = 14f;

    private RoomNode myNode;

    private bool canTrigger = true;
    private bool isUnlocking = false;
    private Coroutine unlockRoutine;

    private bool playerInRange = false;
    private GameObject currentPlayer;

    public DoorDirection Direction => direction;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer.sprite == null && normalDoorSprite != null)
        {
            spriteRenderer.sprite = normalDoorSprite;
        }
    }

    private void Update()
    {
        if (playerInRange && isLocked && !isUnlocking)
        {
            if (currentDoorType == RoomType.Shop && Keyboard.current.fKey.wasPressedThisFrame)
            {
                TryUnlock();
            }
        }
    }

    // Si la room se desactiva (ej. al salir hacia otra room) Unity corta la
    // corutina de desbloqueo sin avisar. Sin esto, isUnlocking podria quedar
    // trabado en true para siempre y la puerta nunca mas respondería a la ganzua.
    private void OnDisable()
    {
        isUnlocking = false;
        unlockRoutine = null;
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
        CancelUnlockAnimation();
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

        CancelUnlockAnimation();
        UpdateDoorVisual();
    }

    // Aplica el sprite correspondiente al estado actual: si esta bloqueada muestra
    // el candado/cadena; si no, el sprite propio del tipo de puerta (Normal/Shop/Boss).
    private void UpdateDoorVisual()
    {
        if (spriteRenderer == null) return;

        if (isLocked)
        {
            if (lockedDoorSprite != null) spriteRenderer.sprite = lockedDoorSprite;
            return;
        }

        switch (currentDoorType)
        {
            case RoomType.Shop:
                if (shopDoorSprite != null) spriteRenderer.sprite = shopDoorSprite;
                break;
            case RoomType.Boss:
                if (bossDoorSprite != null) spriteRenderer.sprite = bossDoorSprite;
                break;
            case RoomType.Normal:
            default:
                if (normalDoorSprite != null) spriteRenderer.sprite = normalDoorSprite;
                break;
        }
    }

    // Desbloquea la puerta reproduciendo la animacion de la cadena rompiendose
    // antes de dejarla en el sprite abierto correspondiente a su tipo. Se usa
    // tanto cuando se limpia la sala de enemigos como cuando se usa una ganzua.
    public void PlayUnlockAnimation(Action onComplete = null)
    {
        if (!isLocked)
        {
            UpdateDoorVisual();
            onComplete?.Invoke();
            return;
        }

        if (isUnlocking) return;

        isLocked = false;

        // Una puerta sin vecino en esa direccion queda inactiva (ver
        // RoomInstance.ConfigureDoors). Unity no permite iniciar corutinas en
        // GameObjects inactivos: en ese caso aplicamos el estado final directo,
        // sin animacion, ya que el jugador no puede verla de todos modos.
        if (!gameObject.activeInHierarchy)
        {
            UpdateDoorVisual();
            onComplete?.Invoke();
            return;
        }

        isUnlocking = true;
        unlockRoutine = StartCoroutine(UnlockAnimationRoutine(onComplete));
    }

    private IEnumerator UnlockAnimationRoutine(Action onComplete)
    {
        if (unlockAnimationFrames != null && unlockAnimationFrames.Length > 0 && spriteRenderer != null)
        {
            float frameDuration = 1f / Mathf.Max(1f, unlockFrameRate);

            foreach (Sprite frame in unlockAnimationFrames)
            {
                if (frame != null) spriteRenderer.sprite = frame;
                yield return new WaitForSeconds(frameDuration);
            }
        }

        isUnlocking = false;
        unlockRoutine = null;
        UpdateDoorVisual();
        onComplete?.Invoke();
    }

    private void CancelUnlockAnimation()
    {
        if (unlockRoutine != null)
        {
            StopCoroutine(unlockRoutine);
            unlockRoutine = null;
        }
        isUnlocking = false;
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
            // reproduce la animacion de desbloqueo (cadena rompiendose) y, al terminar,
            // se pasa directo a la siguiente room en vez de esperar a que salga y vuelva
            // a entrar en el trigger para recien ahi cambiar de habitacion.
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