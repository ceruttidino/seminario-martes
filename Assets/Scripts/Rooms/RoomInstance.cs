using System.Collections.Generic;
using UnityEngine;

public class RoomInstance : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform defaultSpawnPoint;
    [SerializeField] private List<DoorSpawnPoint> doorSpawnPoints = new List<DoorSpawnPoint>();

    [Header("Doors")]
    [SerializeField] private List<RoomDoor> roomDoors = new List<RoomDoor>();

    [Header("Digging Spots (TESTEANDO)")]
    [SerializeField] private Transform[] diggingSpotLocations;
    [SerializeField] private GameObject diggingSpotPrefab;
    [SerializeField] [Range(0f, 100f)] private float chanceToHaveDiggingSpots = 30f;

    [Header("Enemies")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] enemySpawnPoints;

    private EnemyBehaviour[] currentEnemies;
    private bool roomInCombat = false;

    private bool enemiesSpawned = false;
    private int enemiesAlive;


    private Dictionary<DoorDirection, Transform> spawnPointLookup = new Dictionary<DoorDirection, Transform>();
    private Dictionary<DoorDirection, RoomDoor> doorLookup = new Dictionary<DoorDirection, RoomDoor>();

    public Transform DefaultSpawnPoint => defaultSpawnPoint;

    private void Awake()
    {
        BuildLookups();
    }

    private void BuildLookups()
    {
        foreach (DoorSpawnPoint point in doorSpawnPoints)
        {
            if(point != null && point.spawnPoint != null)
            {
                spawnPointLookup.Add(point.direction, point.spawnPoint);
            }
        }
        foreach (RoomDoor door in roomDoors) 
        {
            if(door != null && !doorLookup.ContainsKey(door.Direction))
            {
                doorLookup.Add(door.Direction, door);
            }
        }
    }

    public Transform GetSpawnPointFromEntry(DoorDirection entryDirection)
    {
        if(spawnPointLookup.TryGetValue(entryDirection, out Transform spawn))
        {
            return spawn;
        }
        return defaultSpawnPoint;
    }

    public void ConfigureDoors(RoomNode node, DoorDirection? forcedDoor = null)
    {
        foreach (var pair in doorLookup)
        {
            DoorDirection direction = pair.Key;
            RoomDoor door = pair.Value;

            bool shouldBeActive = node.HasNeighbor(direction);

            if (forcedDoor.HasValue && direction == forcedDoor.Value)
            {
                shouldBeActive = true;
            }

            door.gameObject.SetActive(shouldBeActive);

            if (shouldBeActive)
            {
                door.Initialize(direction);

                RoomNode neighbor = node.GetNeighbor(direction);

                if (neighbor != null)
                {
                    door.SetDoorType(neighbor.information.type);
                }
            }
        }

        GenerateDiggingSpots();
    }

    private void GenerateDiggingSpots()
    {
        if (diggingSpotLocations == null || diggingSpotLocations.Length == 0 || diggingSpotPrefab == null) 
            return;

        // spawn de excavar
        if (Random.value * 100f <= chanceToHaveDiggingSpots)
        {
            int maxPossibleSpawns = Mathf.Min(2, diggingSpotLocations.Length);
            int numToSpawn = Random.Range(1, maxPossibleSpawns + 1); // numero de spawns

            List<Transform> availableLocations = new List<Transform>(diggingSpotLocations);

            for (int i = 0; i < numToSpawn; i++)
            {
                int index = Random.Range(0, availableLocations.Count);
                Transform spawnLoc = availableLocations[index];

                Instantiate(diggingSpotPrefab, spawnLoc.position, Quaternion.identity, transform);

                availableLocations.RemoveAt(index);
            }
        }
    }

    public void SpawnEnemies()
    {
        if (enemiesSpawned) return;

        if (enemyPrefab == null || enemySpawnPoints == null || enemySpawnPoints.Length == 0)
        {
            Debug.LogWarning("No hay configuración de enemigos en esta room");
            return;
        }

        enemiesAlive = 0;

        foreach (Transform point in enemySpawnPoints)
        {
            GameObject enemyGO = Instantiate(enemyPrefab, point.position, Quaternion.identity, transform);

            EnemyHealth enemyHealth = enemyGO.GetComponent<EnemyHealth>();

            if (enemyHealth != null)
            {
                enemiesAlive++;

                enemyHealth.OnDeath += HandleEnemyDeath;
            }
        }

        enemiesSpawned = true;

        if (enemiesAlive > 0)
        {
            roomInCombat = true;
            LockDoors();
        }
    }

    private void LockDoors()
    {
        foreach (var door in roomDoors)
        {
            if (door != null)
                door.SetLocked(true);
        }
    }

    public void UnlockDoorsInstant()
    {
        foreach (var door in roomDoors)
        {
            if (door != null)
                door.SetLocked(false);
        }
    }

    private void EndCombat()
    {
        roomInCombat = false;

        UnlockDoorsInstant();

        Debug.Log("Room completada");
    }

    private void HandleEnemyDeath()
    {
        enemiesAlive--;

        if (enemiesAlive <= 0)
        {
            EndCombat();
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

        if (diggingSpotLocations != null)
        {
            Gizmos.color = new Color(0.5f, 0.3f, 0.1f); 

            foreach (Transform spot in diggingSpotLocations)
            {
                if (spot != null)
                {
                    Gizmos.DrawSphere(spot.position, 0.12f);
                }
            }
        }
    }
}
