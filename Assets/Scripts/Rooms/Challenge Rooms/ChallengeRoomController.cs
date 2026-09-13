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
    [SerializeField] private float spawnRadius = 5.5f;
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

        Vector3 center = container.transform.position;
        float startAngle = Random.Range(0f, 360f);

        for (int i = 0; i < enemiesPerWave; i++)
        {
            float angle = (startAngle + 360f * i / enemiesPerWave) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * spawnRadius;
            Transform parent = roomInstance != null ? roomInstance.transform : transform;
            GameObject enemy = Instantiate(prefab, center + offset, Quaternion.identity, parent);

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
