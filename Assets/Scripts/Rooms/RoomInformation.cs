using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomInformation", menuName = "Dungeon/Room Information")]
public class RoomInformation : ScriptableObject //Save Room ID, Type, Prefab, Doors, Etc. (Existing Rooms)
{
    [Header("Info")]
    public string roomID;
    public RoomType type = RoomType.Normal;
    public RoomInstance prefab;

    [Header("Doors")]
    public List<DoorDirection> availableDoors = new List<DoorDirection>();

    public bool HasDoor(DoorDirection direction)
    {
        return availableDoors.Contains(direction);
    }
}
