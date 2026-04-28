using System.Collections;
using UnityEngine;

public class MutantSpiderBoss : BossBase
{
    private enum BossAttackType
    {
        PoisonTiles,
        WebShots
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.2f;
    [SerializeField] private float chaseDuration = 7f;

    [Header("Poison Attack")]
    [SerializeField] private GameObject poisonTilePrefab;
    [SerializeField] private Transform[] poisonSpawnPoints;
    [SerializeField] private int minPoisonTilesPerWave = 12;
    [SerializeField] private int maxPoisonTilesPerWave = 16;
    [SerializeField] private float secondWaveDelay = 3f;
    [SerializeField] private float disappearDuration = 0.4f;
    [SerializeField] private float reappearDuration = 0.4f;

    [Header("Web Attack")]
    [SerializeField] private GameObject webProjectilePrefab;
    [SerializeField] private Transform webFirePoint;
    [SerializeField] private int minWebShots = 8;
    [SerializeField] private int maxWebShots = 10;
    [SerializeField] private float delayBetweenWebShots = 0.5f;

    [Header("Collision Attack")]
    [SerializeField] private EnemyAttack enemyAttack;

    [Header("Victory")]
    [SerializeField] private GameObject victoryDoorPrefab;
    [SerializeField] private Transform doorSpawnPoint;

    [Header("Chase Settings")]
    [SerializeField] private float stoppingDistance = 1.4f;
    [SerializeField] private float attackStartDistance = 1.6f;

    private BossAttackType lastAttack;
    private int sameAttackCounter = 0;
    private bool hasLastAttack = false;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        EnemyHealth health = GetComponent<EnemyHealth>();
        if (health != null)
            health.OnDeath += HandleDeath;

        if (enemyAttack == null)
            enemyAttack = GetComponent<EnemyAttack>();
    }

    protected override IEnumerator BossRoutine()
    {
        yield return new WaitForSeconds(1f);

        while (!isDead)
        {
            yield return ChasePlayer();

            StopMovement();

            yield return new WaitForSeconds(0.35f);

            BossAttackType nextAttack = ChooseNextAttack();

            yield return ExecuteAttack(nextAttack);

            StopMovement();

            yield return PauseBetweenAttacks();
        }
    }

    private IEnumerator ChasePlayer()
    {
        float timer = 0f;

        if (animator != null)
            animator.SetBool("IsMoving", true);

        while (timer < chaseDuration)
        {
            if (player == null) yield break;

            Vector2 toPlayer = (Vector2)player.position - rb.position;
            float distanceToPlayer = toPlayer.magnitude;

            if (distanceToPlayer <= stoppingDistance)
            {
                StopMovement();

                if (enemyAttack != null)
                    enemyAttack.TryAttack();

                break;
            }

            Vector2 direction = toPlayer.normalized;
            rb.linearVelocity = direction * moveSpeed;

            timer += Time.deltaTime;
            yield return null;
        }

        if (animator != null)
            animator.SetBool("IsMoving", false);

        StopMovement();
    }

    private BossAttackType ChooseNextAttack()
    {
        BossAttackType chosenAttack;

        if (!hasLastAttack)
        {
            chosenAttack = RandomAttack();
        }
        else if (sameAttackCounter >= 2)
        {
            chosenAttack = GetDifferentAttack(lastAttack);
        }
        else
        {
            chosenAttack = RandomAttack();
        }

        if (hasLastAttack && chosenAttack == lastAttack)
        {
            sameAttackCounter++;
        }
        else
        {
            sameAttackCounter = 1;
            lastAttack = chosenAttack;
            hasLastAttack = true;
        }

        return chosenAttack;
    }

    private BossAttackType RandomAttack()
    {
        return Random.Range(0, 2) == 0 ? BossAttackType.PoisonTiles : BossAttackType.WebShots;
    }

    private BossAttackType GetDifferentAttack(BossAttackType attack)
    {
        return attack == BossAttackType.PoisonTiles ? BossAttackType.WebShots : BossAttackType.PoisonTiles;
    }

    private IEnumerator ExecuteAttack(BossAttackType attackType)
    {
        switch (attackType)
        {
            case BossAttackType.PoisonTiles:
                yield return PoisonTileAttack();
                break;

            case BossAttackType.WebShots:
                yield return WebShotAttack();
                break;
        }
    }

    private IEnumerator PoisonTileAttack()
    {
        StopMovement();

        if (animator != null)
            animator.SetTrigger("Disappear");

        yield return FadeSprite(1f, 0f, disappearDuration);

        yield return SpawnPoisonWave();

        yield return new WaitForSeconds(secondWaveDelay);

        yield return SpawnPoisonWave();

        if (animator != null)
            animator.SetTrigger("Reappear");

        yield return FadeSprite(0f, 1f, reappearDuration);
    }

    private IEnumerator SpawnPoisonWave()
    {
        if (poisonTilePrefab == null || poisonSpawnPoints == null || poisonSpawnPoints.Length == 0)
        {
            Debug.LogWarning("Faltan poisonTilePrefab o poisonSpawnPoints");
            yield break;
        }

        int amount = Random.Range(minPoisonTilesPerWave, maxPoisonTilesPerWave + 1);
        amount = Mathf.Min(amount, poisonSpawnPoints.Length);

        Transform[] shuffledPoints = ShuffleSpawnPoints(poisonSpawnPoints);

        for (int i = 0; i < amount; i++)
        {
            Instantiate(poisonTilePrefab, shuffledPoints[i].position, Quaternion.identity);
            yield return new WaitForSeconds(0.03f);
        }
    }

    private IEnumerator WebShotAttack()
    {
        StopMovement();

        if (animator != null)
            animator.SetTrigger("WebAttack");

        int shots = Random.Range(minWebShots, maxWebShots + 1);

        for (int i = 0; i < shots; i++)
        {
            ShootWeb();
            yield return new WaitForSeconds(delayBetweenWebShots);
        }
    }

    private void ShootWeb()
    {
        if (webProjectilePrefab == null || player == null) return;

        Vector3 spawnPosition = webFirePoint != null ? webFirePoint.position : transform.position;

        GameObject webObject = Instantiate(webProjectilePrefab, spawnPosition, Quaternion.identity);

        WebProjectile projectile = webObject.GetComponent<WebProjectile>();

        if (projectile != null)
        {
            Vector2 direction = ((Vector2)player.position - (Vector2)spawnPosition).normalized;
            projectile.Initialize(direction);
        }
    }

    private IEnumerator FadeSprite(float startAlpha, float endAlpha, float duration)
    {
        if (spriteRenderer == null) yield break;

        float timer = 0f;
        Color baseColor = spriteRenderer.color;

        while (timer < duration)
        {
            float alpha = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            timer += Time.deltaTime;
            yield return null;
        }

        spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, endAlpha);
    }

    private Transform[] ShuffleSpawnPoints(Transform[] original)
    {
        Transform[] shuffled = new Transform[original.Length];
        original.CopyTo(shuffled, 0);

        for (int i = 0; i < shuffled.Length; i++)
        {
            int randomIndex = Random.Range(i, shuffled.Length);
            Transform temp = shuffled[i];
            shuffled[i] = shuffled[randomIndex];
            shuffled[randomIndex] = temp;
        }

        return shuffled;
    }

    private void StopMovement()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private void HandleDeath()
    {
        StopBoss();
        StopMovement();

        SpawnVictoryDoor();
    }

    private void SpawnVictoryDoor()
    {
        if (victoryDoorPrefab == null)
        {
            return;
        }

        Vector3 spawnPos = doorSpawnPoint != null
            ? doorSpawnPoint.position
            : transform.position + new Vector3(0, 2f, 0);

        GameObject door = Instantiate(victoryDoorPrefab, spawnPos, Quaternion.identity);

        Transform roomParent = FindRoomParent();
        if (roomParent != null)
        {
            door.transform.SetParent(roomParent, true);
        }

    }

    private Transform FindRoomParent()
    {
        Transform current = transform.parent;

        while (current != null)
        {
            string name = current.name.ToLower();

            if (name.Contains("room_boss") || name.Contains("boss") ||
                name.Contains("room") || current.GetComponent("Room") != null)
            {
                return current;
            }

            current = current.parent;
        }

        return null;
    }
}