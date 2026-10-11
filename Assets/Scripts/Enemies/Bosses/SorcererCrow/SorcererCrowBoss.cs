using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cuervo Hechicero: jefe final (nivel 3). Segun el GDD:
/// - Se queda quieto arriba de la room y NO recibe daño mientras esta ahi: solo se lo puede herir durante
///   sus ataques (segun el ataque).
/// - Dos estados: Normal y Poseido. Al vencerlo en estado Normal "parece" terminar la pelea y se revela
///   Poseido, con los mismos ataques pero mas fuertes. Al vencer al Poseido aparece la puerta final.
/// Ataques:
///   1) Orbes espectrales GRANDES: 5 orbes (0.7 s entre cada una). Solo la ULTIMA se puede desviar: hay que apretar el
///      ATAQUE PESADO (E) en el momento en que la orbe choca con el player (ver CrowOrb). Si vuelve y le pega, el cuervo
///      recibe daño y queda aturdido 2.5 s (ahi se lo golpea).
///   2) Portal y brazo: se escapa por un portal, abre un portal en el piso donde esta el player (aviso con
///      temblor) y saca un brazo. Hay 0.7 s para golpear el brazo antes de que se retraiga. NUNCA hay dos portales
///      abiertos a la vez (el de escape se cierra antes de abrir el del brazo, y ese se cierra antes del de regreso);
///      al terminar vuelve a la arena por un portal.
///   3) Chillido: desaparece y suelta cuervitos desde los costados que vuelan por el eje X, con 0.6 s entre cada uno.
///      Los negros no se pueden golpear; los blancos (versiones chicas del jefe) si, solo pueden aparecer 3 veces por
///      ataque y al recibir el golpe desaparecen y dañan al cuervo.
/// Estado Poseido: 8 orbes al doble de velocidad (0.5 s entre cada una), 5 portales/brazos (0.7 s entre cada uno,
/// todos quedan hasta que aparece el ultimo) y muchos mas cuervitos, varios a la vez, que ahora tambien vuelan por
/// el eje Y y salen cada 0.5 s.
/// </summary>
public class SorcererCrowBoss : BossBase
{
    private enum CrowAttack { SpectralOrbs, PortalArms, RavenSwarm }

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private DamageFlash damageFlash;
    [SerializeField] private Transform visual;
    [SerializeField] private SpriteRenderer visualRenderer;

    [Header("Vida por fase")]
    [SerializeField] private float phaseOneHealth = 100f;
    [SerializeField] private float phaseTwoHealth = 120f;

    [Header("Ataque 1 - Orbes espectrales")]
    [SerializeField] private int orbCount = 5;
    [Tooltip("GDD: 0.7 s entre cada orbe.")]
    [SerializeField] private float orbDelay = 0.7f;
    [SerializeField] private float orbSpeed = 5.5f;
    [Tooltip("Las orbes son GRANDES (GDD).")]
    [SerializeField] private float orbScale = 1.5f;
    [SerializeField] private int orbDamage = 1;
    [SerializeField] private float orbTellTime = 0.6f;
    [SerializeField] private float reflectedOrbSpeed = 13f;
    [Tooltip("Daño que recibe el cuervo cuando le devuelven la orbe.")]
    [SerializeField] private float deflectDamage = 30f;
    [Tooltip("GDD: el cuervo queda aturdido 2.5 s cuando le devuelven la orbe.")]
    [SerializeField] private float stunDuration = 2.5f;

    [Header("Ataque 2 - Portal y brazo")]
    [SerializeField] private float armTellTime = 0.9f;
    [Tooltip("GDD: el player tiene 0.7 s para golpear el brazo antes de que se retraiga.")]
    [SerializeField] private float armWindow = 0.7f;
    [Tooltip("GDD (estado poseido): 0.7 s entre la aparicion de cada portal/brazo.")]
    [SerializeField] private float armSpawnDelay = 0.7f;
    [SerializeField] private int armContactDamage = 2;
    [SerializeField] private float armShake = 0.07f;
    [Tooltip("Pausa entre que se cierra un portal y se abre el siguiente (nunca hay dos portales abiertos a la vez en estado Normal).")]
    [SerializeField] private float portalGap = 0.3f;

    [Header("Ataque 3 - Chillido y cuervitos")]
    [Tooltip("Cuanto dura la lluvia de cuervitos (estado Normal).")]
    [SerializeField] private float ravenDuration = 7.2f;
    [Tooltip("GDD: 0.6 s de delay entre cuervitos.")]
    [SerializeField] private float ravenInterval = 0.6f;
    [SerializeField] private float ravenSpeed = 6.5f;
    [SerializeField] private int ravenDamage = 1;
    [SerializeField] private float ravenTellTime = 0.45f;
    [Tooltip("GDD: los cuervitos blancos solo pueden aparecer 3 veces por ataque.")]
    [SerializeField] private int maxWhiteRavensPerAttack = 3;

    [Header("Estado Poseido (versiones mas fuertes)")]
    [Tooltip("GDD: 8 proyectiles...")]
    [SerializeField] private int possessedOrbCount = 8;
    [Tooltip("...con 0.5 s de delay entre ellos...")]
    [SerializeField] private float possessedOrbDelay = 0.5f;
    [Tooltip("...al doble de velocidad.")]
    [SerializeField] private float possessedOrbSpeedMultiplier = 2f;
    [Tooltip("GDD: 5 portales/brazos en total.")]
    [SerializeField] private int possessedArmCount = 5;
    [SerializeField] private float possessedRavenDuration = 7.2f;
    [Tooltip("GDD: el delay de 0.6 s baja a 0.5 s.")]
    [SerializeField] private float possessedRavenInterval = 0.5f;
    [Tooltip("GDD: aparecen muchos a la vez: cada tanda suelta entre Min y Max cuervitos.")]
    [SerializeField] private int possessedRavenBurstMin = 2;
    [SerializeField] private int possessedRavenBurstMax = 3;
    [Tooltip("GDD: en estado poseido los cuervitos tambien vuelan por el eje Y. Probabilidad de que un cuervito (que no sea el primero de la tanda) sea vertical.")]
    [Range(0f, 1f)]
    [SerializeField] private float possessedVerticalChance = 0.5f;
    [SerializeField] private float possessedRavenSpeedMultiplier = 1.25f;
    [SerializeField] private Color possessedTint = new Color(1f, 0.82f, 1f, 1f);

    [Header("Arena (relativa al centro de la room)")]
    [SerializeField] private float arenaHalfWidth = 8.2f;
    [SerializeField] private float arenaMinY = -3.8f;
    [SerializeField] private float arenaMaxY = 3.3f;

    private Transform player;
    private Vector2 homePosition;
    private Vector3 baseVisualPos = Vector3.zero;
    private Vector3 baseVisualScale = Vector3.one;

    private bool possessed;
    private bool phaseTransitionPending;
    private bool stunned;
    private bool onArena = true;
    private int pendingRavenLaunches;
    private bool hasLastAttack;
    private CrowAttack lastAttack;
    private Coroutine stunRoutine;

    private readonly List<GameObject> hazards = new List<GameObject>();

    private bool ShouldAbort => isDead || phaseTransitionPending;

    /// <summary>Punto al que apuntan las orbes devueltas.</summary>
    public Vector2 HitPoint => bodyCollider != null ? (Vector2)bodyCollider.bounds.center : (Vector2)transform.position;

    /// <summary>true si un circulo (centro, radio) toca el cuerpo del cuervo. Lo usa la orbe devuelta para saber si le pego.</summary>
    public bool OverlapsBody(Vector2 center, float radius)
    {
        if (bodyCollider != null && bodyCollider.enabled)
        {
            Vector2 closest = bodyCollider.ClosestPoint(center);
            return (closest - center).sqrMagnitude <= radius * radius;
        }

        // Sin collider activo (no deberia pasar mientras tira orbes): se usa una zona fija alrededor del cuervo.
        float reach = radius + 0.8f;
        return (HitPoint - center).sqrMagnitude <= reach * reach;
    }

    /// <summary>true si el punto esta fuera de la arena (con un margen). Las orbes que se escapan se descartan.</summary>
    public bool IsOutsideArena(Vector2 point, float margin)
    {
        Vector2 center = RoomCenter();
        return Mathf.Abs(point.x - center.x) > arenaHalfWidth + margin
            || point.y > center.y + arenaMaxY + margin + 2f
            || point.y < center.y + arenaMinY - margin;
    }

    // ================================================================= ciclo de vida

    protected override void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
        if (enemyHealth == null) enemyHealth = GetComponent<EnemyHealth>();
        if (damageFlash == null) damageFlash = GetComponent<DamageFlash>();
        if (visual == null) visual = transform.Find("Visual");
        if (visualRenderer == null && visual != null) visualRenderer = visual.GetComponent<SpriteRenderer>();

        // Arte: si el prefab no trae sprite se usa el placeholder generado por codigo (ver CrowArt).
        if (visualRenderer != null && visualRenderer.sprite == null)
            visualRenderer.sprite = CrowArt.Body(false);

        if (visual != null)
        {
            baseVisualPos = visual.localPosition;
            baseVisualScale = visual.localScale;
        }

        homePosition = transform.position;
        FindPlayer();

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        if (enemyHealth != null)
        {
            enemyHealth.Refill(phaseOneHealth);
            enemyHealth.SetDamageable(false); // solo se lo hiere durante los ataques
            enemyHealth.OnDeath += HandleDeath;
            enemyHealth.DeathInterceptor = InterceptDeath;
        }

        base.Awake();
    }

    private void OnDestroy()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnDeath -= HandleDeath;
            enemyHealth.DeathInterceptor = null;
        }
    }

    private void Update()
    {
        // Flotacion suave en reposo (no durante el cambio de fase, que anima la posicion por su cuenta).
        if (visual != null && onArena && !isDead && !phaseTransitionPending)
            visual.localPosition = new Vector3(baseVisualPos.x, baseVisualPos.y + Mathf.Sin(Time.time * 2.2f) * 0.06f, baseVisualPos.z);
    }

    private void FindPlayer()
    {
        if (player != null) return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
    }

    // ================================================================= rutina principal

    protected override IEnumerator BossRoutine()
    {
        FindPlayer();
        yield return new WaitForSeconds(1f);

        while (!isDead)
        {
            if (phaseTransitionPending)
            {
                yield return PossessionTransition();
                continue;
            }

            // Mientras no ataca (pausa) el cuervo no recibe daño.
            yield return PauseBetweenAttacks();
            if (isDead) yield break;
            if (phaseTransitionPending) continue;

            CrowAttack attack = ChooseNextAttack();
            yield return RunAttack(attack);

            // Si quedo aturdido, el player tiene su ventana: no arranca otro ataque hasta que termine.
            while (stunned && !ShouldAbort)
                yield return null;
        }
    }

    private CrowAttack ChooseNextAttack()
    {
        CrowAttack pick;
        int guard = 0;

        do
        {
            pick = (CrowAttack)Random.Range(0, 3);
            guard++;
        }
        while (hasLastAttack && pick == lastAttack && guard < 12);

        lastAttack = pick;
        hasLastAttack = true;
        return pick;
    }

    private IEnumerator RunAttack(CrowAttack attack)
    {
        FindPlayer();
        if (player == null)
            yield break;

        switch (attack)
        {
            case CrowAttack.SpectralOrbs:
                yield return OrbAttack();
                break;

            case CrowAttack.PortalArms:
                yield return ArmAttack();
                break;

            default:
                yield return RavenAttack();
                break;
        }

        ResetVisual();
    }

    // ================================================================= ataque 1: orbes

    private IEnumerator OrbAttack()
    {
        int count = possessed ? possessedOrbCount : orbCount;
        float delay = possessed ? possessedOrbDelay : orbDelay;
        float speed = possessed ? orbSpeed * possessedOrbSpeedMultiplier : orbSpeed;

        // Abre el pico (aviso)
        float t = 0f;
        while (t < orbTellTime && !ShouldAbort)
        {
            float k = t / orbTellTime;
            SetVisualScale(new Vector3(1f + 0.16f * k, 1f - 0.08f * k, 1f));
            t += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            if (ShouldAbort) yield break;

            bool lastOne = i == count - 1;
            FireOrb(lastOne, speed);

            if (!lastOne)
                yield return Wait(delay);
        }

        ResetVisual();

        // Espera a que la ultima orbe se resuelva (desviada, impacto o se pierde).
        float safety = Time.time + 7f;
        while (!ShouldAbort && AnyHazard<CrowOrb>() && Time.time < safety)
            yield return null;
    }

    private void FireOrb(bool deflectable, float speed)
    {
        if (player == null) return;

        Vector2 mouth = (Vector2)transform.position + Vector2.down * 0.2f;
        Vector2 direction = (Vector2)player.position - mouth;
        if (direction.sqrMagnitude < 0.01f)
            direction = Vector2.down;

        CrowOrb orb = CrowOrb.Spawn(this, mouth, direction, speed, orbDamage, deflectable,
            reflectedOrbSpeed, orbScale, RoomTransform());
        Track(orb.gameObject);

        CameraShake.Play(0.08f, 0.05f);
    }

    /// <summary>La orbe desviable fue devuelta con el ataque pesado.</summary>
    public void OnOrbDeflected()
    {
        CameraShake.Play(0.15f, 0.1f);
    }

    /// <summary>La orbe devuelta llego al cuervo: recibe daño y queda aturdido.</summary>
    public void OnOrbReflectedHit()
    {
        if (ShouldAbort) return;

        ApplyBossDamage(deflectDamage);
        CameraShake.Play(0.3f, 0.2f);

        if (ShouldAbort) return; // el golpe puede haber disparado el cambio de fase

        if (stunRoutine != null)
            StopCoroutine(stunRoutine);
        stunRoutine = StartCoroutine(StunRoutine(stunDuration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        stunned = true;
        RefreshDamageable();

        if (damageFlash != null)
            damageFlash.SetPersistentTint(new Color(0.65f, 0.8f, 1f, 1f));

        float end = Time.time + duration;
        while (Time.time < end && !ShouldAbort)
        {
            if (visual != null)
                visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 18f) * 6f);
            yield return null;
        }

        stunned = false;
        stunRoutine = null;

        if (visual != null)
            visual.localRotation = Quaternion.identity;

        // Si el golpe disparo el cambio de fase, la transicion se encarga del color.
        if (damageFlash != null && !phaseTransitionPending)
            damageFlash.SetPersistentTint(possessed ? possessedTint : Color.white);

        RefreshDamageable();
    }

    // ================================================================= ataque 2: portal y brazo

    private IEnumerator ArmAttack()
    {
        int armCount = possessed ? possessedArmCount : 1;

        // 1) El cuervo se hunde en un portal y espera a que ese portal se CIERRE del todo...
        yield return EscapeThroughPortal();
        if (ShouldAbort) yield break;

        // 2) ...recien despues se abre el portal del brazo (GDD: nunca hay varios portales abiertos a la vez;
        //    el estado Poseido es la excepcion: ahi abre 5 portales/brazos y quedan todos en la arena).
        yield return Wait(portalGap);
        if (ShouldAbort) yield break;

        List<CrowArm> arms = new List<CrowArm>();
        List<Vector2> armPositions = new List<Vector2>();

        for (int i = 0; i < armCount; i++)
        {
            if (ShouldAbort) yield break;

            Vector2 target = PickArmPosition(armPositions);
            armPositions.Add(target);

            // Temblor leve: avisa que va a salir un brazo.
            CameraShake.Play(armTellTime, armShake);

            CrowArm arm = CrowArm.Spawn(this, target, RoomTransform(), armTellTime, armContactDamage);
            Track(arm.gameObject);
            arms.Add(arm);

            if (i < armCount - 1)
                yield return Wait(armSpawnDelay);
        }

        // Todos los brazos quedan hasta que aparece el ultimo; despues hay una ventana para golpearlos.
        float retractTime = Time.time + armTellTime + armWindow;
        for (int i = 0; i < arms.Count; i++)
        {
            if (arms[i] != null)
                arms[i].RetractAt(retractTime);
        }

        float safety = Time.time + armTellTime + armWindow + 3f;
        while (!ShouldAbort && AnyArmAlive(arms) && Time.time < safety)
            yield return null;

        if (ShouldAbort) yield break;

        yield return ReturnThroughPortal();
    }

    private const float ArmSpacing = 1.6f;

    /// <summary>
    /// Donde sale el brazo: justo donde esta el jugador AHORA (GDD). En estado Poseido, si ya hay un brazo en ese lugar (el
    /// jugador se quedo quieto) el nuevo portal se abre al lado, para que siempre sean 5 portales distintos.
    /// </summary>
    private Vector2 PickArmPosition(List<Vector2> existing)
    {
        Vector2 baseTarget = ClampToArena((Vector2)player.position + Vector2.down * 0.35f);
        if (IsFarFromAll(existing, baseTarget))
            return baseTarget;

        for (int attempt = 0; attempt < 10; attempt++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(ArmSpacing, ArmSpacing * 1.9f);
            Vector2 candidate = ClampToArena(baseTarget + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance);
            if (IsFarFromAll(existing, candidate))
                return candidate;
        }

        return baseTarget;
    }

    private static bool IsFarFromAll(List<Vector2> points, Vector2 candidate)
    {
        for (int i = 0; i < points.Count; i++)
        {
            if (Vector2.Distance(points[i], candidate) < ArmSpacing)
                return false;
        }

        return true;
    }

    private static bool AnyArmAlive(List<CrowArm> arms)
    {
        for (int i = 0; i < arms.Count; i++)
        {
            if (arms[i] != null && !arms[i].IsFinished)
                return true;
        }

        return false;
    }

    /// <summary>Un brazo recibio un golpe del player.</summary>
    public void OnArmStruck(float damage)
    {
        if (ShouldAbort) return;

        ApplyBossDamage(damage);
        CameraShake.Play(0.15f, 0.1f);
    }

    private IEnumerator EscapeThroughPortal()
    {
        // Abre (0.35 s), se mantiene hasta que el cuervo termina de hundirse (~0.75 s) y se cierra (0.3 s).
        Vector2 portalPosition = (Vector2)transform.position + Vector2.down * 1.25f;
        GameObject portal = CrowFx.Portal(portalPosition, RoomTransform(), 0.35f, 0.5f, 0.3f, 2.8f);
        yield return Wait(0.3f);

        // El cuervo se hunde en el portal.
        float t = 0f;
        const float sink = 0.45f;
        while (t < sink && !ShouldAbort)
        {
            float k = t / sink;
            SetVisualScale(new Vector3(1f - 0.6f * k, 1f - k, 1f));
            t += Time.deltaTime;
            yield return null;
        }

        if (ShouldAbort) yield break;

        SetOnArena(false);

        // El portal de escape tiene que estar CERRADO antes de que se abra cualquier otro.
        yield return WaitUntilGone(portal);
    }

    private IEnumerator ReturnThroughPortal()
    {
        // Los brazos (y sus portales) ya terminaron: recien ahora se abre el portal de regreso.
        yield return Wait(portalGap);
        if (ShouldAbort) yield break;

        Vector2 portalPosition = (Vector2)transform.position + Vector2.down * 1.25f;
        GameObject portal = CrowFx.Portal(portalPosition, RoomTransform(), 0.35f, 0.5f, 0.3f, 2.8f);
        yield return Wait(0.35f);
        if (ShouldAbort) yield break;

        SetVisualScale(new Vector3(0.4f, 0f, 1f));
        SetOnArena(true);

        float t = 0f;
        const float rise = 0.45f;
        while (t < rise && !ShouldAbort)
        {
            float k = t / rise;
            SetVisualScale(new Vector3(0.4f + 0.6f * k, k, 1f));
            t += Time.deltaTime;
            yield return null;
        }

        ResetVisual();

        // El portal de regreso se cierra por completo antes de seguir con la rutina.
        yield return WaitUntilGone(portal);
    }

    /// <summary>Espera a que un efecto (portal) termine de cerrarse y se destruya.</summary>
    private IEnumerator WaitUntilGone(GameObject effect)
    {
        float safety = Time.time + 3f;
        while (effect != null && !ShouldAbort && Time.time < safety)
            yield return null;
    }

    // ================================================================= ataque 3: chillido y cuervitos

    private IEnumerator RavenAttack()
    {
        float duration = possessed ? possessedRavenDuration : ravenDuration;
        float interval = possessed ? possessedRavenInterval : ravenInterval;
        float speed = possessed ? ravenSpeed * possessedRavenSpeedMultiplier : ravenSpeed;

        // Chillido: se agranda, tiembla y desaparece.
        float t = 0f;
        const float screech = 0.8f;
        while (t < screech && !ShouldAbort)
        {
            float k = t / screech;
            SetVisualScale(new Vector3(1f + 0.25f * k, 1f + 0.25f * k, 1f));
            if (visual != null)
                visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 40f) * 4f);
            if (((int)(t * 10f)) % 3 == 0)
                CameraShake.Play(0.1f, 0.06f);
            t += Time.deltaTime;
            yield return null;
        }

        if (visual != null)
            visual.localRotation = Quaternion.identity;
        if (ShouldAbort) yield break;

        SetOnArena(false);
        ResetVisual();

        // Enjambre: una tanda cada 'interval' segundos (GDD: 0.6 s en estado Normal, 0.5 s en Poseido).
        // Los cuervitos BLANCOS solo pueden aparecer maximo 3 veces por ataque: se reparten al azar a lo largo del ataque.
        int waves = Mathf.Max(1, Mathf.RoundToInt(duration / interval));
        HashSet<int> whiteWaves = PickWhiteRavenWaves(waves, maxWhiteRavensPerAttack);

        for (int wave = 0; wave < waves && !ShouldAbort; wave++)
        {
            // Normal: de a uno. Poseido: "muchos a la vez".
            int burst = possessed ? Random.Range(possessedRavenBurstMin, possessedRavenBurstMax + 1) : 1;
            burst = Mathf.Max(1, burst);
            int whiteIndex = whiteWaves.Contains(wave) ? Random.Range(0, burst) : -1;

            List<float> usedRows = new List<float>();
            List<float> usedColumns = new List<float>();

            for (int i = 0; i < burst; i++)
            {
                // Normal: solo vuelan por el eje X. Poseido: tambien por el eje Y (el primero de la tanda siempre es
                // horizontal para que siempre haya un carril a la altura del player).
                bool vertical = possessed && i > 0 && Random.value < possessedVerticalChance;
                bool white = i == whiteIndex;
                float side = Random.value < 0.5f ? -1f : 1f;
                float lane = vertical ? PickColumn(usedColumns) : PickRow(usedRows);

                pendingRavenLaunches++;
                StartCoroutine(LaunchRaven(white, vertical, side, lane, speed));
            }

            yield return Wait(interval);
        }

        // Espera a que salgan los ultimos y que crucen la sala.
        float safety = Time.time + 6f;
        while (!ShouldAbort && (pendingRavenLaunches > 0 || AnyHazard<CrowRaven>()) && Time.time < safety)
            yield return null;

        if (ShouldAbort) yield break;

        yield return ReturnFromScreech();
    }

    /// <summary>Elige en que tandas sale un cuervito blanco: 'count' (max 3), repartidas en tercios y nunca en la primera tanda.</summary>
    private static HashSet<int> PickWhiteRavenWaves(int waves, int count)
    {
        HashSet<int> result = new HashSet<int>();
        count = Mathf.Clamp(count, 0, waves);
        if (count == 0)
            return result;

        float segment = waves / (float)count;
        for (int i = 0; i < count; i++)
        {
            int from = Mathf.Max(i == 0 ? 1 : 0, Mathf.CeilToInt(segment * i));
            int to = Mathf.Min(waves - 1, Mathf.CeilToInt(segment * (i + 1)) - 1);
            if (to < from)
                to = from;

            int pick = Mathf.Min(Random.Range(from, to + 1), waves - 1);
            int guard = 0;
            while (result.Contains(pick) && guard++ < waves)
                pick = (pick + 1) % waves;

            result.Add(pick);
        }

        return result;
    }

    private const float RavenLaneSpacing = 1.5f;

    /// <summary>Altura (Y) del carril de un cuervito horizontal. Muchos van hacia donde esta el player para obligarlo a moverse.</summary>
    private float PickRow(List<float> used)
    {
        Vector2 center = RoomCenter();
        float min = center.y + arenaMinY;
        float max = center.y + arenaMaxY;

        float pick = Mathf.Clamp(center.y, min, max);
        for (int attempt = 0; attempt < 8; attempt++)
        {
            float y = player != null && Random.value < 0.6f
                ? player.position.y + Random.Range(-0.7f, 0.7f)
                : Random.Range(min, max);

            pick = Mathf.Clamp(y, min, max);
            if (LaneIsFree(used, pick))
                break;
        }

        used.Add(pick);
        return pick;
    }

    /// <summary>Posicion (X) del carril de un cuervito vertical (solo estado Poseido).</summary>
    private float PickColumn(List<float> used)
    {
        Vector2 center = RoomCenter();
        float min = center.x - arenaHalfWidth + 0.8f;
        float max = center.x + arenaHalfWidth - 0.8f;

        float pick = Mathf.Clamp(center.x, min, max);
        for (int attempt = 0; attempt < 8; attempt++)
        {
            float x = player != null && Random.value < 0.7f
                ? player.position.x + Random.Range(-0.7f, 0.7f)
                : Random.Range(min, max);

            pick = Mathf.Clamp(x, min, max);
            if (LaneIsFree(used, pick))
                break;
        }

        used.Add(pick);
        return pick;
    }

    private static bool LaneIsFree(List<float> used, float value)
    {
        for (int i = 0; i < used.Count; i++)
        {
            if (Mathf.Abs(used[i] - value) < RavenLaneSpacing)
                return false;
        }

        return true;
    }

    private IEnumerator LaunchRaven(bool white, bool vertical, float side, float lane, float speed)
    {
        Vector2 center = RoomCenter();

        Vector2 spawn;
        Vector2 direction;
        float travel;
        Vector2 barCenter;
        float barWidth;
        float barHeight;

        if (!vertical)
        {
            // Entra por un costado y cruza toda la sala por el eje X.
            spawn = new Vector2(center.x + side * (arenaHalfWidth + 0.4f), lane);
            direction = new Vector2(-side, 0f);
            travel = arenaHalfWidth * 2f + 2.2f;
            barCenter = new Vector2(center.x, lane);
            barWidth = arenaHalfWidth * 2f + 1f;
            barHeight = 0.45f;
        }
        else
        {
            // Estado Poseido: entra por arriba o por abajo y cruza toda la sala por el eje Y.
            float top = center.y + arenaMaxY + 1f;
            float bottom = center.y + arenaMinY - 0.6f;
            spawn = new Vector2(lane, side > 0f ? top : bottom);
            direction = new Vector2(0f, -side);
            travel = (top - bottom) + 2.2f;
            barCenter = new Vector2(lane, (top + bottom) * 0.5f);
            barWidth = 0.45f;
            barHeight = (top - bottom) + 0.6f;
        }

        // Aviso: barra translucida por donde va a pasar.
        float tell = ravenTellTime;
        Color warn = white ? new Color(1f, 1f, 1f, 0.28f) : new Color(1f, 0.15f, 0.15f, 0.3f);
        GameObject bar = CrowFx.Bar(barCenter, barWidth, barHeight, warn, tell * 0.4f, tell * 0.6f, 0.15f, RoomTransform());
        Track(bar);

        yield return new WaitForSeconds(tell);

        if (ShouldAbort)
        {
            pendingRavenLaunches = Mathf.Max(0, pendingRavenLaunches - 1);
            yield break;
        }

        CrowRaven raven = CrowRaven.Spawn(this, white, spawn, direction, speed, ravenDamage, travel, RoomTransform());
        Track(raven.gameObject);

        pendingRavenLaunches = Mathf.Max(0, pendingRavenLaunches - 1);
    }

    /// <summary>Un cuervito blanco recibio un golpe: el cuervo recibe daño.</summary>
    public void OnWhiteRavenStruck(float damage)
    {
        if (ShouldAbort) return;

        ApplyBossDamage(damage);
        CameraShake.Play(0.12f, 0.08f);
    }

    private IEnumerator ReturnFromScreech()
    {
        SetVisualScale(new Vector3(0.4f, 0.4f, 1f));
        SetOnArena(true);

        float t = 0f;
        const float appear = 0.5f;
        while (t < appear && !ShouldAbort)
        {
            float k = t / appear;
            SetVisualScale(new Vector3(0.4f + 0.6f * k, 0.4f + 0.6f * k, 1f));
            t += Time.deltaTime;
            yield return null;
        }

        ResetVisual();
    }

    // ================================================================= daño y fases

    /// <summary>Daño "scripteado" al cuervo (orbe devuelta, brazo, cuervito blanco).</summary>
    private void ApplyBossDamage(float amount)
    {
        if (enemyHealth == null || isDead) return;

        enemyHealth.SetDamageable(true);
        enemyHealth.TakeDamage(amount);
        RefreshDamageable();
    }

    private void RefreshDamageable()
    {
        if (enemyHealth == null) return;

        // Solo se lo puede golpear de forma directa mientras esta aturdido y a la vista.
        enemyHealth.SetDamageable(stunned && onArena && !phaseTransitionPending && !isDead);
        enemyHealth.SuppressHitFeedback = !onArena;
    }

    /// <summary>Evita la muerte real al vencerlo en estado Normal: dispara el cambio a Poseido.</summary>
    private bool InterceptDeath()
    {
        if (isDead || possessed || phaseTransitionPending)
            return phaseTransitionPending;

        phaseTransitionPending = true;
        RefreshDamageable();
        return true;
    }

    private IEnumerator PossessionTransition()
    {
        ClearHazards();
        stunned = false;
        pendingRavenLaunches = 0;

        if (!onArena)
        {
            ResetVisual();
            SetOnArena(true);
        }

        RefreshDamageable();

        // 1) "Parece" que el cuervo cayo: la pelea parece terminada.
        CameraShake.Play(0.45f, 0.22f);
        if (damageFlash != null)
            damageFlash.SetPersistentTint(new Color(0.55f, 0.55f, 0.6f, 1f));

        float t = 0f;
        const float collapse = 0.9f;
        while (t < collapse)
        {
            float k = t / collapse;
            SetVisualScale(new Vector3(1f + 0.1f * k, 1f - 0.45f * k, 1f));
            if (visual != null)
                visual.localPosition = baseVisualPos + Vector3.down * (0.5f * k);
            t += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(1.6f);

        // 2) Se revela poseido.
        possessed = true;
        if (visualRenderer != null)
            visualRenderer.sprite = CrowArt.Body(true);
        if (damageFlash != null)
            damageFlash.SetPersistentTint(possessedTint);

        if (enemyHealth != null)
            enemyHealth.Refill(phaseTwoHealth);

        CameraShake.Play(0.7f, 0.32f);

        t = 0f;
        const float reveal = 0.5f;
        while (t < reveal)
        {
            float k = t / reveal;
            SetVisualScale(new Vector3(Mathf.Lerp(1.1f, 1.15f, k), Mathf.Lerp(0.55f, 1.15f, k), 1f));
            if (visual != null)
                visual.localPosition = Vector3.Lerp(baseVisualPos + Vector3.down * 0.5f, baseVisualPos, k);
            t += Time.deltaTime;
            yield return null;
        }

        ResetVisual();
        phaseTransitionPending = false;
        RefreshDamageable();

        yield return new WaitForSeconds(0.8f);
    }

    private void HandleDeath()
    {
        StopBoss();
        ClearHazards();
        stunned = false;

        CameraShake.Play(0.6f, 0.3f);

        OpenRoomAfterDefeat();
    }

    // ================================================================= utilidades

    private IEnumerator Wait(float seconds)
    {
        float end = Time.time + seconds;
        while (Time.time < end && !ShouldAbort)
            yield return null;
    }

    private void SetOnArena(bool value)
    {
        onArena = value;

        if (visualRenderer != null)
            visualRenderer.enabled = value;

        if (bodyCollider != null)
            bodyCollider.enabled = value;

        RefreshDamageable();
    }

    private void SetVisualScale(Vector3 multiplier)
    {
        if (visual != null)
            visual.localScale = Vector3.Scale(baseVisualScale, multiplier);
    }

    private void ResetVisual()
    {
        if (visual == null) return;

        visual.localScale = baseVisualScale;
        visual.localRotation = Quaternion.identity;
        visual.localPosition = baseVisualPos;
    }

    private Transform RoomTransform()
    {
        RoomInstance room = GetComponentInParent<RoomInstance>();
        return room != null ? room.transform : transform.parent;
    }

    private Vector2 RoomCenter()
    {
        Transform room = RoomTransform();
        return room != null ? (Vector2)room.position : homePosition;
    }

    private Vector2 ClampToArena(Vector2 point)
    {
        Vector2 center = RoomCenter();
        float margin = 0.6f;
        point.x = Mathf.Clamp(point.x, center.x - arenaHalfWidth + margin, center.x + arenaHalfWidth - margin);
        point.y = Mathf.Clamp(point.y, center.y + arenaMinY, center.y + arenaMaxY);
        return point;
    }

    private void Track(GameObject go)
    {
        if (go != null)
            hazards.Add(go);
    }

    private bool AnyHazard<T>() where T : Component
    {
        hazards.RemoveAll(h => h == null);

        for (int i = 0; i < hazards.Count; i++)
        {
            if (hazards[i].GetComponent<T>() != null)
                return true;
        }

        return false;
    }

    private void ClearHazards()
    {
        for (int i = hazards.Count - 1; i >= 0; i--)
        {
            if (hazards[i] != null)
                Destroy(hazards[i]);
        }

        hazards.Clear();
    }
}
