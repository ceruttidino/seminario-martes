using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class RoomInstance : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform defaultSpawnPoint;
    [SerializeField] private List<DoorSpawnPoint> doorSpawnPoints = new List<DoorSpawnPoint>();

    [Header("Doors")]
    [SerializeField] private List<RoomDoor> roomDoors = new List<RoomDoor>();

    private Dictionary<DoorDirection, Transform> spawnPointLookup = new Dictionary<DoorDirection, Transform>();
    private Dictionary<DoorDirection, RoomDoor> doorLookup = new Dictionary<DoorDirection, RoomDoor>();

    public Transform DefaultSpawnPoint => defaultSpawnPoint;

    private void Awake()
    {
        BuildLookups();
    }

    private void BuildLookups()
    {
        spawnPointLookup.Clear();
        doorLookup.Clear();

        Debug.Log($"[RoomInstance] Building lookups for room instance: {name}");

        foreach (DoorSpawnPoint point in doorSpawnPoints)
        {
            if(point != null && point.spawnPoint != null && !spawnPointLookup.ContainsKey(point.direction))
            {
                spawnPointLookup.Add(point.direction, point.spawnPoint);
                Debug.Log($"[RoomInstance] Spawn point added: {point.direction} -> {point.spawnPoint.name}");
            }
        }
        foreach (RoomDoor door in roomDoors) 
        {
            if(door != null && !doorLookup.ContainsKey(door.Direction))
            {
                doorLookup.Add(door.Direction, door);
                Debug.Log($"[RoomInstance] Door added: {door.Direction} -> {door.name}");
            }
        }
    }

    public Transform GetSpawnPointFromEntry(DoorDirection entryDirection)
    {
        if(spawnPointLookup.TryGetValue(entryDirection, out Transform spawn))
        {
            Debug.Log($"[RoomInstance] Found spawn point for {entryDirection}: {spawn.name}");
            return spawn;
        }
        Debug.LogWarning($"[RoomInstance] No spawn point found for {entryDirection}. Falling back to default spawn.");
        return defaultSpawnPoint;
    }

    public void ConfigureDoors(RoomNode node)
    {
        Debug.Log($"[RoomInstance] Configuring doors for node: {node.uniqueNodeID}");

        foreach (var pair in doorLookup)
        {
            DoorDirection direction = pair.Key;
            RoomDoor door = pair.Value;

            bool shouldBeActive = node.information.HasDoor(direction);

            Debug.Log($"[RoomInstance] Door {direction} active = {shouldBeActive}");

            door.gameObject.SetActive(shouldBeActive);

            if (shouldBeActive)
            {
                door.Initialize(direction);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (defaultSpawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(defaultSpawnPoint.position, 0.15f);
        }

        if (doorSpawnPoints != null)
        {
            Gizmos.color = Color.yellow;

            foreach (DoorSpawnPoint point in doorSpawnPoints)
            {
                if (point != null && point.spawnPoint != null)
                {
                    Gizmos.DrawSphere(point.spawnPoint.position, 0.12f);
                }
            }
        }
    }
}
