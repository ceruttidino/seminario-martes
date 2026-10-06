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
    [SerializeField][Range(0f, 100f)] private float chanceToHaveDiggingSpots = 30f;
    private bool diggingSpotsGenerated = false;

    [Header("Enemies")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] enemySpawnPoints;

    [Header("Room Clear Reward")]
    [Tooltip("Si está asignada, reemplaza los valores locales de abajo.")] // NUEVO
    [SerializeField] private RoomRewardTable rewardTable;                 // NUEVO
    [SerializeField] private Transform rewardSpawnPoint;
    [SerializeField] private List<RoomReward> possibleRewards = new List<RoomReward>();
    [SerializeField][Range(0f, 100f)] private float chanceToSpawnReward = 40f;
    [Tooltip("Opcional: si el prefab elegido tiene UpgradePickup, se le asigna un buff random de esta lista.")]
    [SerializeField] private List<ObjectBuffSO> possibleRewardBuffs = new List<ObjectBuffSO>();

    private bool rewardSpawned = false;

    // NUEVO: valores efectivos (tabla compartida si está asignada, si no los locales de la room)
    private float RewardChance => rewardTable != null ? rewardTable.chanceToSpawnReward : chanceToSpawnReward;
    private List<RoomReward> Rewards => rewardTable != null ? rewardTable.rewards : possibleRewards;
    private List<ObjectBuffSO> RewardBuffs => rewardTable != null ? rewardTable.possibleBuffs : possibleRewardBuffs;

    private EnemyBehaviour[] currentEnemies;

    private bool enemiesSpawned = false;
    private bool combatActive = false;
    private bool checkingCombatClear = false;
    private bool awaitingEnemySpawn = false;


    private Dictionary<DoorDirection, Transform> spawnPointLookup = new Dictionary<DoorDirection, Transform>();
    private Dictionary<DoorDirection, RoomDoor> doorLookup = new Dictionary<DoorDirection, RoomDoor>();

    public Transform DefaultSpawnPoint => defaultSpawnPoint;
    public bool IsInCombat => combatActive || awaitingEnemySpawn || HasLivingEnemies();

    private RoomNode currentNode;
    private DoorDirection? entryDirection;

    private void Awake()
    {
        BuildLookups();

        if (GetComponent<RoomPathGrid>() == null)
            gameObject.AddComponent<RoomPathGrid>();
    }

    private void BuildLookups()
    {
        foreach (DoorSpawnPoint point in doorSpawnPoints)
        {
            if (point != null && point.spawnPoint != null)
            {
                spawnPointLookup.Add(point.direction, point.spawnPoint);
            }
        }
        foreach (RoomDoor door in roomDoors)
        {
            if (door != null && !doorLookup.ContainsKey(door.Direction))
            {
                doorLookup.Add(door.Direction, door);
            }
        }
    }

    public Transform GetSpawnPointFromEntry(DoorDirection entryDirection)
    {
        if (spawnPointLookup.TryGetValue(entryDirection, out Transform spawn))
        {
            return spawn;
        }
        return defaultSpawnPoint;
    }

    public void ConfigureDoors(RoomNode node, DoorDirection? forcedDoor = null)
    {
        currentNode = node;
        if (forcedDoor.HasValue)
            entryDirection = forcedDoor;

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

        if (node.information != null && node.information.type == RoomType.Boss)
            EnsureBossExitDoor();

        GenerateDiggingSpots();
    }

    public bool AnyDoorAnimating()
    {
        foreach (RoomDoor door in roomDoors)
        {
            if (door != null && door.IsAnimating)
                return true;
        }

        return false;
    }

    public Vector3 GetDoorWorldPosition(DoorDirection direction)
    {
        if (doorLookup.TryGetValue(direction, out RoomDoor door) && door != null)
            return door.transform.position;

        switch (direction)
        {
            case DoorDirection.Up: return transform.position + Vector3.up * 4.6f;
            case DoorDirection.Down: return transform.position + Vector3.down * 4.6f;
            case DoorDirection.Left: return transform.position + Vector3.left * 8.4f;
            default: return transform.position + Vector3.right * 8.4f;
        }
    }

    public DoorDirection GetBossExitDirection()
    {
        if (entryDirection.HasValue)
            return entryDirection.Value.Opposite();

        if (currentNode != null)
        {
            foreach (var pair in doorLookup)
            {
                if (currentNode.HasNeighbor(pair.Key))
                    return pair.Key.Opposite();
            }
        }

        return DoorDirection.Up;
    }

    private void EnsureBossExitDoor()
    {
        BossBase boss = GetComponentInChildren<BossBase>(true); // CAMBIADO: antes MutantSpiderBoss
        if (boss != null)
        {
            boss.EnsureExitDoor(GetBossExitDirection());
            return;
        }

        BossExitDoor existing = GetComponentInChildren<BossExitDoor>(true);
        if (existing == null)
        {
            GameObject go = new GameObject("BossExitDoor");
            go.transform.SetParent(transform, false);
            existing = go.AddComponent<BossExitDoor>();
        }

        existing.Place(GetDoorWorldPosition(GetBossExitDirection()), GetBossExitDirection(), transform.position);
        existing.SetOpenedImmediate();
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
        enemiesSpawned = true;

        foreach (EnemyHealth existing in GetComponentsInChildren<EnemyHealth>(true))
            RegisterEnemy(existing);

        if (enemyPrefab == null || enemySpawnPoints == null || enemySpawnPoints.Length == 0)
        {
            if (HasLivingEnemies())
            {
                combatActive = true;
                LockDoors();
            }
            return;
        }

        combatActive = true;
        awaitingEnemySpawn = true;
        LockDoors();
        StartCoroutine(SpawnEnemiesDelayed());
    }

    private IEnumerator SpawnEnemiesDelayed()
    {
        List<GameObject> heldExisting = new List<GameObject>();
        foreach (EnemyHealth existing in GetComponentsInChildren<EnemyHealth>(true))
        {
            if (existing == null || existing.GetComponent<BossBase>() != null)
                continue;

            EnemySummon.Hold(existing.gameObject);
            heldExisting.Add(existing.gameObject);
        }

        List<Transform> points = new List<Transform>();
        foreach (Transform point in enemySpawnPoints)
        {
            if (point != null)
                points.Add(point);
        }

        foreach (Transform point in points)
            EnemySummon.PlaySmoke(point.position);

        float start = Time.time;
        while (Time.time - start < EnemySummon.Delay || AnyDoorAnimating())
            yield return null;

        foreach (GameObject held in heldExisting)
        {
            if (held != null)
                EnemySummon.Release(held);
        }

        foreach (Transform point in points)
        {
            GameObject enemyGO = Instantiate(enemyPrefab, point.position, Quaternion.identity, transform);
            RegisterEnemy(enemyGO);
        }

        PoisonousSnake.EnsureNonSnakeCompany(this);

        awaitingEnemySpawn = false;

        if (!HasLivingEnemies())
            EndCombat();
    }

    public void SpawnEnemyWithSummon(GameObject prefab, Vector3 position)
    {
        if (prefab == null)
            return;

        combatActive = true;
        LockDoors();
        StartCoroutine(SpawnEnemyWithSummonRoutine(prefab, position));
    }

    private IEnumerator SpawnEnemyWithSummonRoutine(GameObject prefab, Vector3 position)
    {
        EnemySummon.PlaySmoke(position);
        yield return new WaitForSeconds(EnemySummon.Delay);

        GameObject enemy = Instantiate(prefab, position, Quaternion.identity, transform);
        RegisterSpawnedCombatEnemy(enemy);
    }

    public void RegisterEnemy(GameObject enemyGO)
    {
        if (enemyGO == null) return;

        foreach (EnemyHealth health in enemyGO.GetComponentsInChildren<EnemyHealth>(true))
            RegisterEnemy(health);
    }

    public void RegisterEnemy(EnemyHealth enemyHealth)
    {
        if (enemyHealth == null) return;

        enemyHealth.OnDeath -= HandleEnemyDeath;
        enemyHealth.OnDeath += HandleEnemyDeath;
    }

    public void RegisterSpawnedCombatEnemy(GameObject enemyGO)
    {
        RegisterEnemy(enemyGO);

        if (!HasLivingEnemies())
            return;

        combatActive = true;
        LockDoors();
    }

    public void InvalidatePathGrid()
    {
        RoomPathGrid grid = GetComponent<RoomPathGrid>();
        grid?.Invalidate();
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

    public void UnlockDoorsAnimated()
    {
        if (awaitingEnemySpawn || HasLivingEnemies())
            return;

        bool playBranchUnlock = false;

        foreach (var door in roomDoors)
        {
            if (door == null) continue;

            if (currentNode != null &&
                currentNode.information.type != RoomType.Shop &&
                door.currentDoorType == RoomType.Shop)
            {
                continue;
            }

            if (door.IsLocked && door.UsesBranchUnlockSfx)
                playBranchUnlock = true;
        }

        if (playBranchUnlock)
            AudioManager.PlaySfx(GameSfx.DoorUnlock);

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
        if (awaitingEnemySpawn)
            return;

        ChallengeRoomController challenge = GetComponent<ChallengeRoomController>();
        if (challenge != null && challenge.HoldsDoorsLocked)
            return;

        if (HasLivingEnemies())
            return;

        combatActive = false;

        TrySpawnClearReward();

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
        yield return null;
        checkingCombatClear = false;

        if (!awaitingEnemySpawn && !HasLivingEnemies())
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

    private void TrySpawnClearReward()
    {
        if (rewardSpawned) return;
        rewardSpawned = true;

        if (currentNode != null && currentNode.information != null)
        {
            RoomType type = currentNode.information.type;
            if (type != RoomType.Normal && type != RoomType.Start)
                return;
        }

        if (Rewards == null || Rewards.Count == 0) // CAMBIADO: antes possibleRewards
            return;

        if (Random.value * 100f > RewardChance) // CAMBIADO: antes chanceToSpawnReward
            return;

        GameObject prefab = PickWeightedReward();
        if (prefab == null) return;

        Vector3 spawnPosition = rewardSpawnPoint != null
            ? rewardSpawnPoint.position
            : transform.position;

        GameObject spawned = Instantiate(prefab, spawnPosition, Quaternion.identity, transform);

        ConfigureRewardBuff(spawned);
    }

    private GameObject PickWeightedReward()
    {
        float totalWeight = 0f;

        foreach (RoomReward reward in Rewards) // CAMBIADO: antes possibleRewards
        {
            if (reward == null || reward.prefab == null) continue;
            totalWeight += Mathf.Max(0f, reward.weight);
        }

        if (totalWeight <= 0f) return null;

        float roll = Random.Range(0f, totalWeight);

        foreach (RoomReward reward in Rewards) // CAMBIADO: antes possibleRewards
        {
            if (reward == null || reward.prefab == null) continue;

            float weight = Mathf.Max(0f, reward.weight);
            if (weight <= 0f) continue;

            if (roll < weight)
                return reward.prefab;

            roll -= weight;
        }

        return null;
    }

    private void ConfigureRewardBuff(GameObject spawned)
    {
        if (spawned == null) return;

        UpgradePickup pickup = spawned.GetComponent<UpgradePickup>();
        if (pickup == null) return;

        if (RewardBuffs == null || RewardBuffs.Count == 0) // CAMBIADO: antes possibleRewardBuffs
            return;

        ObjectBuffSO chosen = BuffPool.PickRandom(RewardBuffs); // CAMBIADO: antes possibleRewardBuffs

        if (chosen == null)
        {
            Destroy(spawned);
            return;
        }

        pickup.SetUpgrade(chosen);

        if (chosen.icon != null)
        {
            SpriteRenderer sr = spawned.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
                sr.sprite = chosen.icon;
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

        if (rewardSpawnPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(rewardSpawnPoint.position, 0.25f);
        }
    }
}

