using System.Collections.Generic;
using UnityEngine;

public class DungeonLayout //Builds floor layout (How Rooms Connect with Each Other)
{
    private List<RoomInformation> remainingNormalRooms = new List<RoomInformation>();
    private List<RoomNode> generatedRooms = new List<RoomNode>();

    private Dictionary<Vector2Int, RoomNode> grid = new Dictionary<Vector2Int, RoomNode>();

    private RoomInformation shopRoomInformation;
    private RoomInformation bossRoomInformation;
    private bool shopAlreadyGenerated = false;
    private bool bossAlreadyGenerated = false;

    public RoomNode StartNode { get; private set; }

    public void Build(RoomInformation startRoom, List<RoomInformation> normalRooms, RoomInformation shopRoom, RoomInformation bossRoom)
    {
        generatedRooms.Clear();
        grid.Clear();
        remainingNormalRooms = new List<RoomInformation>(normalRooms);

        this.shopRoomInformation = shopRoom;
        this.bossRoomInformation = bossRoom;
        shopAlreadyGenerated = false;
        bossAlreadyGenerated = false;

        StartNode = new RoomNode("Start", startRoom);
        StartNode.gridPosition = Vector2Int.zero;

        generatedRooms.Add(StartNode);
        grid.Add(StartNode.gridPosition, StartNode);
    }

    private Vector2Int DirectionToGridOffset(DoorDirection dir)
    {
        switch (dir)
        {
            case DoorDirection.Up: return Vector2Int.up;
            case DoorDirection.Down: return Vector2Int.down;
            case DoorDirection.Right: return Vector2Int.right;
            case DoorDirection.Left: return Vector2Int.left;
            default: return Vector2Int.zero;
        }
    }

    public RoomNode CreateNextRoom(RoomNode fromNode, DoorDirection exitDirection, RoomType roomType)
    {
        if (fromNode == null || fromNode.information == null)
        {
            return null;
        }

        if (fromNode.HasNeighbor(exitDirection))
        {
            RoomNode existingNeighbor = fromNode.GetNeighbor(exitDirection);
            return existingNeighbor;
        }

        Vector2Int newPosition = fromNode.gridPosition + DirectionToGridOffset(exitDirection);

        if (grid.ContainsKey(newPosition))
        {
            RoomNode existing = grid[newPosition];

            fromNode.SetNeighbor(exitDirection, existing);
            existing.SetNeighbor(GetOppositeDirection(exitDirection), fromNode);

            return existing;
        }

        if (!fromNode.information.HasDoor(exitDirection))
        {
            return null;
        }

        DoorDirection requiredEntrance = GetOppositeDirection(exitDirection);

        RoomInformation selected = null;

        if (roomType == RoomType.Normal)
        {
            if (remainingNormalRooms.Count == 0)
            {
                return null;
            }

            List<RoomInformation> validCandidates = new List<RoomInformation>();

            foreach (RoomInformation room in remainingNormalRooms)
            {
                if (room != null && room.HasDoor(requiredEntrance))
                {
                    validCandidates.Add(room);
                }
            }

            if (validCandidates.Count == 0)
            {
                return null;
            }

            int randomIndex = Random.Range(0, validCandidates.Count);
            selected = validCandidates[randomIndex];
            remainingNormalRooms.Remove(selected);
        }
        else if (roomType == RoomType.Shop)
        {
            if (shopAlreadyGenerated || shopRoomInformation == null)
            {
                return null;
            }

            if (!shopRoomInformation.HasDoor(requiredEntrance))
            {
                return null;
            }

            selected = shopRoomInformation;
        }

        else if (roomType == RoomType.Boss)
        {
            if (bossAlreadyGenerated || bossRoomInformation == null)
            {
                return null;
            }

            if (!bossRoomInformation.HasDoor(requiredEntrance))
            {
                return null;
            }
            selected = bossRoomInformation;
        }

        if (selected == null)
        {
            return null;
        }

        RoomNode newNode = new RoomNode($"Room_{generatedRooms.Count}", selected);

        newNode.gridPosition = newPosition;
        grid.Add(newPosition, newNode);

        fromNode.SetNeighbor(exitDirection, newNode);
        newNode.SetNeighbor(requiredEntrance, fromNode);

        generatedRooms.Add(newNode);

        if (roomType == RoomType.Shop)
        {
            shopAlreadyGenerated = true;
        }
        else if (roomType == RoomType.Boss)
        {
            bossAlreadyGenerated = true;
        }

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
