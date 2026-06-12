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
    private RoomNode myNode;

    private bool canTrigger = true;

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
        // Only check for input if the player is in range AND the door is currently locked
        if (playerInRange && isLocked)
        {
            // Check if this is a shop door - you only want the 'F' to unlock logic for shops
            if (currentDoorType == RoomType.Shop && Keyboard.current.fKey.wasPressedThisFrame)
            {
                TryUnlock();
            }
        }
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
        isLocked = locked;
    }

public void SetDoorType(RoomType type, RoomNode node)
    {
        if (currentDoorType == RoomType.Shop && type == RoomType.Normal) return;
        if (currentDoorType == RoomType.Boss && type == RoomType.Normal) return;

        if (spriteRenderer == null) return;
        
        spriteRenderer.color = Color.white; 
        myNode = node;
        currentDoorType = type; 

        switch (type)
        {
            case RoomType.Shop:
                if (shopDoorSprite != null) spriteRenderer.sprite = shopDoorSprite;
                isLocked = !node.isShopUnlocked; 
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
            // 1. Unlock the visual state
            isLocked = false;

            // 2. IMPORTANT: Update the data so it survives when the room is disabled
            // Ensure you have a reference to the node assigned during Initialize
            if (myNode != null)
            {
                myNode.isShopUnlocked = true;
            }
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