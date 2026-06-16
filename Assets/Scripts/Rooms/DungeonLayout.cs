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

        if (fromNode.information.type == RoomType.Shop || fromNode.information.type == RoomType.Boss)
        {
            return null;
        }

        Vector2Int newPosition = fromNode.gridPosition + DirectionToGridOffset(exitDirection);

        if (WouldPlaceSpecialNextToOpposite(newPosition, roomType))
        {
            return null;
        }

        if (grid.ContainsKey(newPosition))
        {
            RoomNode existing = grid[newPosition];

            if (existing == null || existing.information == null)
                return null;

            if ((existing.information.type == RoomType.Shop ||
                existing.information.type == RoomType.Boss) &&
                existing.GetNeighborsCount() >= 1)
            {
                return null;
            }

            if (IsShopBossCombination(fromNode.information.type, existing.information.type))
                return null;

            if (roomType != RoomType.Normal && existing.information.type != roomType)
                return null;

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

        UpdateRoomDoors(fromNode);
        UpdateRoomDoors(newNode);

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

    private bool IsShopBossCombination(RoomType a, RoomType b)
    {
        return (a == RoomType.Shop && b == RoomType.Boss) ||
               (a == RoomType.Boss && b == RoomType.Shop);
    }

    private bool WouldPlaceSpecialNextToOpposite(Vector2Int position, RoomType newRoomType)
    {
        if (newRoomType != RoomType.Shop && newRoomType != RoomType.Boss)
            return false;

        foreach (DoorDirection dir in System.Enum.GetValues(typeof(DoorDirection)))
        {
            Vector2Int checkPos = position + DirectionToGridOffset(dir);

            if (!grid.TryGetValue(checkPos, out RoomNode neighbor))
                continue;

            if (neighbor == null || neighbor.information == null)
                continue;

            if (IsShopBossCombination(newRoomType, neighbor.information.type))
                return true;
        }

        return false;
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

    private void UpdateRoomDoors(RoomNode node)
    {
        if (node.spawnedInstance != null)
        {
            node.spawnedInstance.ConfigureDoors(node);
        }
    }

    public List<RoomNode> GetAllRooms()
    {
        return generatedRooms;
    }

}
