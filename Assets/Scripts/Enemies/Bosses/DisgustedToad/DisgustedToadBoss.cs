using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisgustedToadBoss : BossBase
{
    private enum ToadAttackType { TongueLash, SlimeBurst }

    [Header("Debug")]                                      
    [SerializeField] private bool logAttackChoice = true;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private EnemyHealth enemyHealth;

    [Header("Ataque 1 - Saltos")]
    [SerializeField] private int contactDamage = 1;                 
    [SerializeField] private float contactRangeTiles = 0.4f;        
    [SerializeField] private float landingPushMargin = 0.35f;       

    [Header("Visual")]
    [SerializeField] private Transform visual;
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform shadow;
    [SerializeField] private Transform mouthPoint;
    [SerializeField] private bool flipSpriteHorizontally = true;
    [SerializeField] private int airSortingBoost = 20;

    [Header("Grid")]
    [Tooltip("Cuántas unidades de Unity mide 1 tile.")]
    [SerializeField] private float tileSize = 1f;

    [Header("Ataque 1 - Saltos")]
    [SerializeField] private float chaseDuration = 7f;
    [SerializeField] private float hopWindup = 0.3f;
    [SerializeField] private float hopAirTime = 0.55f;
    [SerializeField] private float hopHeight = 1.6f;
    [SerializeField] private float maxHopDistanceTiles = 3.5f;
    [SerializeField] private float landingRecovery = 0.35f;
    [SerializeField] private float landingDamageRadiusTiles = 1.1f;
    [SerializeField] private int landingDamage = 1;
    [SerializeField] private bool invulnerableInAir = true;

    [Header("Ataque 2 - Lengua")]
    [SerializeField] private ToadTongue tongue;
    [SerializeField] private float tongueMinDistanceTiles = 10f;
    [SerializeField] private int tongueRepeats = 3;
    [SerializeField] private float tongueTellDuration = 0.9f;
    [SerializeField] private float delayBetweenTongues = 0.6f;

    [Header("Ataque 3 - Moco")]
    [SerializeField] private ToadSlimeGlob slimeGlobPrefab;
    [SerializeField] private SlimePuddle slimePuddlePrefab;
    [Tooltip("Opcional: círculo de aviso (podés reusar el prefab de PoisonWarning).")]
    [SerializeField] private GameObject slimeWarningPrefab;
    [SerializeField] private float slimeRadiusTiles = 5f;
    [SerializeField] private float slimeTellDuration = 0.7f;
    [SerializeField] private float slimeFlightTime = 0.8f;
    [SerializeField] private float afterSpitRecovery = 0.6f;

    private Vector3 baseVisualPos = Vector3.zero;
    private Vector3 baseVisualScale = Vector3.one;
    private Quaternion baseVisualRot = Quaternion.identity;
    private Vector3 baseShadowScale = Vector3.one;
    private RigidbodyType2D storedBodyType;
    private bool isAirborne;
    private readonly HashSet<string> animatorParams = new HashSet<string>();

    protected override void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
        if (enemyHealth == null) enemyHealth = GetComponent<EnemyHealth>();
        if (visual == null) visual = transform.Find("Visual");
        if (visual != null && visualRenderer == null) visualRenderer = visual.GetComponent<SpriteRenderer>();
        if (visual != null && animator == null) animator = visual.GetComponent<Animator>();
        if (tongue == null) tongue = GetComponentInChildren<ToadTongue>(true);

        FindPlayer();

        if (visual != null)
        {
            baseVisualPos = visual.localPosition;
            baseVisualScale = visual.localScale;
            baseVisualRot = visual.localRotation;
        }
        if (shadow != null) baseShadowScale = shadow.localScale;
        if (rb != null) storedBodyType = rb.bodyType;

        CacheAnimatorParams();

        if (enemyHealth != null)
            enemyHealth.OnDeath += HandleDeath;

        base.Awake();
    }

    private void OnDestroy()
    {
        if (enemyHealth != null)
            enemyHealth.OnDeath -= HandleDeath;
    }

    private void FindPlayer()
    {
        if (player != null) return;
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
    }

    protected override IEnumerator BossRoutine()
    {
        // El summon de BossBase re-activa todos los sprites/colliders hijos: volvemos a esconder la lengua
        if (tongue != null) tongue.Hide();

        FindPlayer();
        yield return new WaitForSeconds(1f);

        while (!isDead)
        {
            yield return HopChase();
            StopMovement();

            yield return PauseBetweenAttacks();
            if (isDead) yield break;

            if (ChooseNextAttack() == ToadAttackType.TongueLash)
                yield return TongueAttack();
            else
                yield return SlimeAttack();

            StopMovement();
            yield return PauseBetweenAttacks();
        }
    }

    private ToadAttackType ChooseNextAttack()
    {
        if (player == null || tongue == null)
        {
            if (logAttackChoice) // NUEVO
                Debug.Log($"[Toad] Baba: {(player == null ? "no encontró al jugador" : "no tiene ToadTongue asignada")}");
            return ToadAttackType.SlimeBurst;
        }

        float distance = Vector2.Distance(rb.position, player.position);
        float threshold = tongueMinDistanceTiles * tileSize;                                                     
        ToadAttackType chosen = distance >= threshold ? ToadAttackType.TongueLash : ToadAttackType.SlimeBurst;   

        if (logAttackChoice) // NUEVO
            Debug.Log($"[Toad] Distancia: {distance:F1} u ({distance / tileSize:F1} tiles). Lengua desde {threshold:F1} u → {chosen}");

        return chosen;
    }

    // ───── Ataque 1: saltos ─────

    private IEnumerator HopChase()
    {
        float endTime = Time.time + chaseDuration;

        while (Time.time < endTime && !isDead)
        {
            if (player == null) yield break;

            yield return Hop();
            if (isDead) yield break;

            float t = 0f;
            while (t < landingRecovery && !isDead)
            {
                TryContactDamage();
                t += Time.deltaTime;
                yield return null;
            }
        }
    }

    private IEnumerator Hop()
    {
        Vector2 start = rb.position;
        Vector2 toPlayer = (Vector2)player.position - start;
        float maxDistance = maxHopDistanceTiles * tileSize;
        Vector2 target = toPlayer.magnitude > maxDistance
            ? start + toPlayer.normalized * maxDistance
            : (Vector2)player.position;

        Face(toPlayer);

        // Se agacha
        AnimTrigger("HopPrep");
        float t = 0f;
        while (t < hopWindup && !isDead)
        {
            float k = t / hopWindup;
            SetVisualScale(new Vector3(1f + 0.25f * k, 1f - 0.25f * k, 1f));
            t += Time.deltaTime;
            yield return null;
        }
        if (isDead) yield break;

        // En el aire: sin collider, el jugador pasa por abajo
        SetAirborne(true);
        AnimBool("InAir", true);

        t = 0f;
        while (t < hopAirTime && !isDead)
        {
            float k = t / hopAirTime;
            float height = 4f * hopHeight * k * (1f - k);

            rb.MovePosition(Vector2.Lerp(start, target, k));

            if (visual != null)
            {
                visual.localPosition = baseVisualPos + Vector3.up * height;
                SetVisualScale(new Vector3(0.85f, 1.2f, 1f));
            }
            if (shadow != null)
                shadow.localScale = baseShadowScale * Mathf.Lerp(1f, 0.6f, hopHeight > 0f ? height / hopHeight : 0f);

            t += Time.deltaTime;
            yield return null;
        }

        rb.position = target;
        transform.position = target;
        ResetVisual();
        AnimBool("InAir", false);
        AnimTrigger("Land");
        SetAirborne(false);

        TryLandingDamage(target);

        // Aplaste al caer
        t = 0f;
        const float squashTime = 0.12f;
        while (t < squashTime && !isDead)
        {
            float k = 1f - t / squashTime;
            SetVisualScale(new Vector3(1f + 0.3f * k, 1f - 0.3f * k, 1f));
            t += Time.deltaTime;
            yield return null;
        }
        ResetVisual();
    }

    private void SetAirborne(bool airborne)
    {
        if (isAirborne == airborne) return;
        isAirborne = airborne;

        if (bodyCollider != null) bodyCollider.enabled = !airborne;
        if (invulnerableInAir && enemyHealth != null) enemyHealth.SetDamageable(!airborne);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            if (airborne) { storedBodyType = rb.bodyType; rb.bodyType = RigidbodyType2D.Kinematic; }
            else rb.bodyType = storedBodyType;
        }

        if (visualRenderer != null)
            visualRenderer.sortingOrder += airborne ? airSortingBoost : -airSortingBoost;

        if (shadow != null && !airborne) shadow.localScale = baseShadowScale;
    }

    private void TryLandingDamage(Vector2 landPosition)
    {
        if (player == null) return;

        float distance = Vector2.Distance(landPosition, player.position);
        if (distance > landingDamageRadiusTiles * tileSize) return;

        if (landingDamage > 0)
            BossPlayerUtils.DamagePlayer(player.gameObject, landingDamage);

        PushPlayerOut(landPosition); // que no quede atrapado debajo del sapo
    }

    private void TryContactDamage()
    {
        if (player == null || contactDamage <= 0 || isAirborne) return;

        float range = GetBodyRadius() + contactRangeTiles * tileSize;
        if (Vector2.Distance(rb.position, player.position) <= range)
            BossPlayerUtils.DamagePlayer(player.gameObject, contactDamage);
    }

    private void PushPlayerOut(Vector2 center)
    {
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        Vector2 playerPos = playerRb != null ? playerRb.position : (Vector2)player.position;

        Vector2 away = playerPos - center;
        if (away.sqrMagnitude < 0.0001f)
            away = Vector2.down;

        float minDistance = GetBodyRadius() + landingPushMargin;
        if (away.magnitude >= minDistance) return;

        Vector2 newPos = center + away.normalized * minDistance;
        player.position = newPos;

        if (playerRb != null)
        {
            playerRb.position = newPos;
            playerRb.linearVelocity = Vector2.zero;
        }
    }

    private float GetBodyRadius()
    {
        CircleCollider2D circle = bodyCollider as CircleCollider2D;
        if (circle != null)
            return circle.radius * Mathf.Abs(transform.lossyScale.x);

        return 0.5f;
    }

    // ───── Ataque 2: lengua ─────

    private IEnumerator TongueAttack()
    {
        for (int i = 0; i < tongueRepeats && !isDead; i++)
        {
            if (player == null) yield break;
            Face((Vector2)player.position - rb.position);

            AnimBool("MouthWiggle", true);
            yield return MouthTell(tongueTellDuration, jerky: false);
            AnimBool("MouthWiggle", false);
            if (isDead || player == null) yield break;

            AnimBool("TongueOut", true);
            yield return tongue.Lash(player);
            AnimBool("TongueOut", false);

            if (i < tongueRepeats - 1)
                yield return new WaitForSeconds(delayBetweenTongues);
        }
    }

    // ───── Ataque 3: moco ─────

    private IEnumerator SlimeAttack()
    {
        if (player == null) yield break;
        Face((Vector2)player.position - rb.position);

        AnimTrigger("SlimeTell");
        yield return MouthTell(slimeTellDuration, jerky: true);
        if (isDead || player == null) yield break;

        Vector3 target = player.position; // donde está parado EN ESTE MOMENTO
        float radius = slimeRadiusTiles * tileSize;
        Transform roomParent = FindRoomParent();

        AnimTrigger("Spit");
        SpawnSlimeWarning(target, radius, roomParent);

        Vector3 from = mouthPoint != null ? mouthPoint.position : transform.position;
        if (slimeGlobPrefab != null)
        {
            ToadSlimeGlob glob = Instantiate(slimeGlobPrefab, from, Quaternion.identity, roomParent);
            glob.Launch(from, target, slimeFlightTime, slimePuddlePrefab, radius, roomParent);
        }
        else
        {
            SlimePuddle.Spawn(slimePuddlePrefab, target, radius, roomParent);
        }

        // Retroceso
        float t = 0f;
        const float recoilTime = 0.18f;
        while (t < recoilTime && !isDead)
        {
            float k = 1f - t / recoilTime;
            SetVisualScale(new Vector3(1f - 0.2f * k, 1f + 0.25f * k, 1f));
            t += Time.deltaTime;
            yield return null;
        }
        ResetVisual();

        yield return new WaitForSeconds(afterSpitRecovery);
    }

    private void SpawnSlimeWarning(Vector3 position, float radius, Transform roomParent)
    {
        if (slimeWarningPrefab == null) return;

        GameObject warning = Instantiate(slimeWarningPrefab, position, Quaternion.identity);
        ScaleSpriteToDiameter(warning.transform, warning.GetComponentInChildren<SpriteRenderer>(), radius * 2f);
        if (roomParent != null) warning.transform.SetParent(roomParent, true);

        PoisonWarning warningScript = warning.GetComponent<PoisonWarning>();
        if (warningScript != null) warningScript.Play(slimeFlightTime);

        Destroy(warning, slimeFlightTime + 0.1f);
    }

    // ───── Tells / visual ─────

    // jerky = false: ondulación rara (lengua). jerky = true: sacudones bruscos (moco).
    private IEnumerator MouthTell(float duration, bool jerky)
    {
        float t = 0f, nextJerk = 0f, jerkRotation = 0f;
        Vector3 jerkScale = Vector3.one;

        while (t < duration && !isDead)
        {
            if (visual != null)
            {
                if (jerky)
                {
                    if (t >= nextJerk)
                    {
                        nextJerk = t + Random.Range(0.06f, 0.12f);
                        float s = Random.Range(-0.18f, 0.18f);
                        jerkScale = new Vector3(1f + s, 1f - s, 1f);
                        jerkRotation = Random.Range(-12f, 12f);
                    }
                    SetVisualScale(jerkScale);
                    visual.localRotation = baseVisualRot * Quaternion.Euler(0f, 0f, jerkRotation);
                }
                else
                {
                    float wobble = Mathf.Sin(t * 22f) * 0.08f;
                    float gulp = Mathf.Sin(t * 9f) * 0.06f;
                    SetVisualScale(new Vector3(1f + wobble, 1f - wobble + gulp, 1f));
                    visual.localRotation = baseVisualRot * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 13f) * 5f);
                }
            }
            t += Time.deltaTime;
            yield return null;
        }
        ResetVisual();
    }

    private void SetVisualScale(Vector3 multiplier)
    {
        if (visual != null) visual.localScale = Vector3.Scale(baseVisualScale, multiplier);
    }

    private void ResetVisual()
    {
        if (visual == null) return;
        visual.localPosition = baseVisualPos;
        visual.localScale = baseVisualScale;
        visual.localRotation = baseVisualRot;
    }

    private void Face(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) return;

        Vector2 facing = Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? new Vector2(Mathf.Sign(direction.x), 0f)
            : new Vector2(0f, Mathf.Sign(direction.y));

        AnimFloat("MoveX", facing.x);
        AnimFloat("MoveY", facing.y);

        if (flipSpriteHorizontally && visualRenderer != null && facing.x != 0f)
            visualRenderer.flipX = facing.x < 0f;
    }

    private static void ScaleSpriteToDiameter(Transform root, SpriteRenderer sr, float diameter)
    {
        if (root == null || sr == null || sr.sprite == null) return;
        float currentWidth = sr.sprite.bounds.size.x * Mathf.Abs(sr.transform.lossyScale.x);
        if (currentWidth <= 0.0001f) return;
        root.localScale *= diameter / currentWidth;
    }

    // ───── Animator (solo setea parámetros que existen) ─────

    private void CacheAnimatorParams()
    {
        animatorParams.Clear();
        if (animator == null || animator.runtimeAnimatorController == null) return;
        foreach (AnimatorControllerParameter p in animator.parameters) animatorParams.Add(p.name);
    }

    private void AnimTrigger(string n) { if (animator != null && animatorParams.Contains(n)) animator.SetTrigger(n); }
    private void AnimBool(string n, bool v) { if (animator != null && animatorParams.Contains(n)) animator.SetBool(n, v); }
    private void AnimFloat(string n, float v) { if (animator != null && animatorParams.Contains(n)) animator.SetFloat(n, v); }

    // ───── Muerte ─────

    private void StopMovement()
    {
        if (rb == null) return;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    private void HandleDeath()
    {
        StopBoss();
        if (tongue != null) tongue.ForceRelease(); // si muere arrastrando al jugador, le devuelve el control
        if (isAirborne) SetAirborne(false);

        ResetVisual();
        StopMovement();
        AnimBool("InAir", false);
        AnimBool("MouthWiggle", false);
        AnimBool("TongueOut", false);

        OpenRoomAfterDefeat();
    }

    private Transform FindRoomParent()
    {
        RoomInstance room = GetComponentInParent<RoomInstance>();
        return room != null ? room.transform : transform.parent;
    }
}
