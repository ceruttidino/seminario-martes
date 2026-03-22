using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class DungeonLayout //Builds floor layout (How Rooms Connect with Each Other)
{
    private List<RoomInformation> remainingNormalRooms = new List<RoomInformation>();
    private List<RoomNode> generatedRooms = new List<RoomNode>();

    public RoomNode StartNode { get; private set; }

    public void Build(RoomInformation startRoom, List<RoomInformation> normalRooms)
    {
        generatedRooms.Clear();
        remainingNormalRooms = new List<RoomInformation>(normalRooms);

        StartNode = new RoomNode("Start", startRoom);
        generatedRooms.Add(StartNode);

        Debug.Log($"[DungeonLayout] Build complete. Start room: {startRoom.roomID}");
        Debug.Log($"[DungeonLayout] Normal rooms available: {remainingNormalRooms.Count}");
    }

    public RoomNode CreateNextRoom(RoomNode fromNode, DoorDirection exitDirection)
    {
        if (fromNode == null || fromNode.information == null)
        {
            Debug.LogWarning("[DungeonLayout] CreateNextRoom failed: fromNode invalid.");
            return null;
        }

        Debug.Log($"[DungeonLayout] Request to move from node: {fromNode.uniqueNodeID} ({fromNode.information.roomID}) through {exitDirection}");

        if (fromNode.HasNeighbor(exitDirection))
        {
            RoomNode existingNeighbor = fromNode.GetNeighbor(exitDirection);
            Debug.Log($"[DungeonLayout] Existing neighbor found: {existingNeighbor.uniqueNodeID} ({existingNeighbor.information.roomID})");
            return existingNeighbor;
        }

        Debug.Log("[DungeonLayout] No existing neighbor found. Attempting to create new room.");

        if (remainingNormalRooms.Count == 0)
        {
            Debug.LogWarning("[DungeonLayout] No remaining rooms available to create.");
            return null;
        }
        if (!fromNode.information.HasDoor(exitDirection))
        {
            Debug.LogWarning($"[DungeonLayout] Current room does not have a door towards {exitDirection}");
            return null;
        }

        DoorDirection requiredEntrance = GetOppositeDirection(exitDirection);

        List<RoomInformation> validCandidates = new List<RoomInformation>();

        foreach (RoomInformation room in remainingNormalRooms) 
        {
            if(room != null && room.HasDoor(requiredEntrance))
            {
                validCandidates.Add(room);
                Debug.Log($"[DungeonLayout] Valid candidate: {room.roomID}");
            }
        }

        if (validCandidates.Count == 0) 
        {
            Debug.LogWarning("[DungeonLayout] No valid candidates found for required entrance.");
            return null;
        }

        int randomIndex = Random.Range(0, validCandidates.Count);
        RoomInformation selected = validCandidates[randomIndex];

        Debug.Log($"[DungeonLayout] Selected new room: {selected.roomID}");

        remainingNormalRooms.Remove(selected);

        RoomNode newNode = new RoomNode($"Room_{generatedRooms.Count}", selected);

        fromNode.SetNeighbor(exitDirection, newNode);
        newNode.SetNeighbor(requiredEntrance, fromNode);

        generatedRooms.Add(newNode);

        Debug.Log($"[DungeonLayout] New node created: {newNode.uniqueNodeID}");
        Debug.Log($"[DungeonLayout] Connected {fromNode.uniqueNodeID} --({exitDirection}/{requiredEntrance})--> {newNode.uniqueNodeID}");
        Debug.Log($"[DungeonLayout] Remaining rooms count: {remainingNormalRooms.Count}");

        return newNode;
    }

    public static DoorDirection GetOppositeDirection(DoorDirection direction) 
    {
        switch (direction) 
        {
            case DoorDirection.Up:
                return DoorDirection.Down;
            case DoorDirection.Down:
                return DoorDirection.Up;
            case DoorDirection.Right:
                return DoorDirection.Left;
            case DoorDirection.Left:
                return DoorDirection.Right;
            default:
                return DoorDirection.Down;
        }
    }
}
