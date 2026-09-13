using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChallengeRoomController : MonoBehaviour
{
    [Header("Waves")]
    [SerializeField] private int waveCount = 3;
    [SerializeField] private int enemiesPerWave = 4;
    [SerializeField] private float delayBetweenWaves = 1.75f;
    [SerializeField] private float enemySpeedMultiplier = 0.6f;
    [SerializeField] private List<GameObject> waveEnemyPrefabs = new List<GameObject>();

    [Header("Rewards")]
    [SerializeField] private List<ObjectBuffSO> possibleBuffs = new List<ObjectBuffSO>();
    [SerializeField] private GameObject buffPickupPrefab;
    [SerializeField] private GameObject heartPickupPrefab;
    [SerializeField] private LootItem heartLoot;
    [SerializeField] [Range(0f, 100f)] private float heartDropChance = 30f;

    private RoomInstance roomInstance;
    private Supercontainer container;
    private readonly List<GameObject> livingEnemies = new List<GameObject>();
    private readonly List<GameObject> remainingEnemyPrefabs = new List<GameObject>();

    private bool challengeStarted;
    private bool challengeFinished;

    public bool HoldsDoorsLocked => challengeStarted && !challengeFinished;

    private void Awake()
    {
        roomInstance = GetComponent<RoomInstance>();
        if (roomInstance == null)
            roomInstance = GetComponentInParent<RoomInstance>();

        container = GetComponentInChildren<Supercontainer>(true);
    }

    public void StartChallenge()
    {
        if (challengeStarted || challengeFinished)
            return;

        if (ChallengeRunState.WasCompleted(ChallengeRunState.Supercontainer))
        {
            roomInstance?.UnlockDoorsInstant();
            return;
        }

        challengeStarted = true;
        remainingEnemyPrefabs.Clear();
        remainingEnemyPrefabs.AddRange(waveEnemyPrefabs.FindAll(prefab => prefab != null));

        if (container != null)
            container.Destroyed += FailChallenge;

        roomInstance?.LockDoors();
        StartCoroutine(RunWaves());
    }

    public void CompleteChallenge()
    {
        if (!challengeStarted || challengeFinished)
            return;

        Finish(won: true);
    }

    public bool IsCompleted()
    {
        return challengeFinished;
    }

    private IEnumerator RunWaves()
    {
        for (int wave = 0; wave < waveCount; wave++)
        {
            if (challengeFinished)
                yield break;

            SpawnWave();

            while (!challengeFinished && HasLivingEnemies())
                yield return null;

            if (challengeFinished)
                yield break;

            if (wave < waveCount - 1)
                yield return new WaitForSeconds(delayBetweenWaves);
        }

        if (!challengeFinished)
            Finish(won: true);
    }

    private void SpawnWave()
    {
        if (remainingEnemyPrefabs.Count == 0 || container == null)
            return;

        int index = Random.Range(0, remainingEnemyPrefabs.Count);
        GameObject prefab = remainingEnemyPrefabs[index];
        remainingEnemyPrefabs.RemoveAt(index);

        Transform parent = roomInstance != null ? roomInstance.transform : transform;
        Vector3[] spawnPositions = GetInsideSpawnPositions(enemiesPerWave);

        for (int i = 0; i < enemiesPerWave; i++)
        {
            Vector3 spawnPos = spawnPositions[i];
            GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity, parent);

            ChallengeContainerHunter hunter = enemy.GetComponent<ChallengeContainerHunter>();
            if (hunter == null)
                hunter = enemy.AddComponent<ChallengeContainerHunter>();

            hunter.Setup(container, enemySpeedMultiplier);
            livingEnemies.Add(enemy);

            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health != null)
                health.OnDeath += OnEnemyDied;
        }
    }

    private Vector3[] GetInsideSpawnPositions(int count)
    {
        Vector3 center = container.transform.position;
        List<Vector3> anchors = new List<Vector3>();

        if (roomInstance != null)
        {
            AddInsetSpawn(anchors, roomInstance.GetSpawnPointFromEntry(DoorDirection.Up), center);
            AddInsetSpawn(anchors, roomInstance.GetSpawnPointFromEntry(DoorDirection.Down), center);
            AddInsetSpawn(anchors, roomInstance.GetSpawnPointFromEntry(DoorDirection.Left), center);
            AddInsetSpawn(anchors, roomInstance.GetSpawnPointFromEntry(DoorDirection.Right), center);
        }

        if (anchors.Count == 0)
        {
            anchors.Add(center + Vector3.up * 2.2f);
            anchors.Add(center + Vector3.down * 2.2f);
            anchors.Add(center + Vector3.left * 5f);
            anchors.Add(center + Vector3.right * 5f);
        }

        Vector3[] positions = new Vector3[count];
        int start = Random.Range(0, anchors.Count);

        for (int i = 0; i < count; i++)
            positions[i] = PullInsideIfBlocked(anchors[(start + i) % anchors.Count], center);

        return positions;
    }

    private static void AddInsetSpawn(List<Vector3> anchors, Transform spawn, Vector3 center)
    {
        if (spawn == null) return;

        Vector3 pos = Vector3.Lerp(spawn.position, center, 0.22f);
        if ((pos - center).sqrMagnitude < 1f)
            return;

        anchors.Add(pos);
    }

    private static Vector3 PullInsideIfBlocked(Vector3 desired, Vector3 center)
    {
        Vector3 pos = desired;
        for (int step = 0; step < 6; step++)
        {
            if (!IsBlocked(pos))
                return pos;

            pos = Vector3.Lerp(pos, center, 0.25f);
        }

        return pos;
    }

    private static bool IsBlocked(Vector3 worldPos)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, 0.35f);
        foreach (Collider2D hit in hits)
        {
            if (hit == null || hit.isTrigger) continue;
            if (hit.GetComponent<Supercontainer>() != null) continue;
            if (hit.GetComponent<EnemyHealth>() != null) continue;
            return true;
        }

        return false;
    }

    private void OnEnemyDied()
    {
        livingEnemies.RemoveAll(enemy => enemy == null);
    }

    private bool HasLivingEnemies()
    {
        livingEnemies.RemoveAll(enemy => enemy == null);
        foreach (GameObject enemy in livingEnemies)
        {
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health != null && !health.IsDead)
                return true;
        }

        return false;
    }

    private void FailChallenge()
    {
        if (challengeFinished) return;
        Finish(won: false);
    }

    private void Finish(bool won)
    {
        if (challengeFinished) return;

        challengeFinished = true;
        ChallengeRunState.MarkCompleted(ChallengeRunState.Supercontainer);

        if (container != null)
            container.Destroyed -= FailChallenge;

        if (!won)
        {
            KillRemainingEnemies();
            roomInstance?.UnlockDoorsInstant();
        }
        else
        {
            if (container != null && !container.IsDestroyed)
            {
                container.MarkOpened();
                DropRewards();
            }

            roomInstance?.UnlockDoorsAnimated();
        }
    }

    private void KillRemainingEnemies()
    {
        for (int i = livingEnemies.Count - 1; i >= 0; i--)
        {
            GameObject enemy = livingEnemies[i];
            if (enemy == null) continue;
            Destroy(enemy);
        }

        livingEnemies.Clear();
    }

    private void DropRewards()
    {
        Transform parent = roomInstance != null ? roomInstance.transform : transform;
        Vector3 origin = container != null ? container.transform.position : transform.position;

        ObjectBuffSO buff = BuffPool.PickRandom(possibleBuffs);
        if (buff != null && buffPickupPrefab != null)
        {
            GameObject pickup = Instantiate(buffPickupPrefab, origin, Quaternion.identity, parent);
            UpgradePickup upgradePickup = pickup.GetComponent<UpgradePickup>();
            if (upgradePickup != null)
                upgradePickup.SetUpgrade(buff);

            if (buff.icon != null)
            {
                SpriteRenderer sr = pickup.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                    sr.sprite = buff.icon;
            }
        }

        if (heartPickupPrefab != null && Random.Range(0f, 100f) <= heartDropChance)
        {
            int hearts = Random.Range(1, 3);
            for (int i = 0; i < hearts; i++)
            {
                Vector3 offset = Quaternion.Euler(0f, 0f, 90f * i) * Vector3.right * 0.55f;
                GameObject heart = Instantiate(heartPickupPrefab, origin + offset, Quaternion.identity, parent);
                LootPickup loot = heart.GetComponent<LootPickup>();
                if (loot != null && heartLoot != null)
                    loot.SetLootItem(heartLoot);
            }
        }
    }
}
