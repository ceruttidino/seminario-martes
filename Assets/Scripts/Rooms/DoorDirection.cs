using UnityEngine;

public enum DoorDirection
{
    Up,
    Down,
    Left,
    Right
}

public static class DoorDirectionExtensions
{
    public static DoorDirection Opposite(this DoorDirection direction)
    {
        switch (direction)
        {
            case DoorDirection.Up: return DoorDirection.Down;
            case DoorDirection.Down: return DoorDirection.Up;
            case DoorDirection.Left: return DoorDirection.Right;
            default: return DoorDirection.Left;
        }
    }
}
