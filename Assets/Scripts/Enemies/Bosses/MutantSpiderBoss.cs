using System.Collections;
using UnityEngine;
using System.Collections.Generic;

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
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private EnemyHealth enemyHealth;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.55f;
    [SerializeField] private float chaseDuration = 7f;

    [Header("Poison Attack")]
    [SerializeField] private GameObject poisonTilePrefab;
    [SerializeField] private Transform[] poisonSpawnPoints;
    [SerializeField] private int minPoisonTilesPerWave = 12;
    [SerializeField] private int maxPoisonTilesPerWave = 16;
    [SerializeField] private float secondWaveDelay = 3f;
    [SerializeField] private float disappearDuration = 0.4f;
    [SerializeField] private float reappearDuration = 0.4f;
    [SerializeField] private float ceilingHeight = 8f;
    [SerializeField] private GameObject poisonWarningPrefab;
    [SerializeField] private float warningDuration = 0.8f;

    [Header("Web Attack")]
    [SerializeField] private GameObject webProjectilePrefab;
    [SerializeField] private Transform webFirePoint;
    [SerializeField] private int minWebShots = 8;
    [SerializeField] private int maxWebShots = 10;
    [SerializeField] private float delayBetweenWebShots = 0.5f;

    [Header("Victory")]
    [SerializeField] private GameObject victoryDoorPrefab;
    [SerializeField] private Transform[] victoryDoorSpawnPoints; 
    [SerializeField] private Transform entryDoorPoint;          
    [SerializeField] private Transform roomCenterPoint;          

    [Header("Victory - Rotación de puerta por pared (Z)")]
    [SerializeField] private float doorRotUp = 0f;
    [SerializeField] private float doorRotDown = 0f;
    [SerializeField] private float doorRotLeft = 90f;
    [SerializeField] private float doorRotRight = -90f;

    [Header("Melee")]
    [SerializeField] private EnemyAttack enemyAttack;

    private BossAttackType lastAttack;
    private int sameAttackCounter = 0;
    private bool hasLastAttack = false;

    private Vector2 lastMoveDirection = Vector2.down;
    private Vector3 groundedPosition;
    private bool isOnCeiling;
    private RigidbodyType2D storedBodyType;

    private Vector3 capturedEntryPosition;
    private bool hasCapturedEntry;

    protected override void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (bodyCollider == null)
            bodyCollider = GetComponent<Collider2D>();

        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();

        if (enemyAttack == null)
            enemyAttack = GetComponent<EnemyAttack>();

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (enemyHealth != null)
            enemyHealth.OnDeath += HandleDeath;

        ConfigurePlayerBlocking();
        base.Awake();
    }

    protected override IEnumerator BossRoutine()
    {
        CaptureEntryPosition();
        yield return new WaitForSeconds(1f);
        ConfigurePlayerBlocking();

        while (!isDead)
        {
            yield return ChasePlayer();
            StopMovement();

            yield return PauseBetweenAttacks();
            if (isDead) yield break;

            BossAttackType nextAttack = ChooseNextAttack();
            yield return ExecuteAttack(nextAttack);
            StopMovement();

            yield return PauseBetweenAttacks();
        }
    }

    private void CaptureEntryPosition()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (player != null)
        {
            capturedEntryPosition = player.position;
            hasCapturedEntry = true;
        }
    }

    private void ConfigurePlayerBlocking()
    {
        if (bodyCollider == null)
            return;

        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
            bodyCollider.excludeLayers &= ~(1 << playerLayer);

        CircleCollider2D circle = bodyCollider as CircleCollider2D;
        if (circle != null && circle.radius < 0.38f)
            circle.radius = 0.38f;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (player == null)
            return;

        Collider2D[] playerCols = player.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in playerCols)
        {
            if (col == null || col.isTrigger) continue;
            Physics2D.IgnoreCollision(bodyCollider, col, false);
        }
    }

    private void IgnorePlayerCollision()
    {
        ConfigurePlayerBlocking();
    }

    private IEnumerator ChasePlayer()
    {
        float timer = 0f;

        if (animator != null)
            animator.SetBool("IsMoving", true);

        AudioManager.PlayLoop(GameSfx.SpiderWalk);

        while (timer < chaseDuration && !isDead)
        {
            if (player == null)
            {
                AudioManager.StopLoop();
                yield break;
            }

            Vector2 toPlayer = (Vector2)player.position - rb.position;
            float distance = toPlayer.magnitude;
            Vector2 direction = distance > 0.001f ? toPlayer / distance : lastMoveDirection;

            float stopDistance = 1.25f;
            if (bodyCollider != null)
                stopDistance = Mathf.Max(stopDistance, bodyCollider.bounds.extents.magnitude * 0.65f + 0.55f);

            if (distance <= stopDistance)
                rb.linearVelocity = Vector2.zero;
            else
                rb.linearVelocity = direction * moveSpeed;
            UpdateMovementAnimation(direction);

            if (enemyAttack != null && !isOnCeiling && enemyAttack.TryAttack())
                AudioManager.PlaySpiderMelee();

            timer += Time.deltaTime;
            yield return null;
        }

        AudioManager.StopLoop();

        if (animator != null)
            animator.SetBool("IsMoving", false);

        StopMovement();
    }

    private void UpdateMovementAnimation(Vector2 direction)
    {
        if (animator == null) return;

        if (direction.sqrMagnitude <= 0.01f)
            return;

        direction.Normalize();

        Vector2 animDirection;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            animDirection = direction.x > 0 ? Vector2.right : Vector2.left;
        }
        else
        {
            animDirection = direction.y > 0 ? Vector2.up : Vector2.down;
        }

        lastMoveDirection = animDirection;

        animator.SetBool("IsMoving", true);
        animator.SetFloat("MoveX", lastMoveDirection.x);
        animator.SetFloat("MoveY", lastMoveDirection.y);
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
        groundedPosition = transform.position;

        SetCeilingState(true);

        if (animator != null)
            animator.SetTrigger("Disappear");

        Vector3 ceilingPosition = groundedPosition + Vector3.up * ceilingHeight;
        yield return MoveAndFade(groundedPosition, ceilingPosition, 1f, 0f, disappearDuration);
        transform.position = ceilingPosition;

        yield return new WaitForSeconds(0.35f);

        yield return SpawnPoisonWave();
        yield return new WaitForSeconds(secondWaveDelay);
        yield return SpawnPoisonWave();

        if (animator != null)
            animator.SetTrigger("Reappear");

        yield return MoveAndFade(transform.position, groundedPosition, 0f, 1f, reappearDuration);

        SetCeilingState(false);
    }

    private void SetCeilingState(bool onCeiling)
    {
        isOnCeiling = onCeiling;

        if (bodyCollider != null)
            bodyCollider.enabled = !onCeiling;

        if (enemyHealth != null)
            enemyHealth.SetDamageable(!onCeiling);

        if (rb != null)
        {
            if (onCeiling)
            {
                storedBodyType = rb.bodyType;
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
            else
            {
                rb.bodyType = storedBodyType;
            }
        }
    }

    private IEnumerator MoveAndFade(Vector3 from, Vector3 to, float startAlpha, float endAlpha, float duration)
    {
        float timer = 0f;
        Color baseColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        while (timer < duration && !isDead)
        {
            float t = duration <= 0f ? 1f : timer / duration;
            transform.position = Vector3.Lerp(from, to, t);

            if (spriteRenderer != null)
            {
                float alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            }

            timer += Time.deltaTime;
            yield return null;
        }

        transform.position = to;

        if (spriteRenderer != null)
            spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, endAlpha);
    }

    private IEnumerator SpawnPoisonWave()
    {
        if (poisonTilePrefab == null || poisonSpawnPoints == null || poisonSpawnPoints.Length == 0)
            yield break;

        int amount = Random.Range(minPoisonTilesPerWave, maxPoisonTilesPerWave + 1);
        amount = Mathf.Min(amount, poisonSpawnPoints.Length);

        Transform[] shuffledPoints = ShuffleSpawnPoints(poisonSpawnPoints);
        Transform roomParent = FindRoomParent();

        List<Transform> chosenPoints = new List<Transform>(amount);
        for (int i = 0; i < shuffledPoints.Length && chosenPoints.Count < amount; i++)
        {
            if (shuffledPoints[i] != null)
                chosenPoints.Add(shuffledPoints[i]);
        }

        if (chosenPoints.Count == 0)
            yield break;

        if (poisonWarningPrefab != null)
        {
            for (int i = 0; i < chosenPoints.Count; i++)
            {
                GameObject warning = Instantiate(poisonWarningPrefab, chosenPoints[i].position, Quaternion.identity);
                if (roomParent != null)
                    warning.transform.SetParent(roomParent, true);

                PoisonWarning warningScript = warning.GetComponent<PoisonWarning>();
                if (warningScript != null)
                    warningScript.Play(warningDuration);

                yield return new WaitForSeconds(0.03f);
            }

            yield return new WaitForSeconds(warningDuration);
        }

        AudioManager.PlaySfx(GameSfx.SpiderPoisonFloor);

        for (int i = 0; i < chosenPoints.Count; i++)
        {
            GameObject tile = Instantiate(poisonTilePrefab, chosenPoints[i].position, Quaternion.identity);
            if (roomParent != null)
                tile.transform.SetParent(roomParent, true);
        }
    }

    private IEnumerator WebShotAttack()
    {
        StopMovement();

        if (animator != null)
            animator.SetTrigger("WebAttack");

        int shots = Random.Range(minWebShots, maxWebShots + 1);
        Transform roomParent = FindRoomParent();

        for (int i = 0; i < shots && !isDead; i++)
        {
            ShootWeb(roomParent);
            yield return new WaitForSeconds(delayBetweenWebShots);
        }
    }

    private void ShootWeb(Transform roomParent)
    {
        if (webProjectilePrefab == null || player == null) return;

        Vector3 spawnPosition = webFirePoint != null ? webFirePoint.position : transform.position;

        GameObject webObject = Instantiate(webProjectilePrefab, spawnPosition, Quaternion.identity);
        if (roomParent != null)
            webObject.transform.SetParent(roomParent, true);

        WebProjectile projectile = webObject.GetComponent<WebProjectile>();

        if (projectile != null)
        {
            Vector2 direction = ((Vector2)player.position - (Vector2)spawnPosition).normalized;
            projectile.Initialize(direction);
        }

        AudioManager.PlaySfx(GameSfx.SpiderWebShot);
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

        if (animator != null)
            animator.SetBool("IsMoving", false);
    }

    private void HandleDeath()
    {
        AudioManager.StopLoop();
        StopBoss();
        StopMovement();

        if (isOnCeiling)
            SetCeilingState(false);

        RoomInstance room = GetComponentInParent<RoomInstance>();
        if (room != null)
            room.UnlockDoorsAnimated();

        SpawnVictoryDoor();
    }

    private void SpawnVictoryDoor()
    {
        if (victoryDoorPrefab == null)
            return;

        Transform spawnPoint = PickVictoryDoorPoint();

        Vector3 spawnPos = spawnPoint != null
            ? spawnPoint.position
            : transform.position + new Vector3(0, 2f, 0);

        Quaternion rot = spawnPoint != null
            ? GetDoorRotation(spawnPoint)
            : Quaternion.identity;

        GameObject door = Instantiate(victoryDoorPrefab, spawnPos, rot);

        Transform roomParent = FindRoomParent();
        if (roomParent != null)
            door.transform.SetParent(roomParent, true);
    }

    private Quaternion GetDoorRotation(Transform point)
    {
        Vector2 center = GetRoomCenter();
        Vector2 offset = (Vector2)point.position - center;

        float z;
        if (Mathf.Abs(offset.x) > Mathf.Abs(offset.y))
            z = offset.x > 0f ? doorRotRight : doorRotLeft;   // pared derecha / izquierda
        else
            z = offset.y > 0f ? doorRotUp : doorRotDown;       // pared arriba / abajo

        return Quaternion.Euler(0f, 0f, z);
    }

    private Vector2 GetRoomCenter()
    {
        if (roomCenterPoint != null)
            return roomCenterPoint.position;

        Transform room = FindRoomParent();
        return room != null ? (Vector2)room.position : (Vector2)transform.position;
    }
    private Transform PickVictoryDoorPoint()
    {
        List<Transform> all = new List<Transform>();
        if (victoryDoorSpawnPoints != null)
            foreach (Transform p in victoryDoorSpawnPoints)
                if (p != null) all.Add(p);

        if (all.Count == 0)
            return null;

        // posición de entrada: manual si está seteada, si no la capturada al empezar la pelea
        Vector2 entryPos = Vector2.zero;
        bool haveEntry = false;

        if (entryDoorPoint != null)
        {
            entryPos = entryDoorPoint.position;
            haveEntry = true;
        }
        else if (hasCapturedEntry)
        {
            entryPos = capturedEntryPosition;
            haveEntry = true;
        }

        // ancla más cercana a la entrada => esa se excluye
        Transform entrySide = null;
        if (haveEntry)
        {
            float best = float.MaxValue;
            foreach (Transform p in all)
            {
                float d = ((Vector2)p.position - entryPos).sqrMagnitude;
                if (d < best) { best = d; entrySide = p; }
            }
        }

        List<Transform> candidates = new List<Transform>();
        foreach (Transform p in all)
            if (p != entrySide) candidates.Add(p);

        if (candidates.Count == 0)
            candidates = all;

        return candidates[Random.Range(0, candidates.Count)];
    }

    private Transform FindRoomParent()
    {
        RoomInstance room = GetComponentInParent<RoomInstance>();
        if (room != null) return room.transform;

        Transform current = transform.parent;
        while (current != null)
        {
            if (current.GetComponent<RoomInstance>() != null)
                return current;

            current = current.parent;
        }

        return transform.parent;
    }
}
