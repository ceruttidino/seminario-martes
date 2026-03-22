using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoomNode //Logic room inside the dungeon
{
    public string uniqueNodeID;
    public RoomInformation information;
    public Dictionary<DoorDirection, RoomNode> neighboors = new Dictionary<DoorDirection, RoomNode>();

    public RoomInstance spawnedInstance;
    public bool hasBeenVisited;

    public RoomNode(string id, RoomInformation roomInformation)
    {
        uniqueNodeID = id;
        information = roomInformation;
    }

    public bool HasNeighbor(DoorDirection direction)
    {
        return neighboors.ContainsKey(direction);
    }

    public RoomNode GetNeighbor(DoorDirection direction) 
    {
        if (neighboors.TryGetValue(direction, out RoomNode neighbor)) 
        {
            return neighbor;
        }
        return null;
    }

    public void SetNeighbor(DoorDirection direction, RoomNode neighbor)
    {
        if (!neighboors.ContainsKey(direction))
        {
            neighboors.Add(direction, neighbor);
            Debug.Log($"[RoomNode] {uniqueNodeID} connected {direction} -> {neighbor.uniqueNodeID}");
        }
    }
}
