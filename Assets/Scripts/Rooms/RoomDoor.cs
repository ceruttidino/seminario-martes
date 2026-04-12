using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RoomDoor : MonoBehaviour //Script for each door prefab
{
    [SerializeField] private DoorDirection direction;
    [SerializeField] private bool isLocked;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Door Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color shopColor = Color.green;
    [SerializeField] private Color bossColor = Color.red;

    private bool canTrigger = true;

    public DoorDirection Direction => direction;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(DoorDirection newDirection)
    {
        direction = newDirection;
        canTrigger = true;

    }

    public void SetLocked(bool locked) 
    {
        isLocked = locked;
    }

    public void SetDoorType(RoomType type)
    {
        if (spriteRenderer == null) return;

        switch (type)
        {
            case RoomType.Shop:
                spriteRenderer.color = shopColor;
                break;

            case RoomType.Boss:
                spriteRenderer.color = bossColor;
                break;

            default:
                spriteRenderer.color = normalColor;
                break;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!canTrigger || isLocked)
        {
            return;
        }
        if (!other.CompareTag("Player"))
        {
            return;
        }

        canTrigger = false;
        DungeonManager.Instance.TryMoveToNextRoom(direction);
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canTrigger = true;
        }
    }
}
