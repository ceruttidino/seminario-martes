using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Layout dinámico: el piso crece al cruzar puertas. Cada puerta solo existe si hay una room vecina
/// real, y esa vecina siempre tiene la puerta de vuelta (mismo lado opuesto).
/// </summary>
public class DungeonLayout
{
    private readonly List<RoomInformation> allNormalRooms = new List<RoomInformation>();
    private readonly List<RoomInformation> remainingNormalRooms = new List<RoomInformation>();
    private readonly List<RoomNode> generatedRooms = new List<RoomNode>();
    private readonly Dictionary<Vector2Int, RoomNode> grid = new Dictionary<Vector2Int, RoomNode>();

    private RoomInformation shopRoomInformation;
    private RoomInformation bossRoomInformation;
    private RoomInformation buffRoomInformation;
    private List<RoomInformation> challengeRoomInformations = new List<RoomInformation>();

    private bool shopAlreadyGenerated;
    private bool bossAlreadyGenerated;
    private bool challengeAlreadyGenerated;
    private bool buffAlreadyGenerated;
    private bool preferRoomsWithSpareDoor;
    private RoomInformation forcedSelection;

    public RoomNode StartNode { get; private set; }

    public void Build(RoomInformation startRoom, List<RoomInformation> normalRooms, RoomInformation shopRoom, RoomInformation bossRoom, List<RoomInformation> challengeRooms, RoomInformation buffRoom = null)
    {
        generatedRooms.Clear();
        grid.Clear();
        allNormalRooms.Clear();
        remainingNormalRooms.Clear();
        buffRoomInformation = buffRoom;
        buffAlreadyGenerated = false;
        preferRoomsWithSpareDoor = false;
        forcedSelection = null;

        if (normalRooms != null)
        {
            foreach (RoomInformation room in normalRooms)
            {
                // CAMBIADO: las puertas ahora salen de room.Doors (detectadas del prefab).
                // Además se ignora cualquier room que no sea Normal, por si quedó en la lista equivocada.
                if (room == null || room.prefab == null || room.type != RoomType.Normal || room.Doors.Count == 0)
                    continue;

                // La buff room se coloca aparte, una vez por piso. No entra en el sorteo normal.
                if (buffRoom != null && room == buffRoom)
                    continue;

                allNormalRooms.Add(room);
            }
        }

        remainingNormalRooms.AddRange(allNormalRooms);

        shopRoomInformation = shopRoom;
        bossRoomInformation = bossRoom;
        challengeRoomInformations = challengeRooms != null
            ? new List<RoomInformation>(challengeRooms)
            : new List<RoomInformation>();

        shopAlreadyGenerated = false;
        bossAlreadyGenerated = false;
        challengeAlreadyGenerated = false;

        StartNode = new RoomNode("Start", startRoom);
        StartNode.gridPosition = Vector2Int.zero;

        generatedRooms.Add(StartNode);
        grid.Add(StartNode.gridPosition, StartNode);
    }

    public RoomNode CreateNextRoom(RoomNode fromNode, DoorDirection exitDirection, RoomType roomType)
    {
        return CreateNextRoom(fromNode, exitDirection, roomType, out _);
    }

    public RoomNode CreateNextRoom(RoomNode fromNode, DoorDirection exitDirection, RoomType roomType, out bool createdNew)
    {
        createdNew = false;

        if (fromNode == null || fromNode.information == null)
            return null;

        if (fromNode.HasNeighbor(exitDirection))
            return fromNode.GetNeighbor(exitDirection);

        if (IsDeadEndType(fromNode.information.type))
            return null;

        if (!fromNode.information.HasDoor(exitDirection))
            return null;

        DoorDirection requiredEntrance = GetOppositeDirection(exitDirection);
        Vector2Int newPosition = fromNode.gridPosition + DirectionToGridOffset(exitDirection);

        if (WouldPlaceSpecialNextToOpposite(newPosition, roomType))
            return null;

        if (grid.TryGetValue(newPosition, out RoomNode existing))
            return TryReuseExisting(fromNode, exitDirection, requiredEntrance, existing, roomType);

        RoomInformation selected = SelectRoom(roomType, requiredEntrance);
        if (selected == null)
            return null;

        RoomNode newNode = new RoomNode($"Room_{generatedRooms.Count}", selected);
        newNode.gridPosition = newPosition;
        grid.Add(newPosition, newNode);

        fromNode.SetNeighbor(exitDirection, newNode);
        newNode.SetNeighbor(requiredEntrance, fromNode);

        UpdateRoomDoors(fromNode);
        UpdateRoomDoors(newNode);

        generatedRooms.Add(newNode);
        MarkGenerated(roomType);
        createdNew = true;

        return newNode;
    }

    private RoomNode TryReuseExisting(RoomNode fromNode, DoorDirection exitDirection, DoorDirection requiredEntrance, RoomNode existing, RoomType roomType)
    {
        if (existing == null || existing.information == null)
            return null;

        // Si ya estaban unidas por esa puerta, se reutiliza. Nunca se "perfora" una room ya colocada
        // para inventar una puerta que el jugador no vio la primera vez.
        if (existing.GetNeighbor(requiredEntrance) == fromNode)
        {
            fromNode.SetNeighbor(exitDirection, existing);
            return existing;
        }

        if (existing.HasNeighbor(requiredEntrance))
            return null;

        if (!existing.information.HasDoor(requiredEntrance))
            return null;

        if (IsDeadEndType(existing.information.type) && existing.GetNeighborsCount() >= 1)
            return null;

        if (AreIncompatibleSpecialRooms(fromNode.information.type, existing.information.type))
            return null;

        if (roomType != RoomType.Normal && existing.information.type != roomType)
            return null;

        fromNode.SetNeighbor(exitDirection, existing);
        existing.SetNeighbor(requiredEntrance, fromNode);
        UpdateRoomDoors(fromNode);
        UpdateRoomDoors(existing);
        return existing;
    }

    private RoomInformation SelectRoom(RoomType roomType, DoorDirection requiredEntrance)
    {
        if (roomType == RoomType.Normal)
        {
            if (forcedSelection != null)
            {
                RoomInformation forced = forcedSelection;
                forcedSelection = null;
                return forced.prefab != null && forced.HasDoor(requiredEntrance) ? forced : null;
            }

            return PickNormalWithDoor(requiredEntrance);
        }

        if (roomType == RoomType.Shop)
        {
            if (shopAlreadyGenerated || shopRoomInformation == null || shopRoomInformation.prefab == null)
                return null;

            return shopRoomInformation.HasDoor(requiredEntrance) ? shopRoomInformation : null;
        }

        if (roomType == RoomType.Boss)
        {
            if (bossAlreadyGenerated || bossRoomInformation == null || bossRoomInformation.prefab == null)
                return null;

            return bossRoomInformation.HasDoor(requiredEntrance) ? bossRoomInformation : null;
        }

        if (roomType == RoomType.Challenge)
        {
            if (challengeAlreadyGenerated || challengeRoomInformations == null || challengeRoomInformations.Count == 0)
                return null;

            List<RoomInformation> validChallenges = new List<RoomInformation>();
            foreach (RoomInformation challengeRoom in challengeRoomInformations)
            {
                if (challengeRoom == null || challengeRoom.prefab == null)
                    continue;

                if (!string.IsNullOrEmpty(challengeRoom.challengeId) && ChallengeRunState.WasCompleted(challengeRoom.challengeId))
                    continue;

                if (challengeRoom.HasDoor(requiredEntrance))
                    validChallenges.Add(challengeRoom);
            }

            if (validChallenges.Count == 0)
                return null;

            return validChallenges[Random.Range(0, validChallenges.Count)];
        }

        return null;
    }

    private RoomInformation PickNormalWithDoor(DoorDirection requiredEntrance)
    {
        RoomInformation selected = PickFromPool(remainingNormalRooms, requiredEntrance);
        if (selected != null)
            return selected;

        // Pisos largos: si se acabó el pool, se reusa (sin inventar prefabs que no tengan esa puerta).
        remainingNormalRooms.Clear();
        remainingNormalRooms.AddRange(allNormalRooms);
        return PickFromPool(remainingNormalRooms, requiredEntrance);
    }

    public void SetPreferSpareDoors(bool prefer)
    {
        preferRoomsWithSpareDoor = prefer;
    }

    public RoomNode CreateBuffRoom(RoomNode fromNode, DoorDirection exitDirection, out bool createdNew)
    {
        createdNew = false;

        if (buffAlreadyGenerated || buffRoomInformation == null || buffRoomInformation.prefab == null)
            return null;

        forcedSelection = buffRoomInformation;
        RoomNode node = CreateNextRoom(fromNode, exitDirection, RoomType.Normal, out createdNew);
        forcedSelection = null;

        if (createdNew)
            buffAlreadyGenerated = true;

        return node;
    }

    private RoomInformation PickFromPool(List<RoomInformation> pool, DoorDirection requiredEntrance)
    {
        List<RoomInformation> valid = new List<RoomInformation>();
        foreach (RoomInformation room in pool)
        {
            if (room != null && room.prefab != null && room.HasDoor(requiredEntrance))
                valid.Add(room);
        }

        if (valid.Count == 0)
            return null;

        // Mientras falten shop / boss / challenge / buff, se prefieren prefabs con una puerta de sobra
        // para poder colgar esas rooms sin inventar puertas.
        if (preferRoomsWithSpareDoor)
        {
            List<RoomInformation> spacious = valid.FindAll(room => room.Doors.Count >= 2); // CAMBIADO: room.Doors
            if (spacious.Count > 0)
                valid = spacious;
        }

        RoomInformation selected = valid[Random.Range(0, valid.Count)];
        pool.Remove(selected);
        return selected;
    }

    private void MarkGenerated(RoomType roomType)
    {
        if (roomType == RoomType.Shop)
            shopAlreadyGenerated = true;
        else if (roomType == RoomType.Boss)
            bossAlreadyGenerated = true;
        else if (roomType == RoomType.Challenge)
            challengeAlreadyGenerated = true;
    }

    private static bool IsDeadEndType(RoomType type)
    {
        return type == RoomType.Shop || type == RoomType.Boss || type == RoomType.Challenge;
    }

    private static bool AreIncompatibleSpecialRooms(RoomType a, RoomType b)
    {
        return IsDeadEndType(a) && IsDeadEndType(b);
    }

    private bool WouldPlaceSpecialNextToOpposite(Vector2Int position, RoomType newRoomType)
    {
        if (!IsDeadEndType(newRoomType))
            return false;

        foreach (DoorDirection dir in System.Enum.GetValues(typeof(DoorDirection)))
        {
            if (!grid.TryGetValue(position + DirectionToGridOffset(dir), out RoomNode neighbor))
                continue;

            if (neighbor == null || neighbor.information == null)
                continue;

            if (AreIncompatibleSpecialRooms(newRoomType, neighbor.information.type))
                return true;
        }

        return false;
    }

    private static Vector2Int DirectionToGridOffset(DoorDirection dir)
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

    public static DoorDirection GetOppositeDirection(DoorDirection direction)
    {
        return direction.Opposite();
    }

    private static void UpdateRoomDoors(RoomNode node)
    {
        if (node != null && node.spawnedInstance != null)
            node.spawnedInstance.ConfigureDoors(node);
    }

    public List<RoomNode> GetAllRooms()
    {
        return generatedRooms;
    }
}