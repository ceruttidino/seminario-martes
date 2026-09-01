using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
    private bool diggingSpotsGenerated = false;

    [Header("Enemies")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] enemySpawnPoints;

    private EnemyBehaviour[] currentEnemies;

    private bool enemiesSpawned = false;
    private bool combatActive = false;
    private bool checkingCombatClear = false;


    private Dictionary<DoorDirection, Transform> spawnPointLookup = new Dictionary<DoorDirection, Transform>();
    private Dictionary<DoorDirection, RoomDoor> doorLookup = new Dictionary<DoorDirection, RoomDoor>();

    public Transform DefaultSpawnPoint => defaultSpawnPoint;

    private RoomNode currentNode;

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
        currentNode = node;

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
                door.Initialize(node, direction);

                RoomNode neighbor = node.GetNeighbor(direction);

                if (node.information.type == RoomType.Boss)
                {
                    door.SetDoorType(RoomType.Boss, neighbor != null ? neighbor : node);
                    door.SetLocked(true);
                }
                else if (neighbor != null)
                {
                    door.SetDoorType(neighbor.information.type, neighbor);
                }

                if (node.information.type == RoomType.Shop)
                {
                    door.SetLocked(false);
                }
                else if (node.isShopUnlocked)
                {
                    door.SetLocked(false);
                }
            }
        }

        if (combatActive || HasLivingEnemies())
        {
            combatActive = true;
            LockDoors(instant: true);
        }

        GenerateDiggingSpots();
    }

    private void GenerateDiggingSpots()
    {
        if (diggingSpotsGenerated) return;
        diggingSpotsGenerated = true;

        if (diggingSpotLocations == null || diggingSpotLocations.Length == 0 || diggingSpotPrefab == null) 
            return;

        if (Random.value * 100f <= chanceToHaveDiggingSpots)
        {
            int maxPossibleSpawns = Mathf.Min(2, diggingSpotLocations.Length);
            int numToSpawn = Random.Range(1, maxPossibleSpawns + 1);

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

        // Start, Shop, Boss, Connection y Challenge no spawnean enemigos por este
        // sistema. Si el prefab o los puntos faltan / están vacíos, simplemente no
        // hay combate: no es un error.
        if (enemyPrefab == null || enemySpawnPoints == null || enemySpawnPoints.Length == 0)
            return;

        foreach (Transform point in enemySpawnPoints)
        {
            if (point == null)
                continue;

            GameObject enemyGO = Instantiate(enemyPrefab, point.position, Quaternion.identity, transform);
            RegisterEnemy(enemyGO);
        }

        foreach (EnemyHealth existing in GetComponentsInChildren<EnemyHealth>(true))
            RegisterEnemy(existing);

        enemiesSpawned = true;

        if (HasLivingEnemies())
        {
            combatActive = true;
            LockDoors();
        }
    }

    private void RegisterEnemy(GameObject enemyGO)
    {
        if (enemyGO == null) return;

        foreach (EnemyHealth health in enemyGO.GetComponentsInChildren<EnemyHealth>(true))
            RegisterEnemy(health);
    }

    private void RegisterEnemy(EnemyHealth enemyHealth)
    {
        if (enemyHealth == null) return;

        enemyHealth.OnDeath -= HandleEnemyDeath;
        enemyHealth.OnDeath += HandleEnemyDeath;
    }

    public void LockDoors()
    {
        LockDoors(false);
    }

    public void LockDoors(bool instant)
    {
        foreach (var door in roomDoors)
        {
            if (door == null) continue;

            if (instant)
                door.SetLocked(true);
            else
                door.PlayLockAnimation();
        }
    }

    public void UnlockDoorsInstant()
    {
        foreach (var door in roomDoors)
        {
            if (door == null) continue;

            if (currentNode != null &&
                currentNode.information.type != RoomType.Shop &&
                door.currentDoorType == RoomType.Shop)
            {
                continue;
            }

            door.SetLocked(false);
        }
    }

    // Igual que UnlockDoorsInstant, pero reproduciendo la animacion de apertura
    // (combate, sin candado). Se usa al limpiar la sala de enemigos, que es el
    // unico caso donde el desbloqueo ocurre con el jugador mirando. La Shop no
    // se abre aca: sigue pidiendo ganzua.
    public void UnlockDoorsAnimated()
    {
        if (HasLivingEnemies())
            return;

        foreach (var door in roomDoors)
        {
            if (door == null) continue;

            if (currentNode != null &&
                currentNode.information.type != RoomType.Shop &&
                door.currentDoorType == RoomType.Shop)
            {
                continue;
            }

            door.PlayUnlockAnimation();
        }
    }

    private void EndCombat()
    {
        if (HasLivingEnemies())
            return;

        combatActive = false;
        UnlockDoorsAnimated();
    }

    public void HandleEnemyDeath()
    {
        if (checkingCombatClear) return;
        StartCoroutine(CheckCombatClearedNextFrame());
    }

    private IEnumerator CheckCombatClearedNextFrame()
    {
        checkingCombatClear = true;
        yield return null;
        checkingCombatClear = false;

        if (!HasLivingEnemies())
            EndCombat();
    }

    public bool HasLivingEnemies()
    {
        EnemyHealth[] healths = GetComponentsInChildren<EnemyHealth>(true);
        foreach (EnemyHealth health in healths)
        {
            if (health != null && !health.IsDead)
                return true;
        }

        if (GetComponentInChildren<RatBody>(true) != null)
            return true;

        return false;
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
