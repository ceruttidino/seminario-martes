using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RoomDoor : MonoBehaviour //Script for each door prefab
{
    [SerializeField] private DoorDirection direction;
    [SerializeField] private bool isLocked;

    private bool canTrigger = true;

    public DoorDirection Direction => direction;

    public void Initialize(DoorDirection newDirection)
    {
        direction = newDirection;
        canTrigger = true;

        Debug.Log($"[RoomDoor] Initialized door: {name} with direction: {direction}");
    }

    public void SetLocked(bool locked) 
    {
        isLocked = locked;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[RoomDoor] Trigger entered on door {name} by {other.name}");

        if (!canTrigger || isLocked)
        {
            Debug.Log("[RoomDoor] Trigger blocked because canTrigger = false or door is locked");
            return;
        }
        if (!other.CompareTag("Player"))
        {
            return;
        }

        Debug.Log($"[RoomDoor] Player used door {direction}");

        canTrigger = false;
        DungeonManager.Instance.TryMoveToNextRoom(direction);
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canTrigger = true;
            Debug.Log($"[RoomDoor] Player exited door trigger: {name}. canTrigger reset to true.");
        }
    }
}
