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

    }

    public void SetLocked(bool locked) 
    {
        isLocked = locked;
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
