using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cuervo Hechicero: jefe final (nivel 3). Segun el GDD:
/// - Se queda quieto arriba de la room y NO recibe daño mientras esta ahi: solo se lo puede herir durante
///   sus ataques (segun el ataque).
/// - Dos estados: Normal ("The Sorcerer Crow", barra roja, mas vida) y Poseido ("The Possessed Crow", barra violeta,
///   un poco menos de vida). Al vencerlo en estado Normal muere (se sacude, grita y cae mientras su barra roja se apaga
///   con un fade), queda tirado unos segundos y RESURGE renovado en su segunda forma con una barra nueva violeta,
///   con los mismos ataques pero mas fuertes. Al vencer al Poseido aparece la puerta final.
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
    [Tooltip("Vida de la PRIMERA fase (The Sorcerer Crow). Tiene que ser mayor que la de la segunda.")]
    [SerializeField] private float phaseOneHealth = 300f;
    [Tooltip("Vida de la SEGUNDA fase (The Possessed Crow): un poco menos que la primera.")]
    [SerializeField] private float phaseTwoHealth = 240f;

    [Header("Muerte de la 1ra fase y resurreccion (2da fase)")]
    [Tooltip("El cuervo se sacude y grita antes de caer.")]
    [SerializeField] private float deathConvulseTime = 0.45f;
    [SerializeField] private float deathCollapseTime = 1.2f;
    [Tooltip("Tiempo que queda tirado antes de empezar a resurgir.")]
    [SerializeField] private float deathLieTime = 1.5f;
    [Tooltip("La energia violeta se junta sobre el cuerpo justo antes de resurgir.")]
    [SerializeField] private float rebirthChargeTime = 1.2f;
    [SerializeField] private float rebirthRiseTime = 0.9f;
    [Tooltip("La barra de vida ROJA se apaga con un fade apenas muere la 1ra fase.")]
    [SerializeField] private float barFadeOutTime = 1.0f;
    [Tooltip("La barra de vida VIOLETA aparece con un fade cuando resurge...")]
    [SerializeField] private float barFadeInTime = 0.7f;
    [Tooltip("...y se va llenando hasta el maximo en este tiempo.")]
    [SerializeField] private float barFillTime = 1.6f;

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

    // Barra de vida del jefe (BossHealthBarUI lee estos valores): nombre y color segun la fase + fades entre fases.
    private static readonly Color PossessedBarColor = new Color(0.62f, 0.24f, 0.95f, 1f);
    private float barOpacity = 1f;
    private float barFill = -1f;

    /// <summary>Primera fase: "The Sorcerer Crow". Segunda fase: "The Possessed Crow".</summary>
    public override string BarTitle => possessed ? "The Possessed Crow" : "The Sorcerer Crow";

    /// <summary>Primera fase: barra roja (la del prefab). Segunda fase: barra violeta.</summary>
    public override Color? BarFillColor => possessed ? PossessedBarColor : (Color?)null;

    public override float BarOpacity => barOpacity;

    public override float BarFillOverride => barFill;

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

    // Colores de los efectos de la transicion.
    private static readonly Color FeatherDark = new Color(0.08f, 0.07f, 0.14f, 1f);
    private static readonly Color FeatherViolet = new Color(0.45f, 0.2f, 0.72f, 1f);
    private static readonly Color EnergyViolet = new Color(0.72f, 0.38f, 1f, 1f);
    private static readonly Color EnergyPink = new Color(1f, 0.55f, 0.95f, 1f);

    private Coroutine barRoutine;

    /// <summary>
    /// Muerte de la primera fase + resurreccion en la segunda:
    ///  1) El cuervo se sacude, grita y cae (la animacion de muerte de siempre, con mas efectos) mientras la barra de vida
    ///     ROJA desaparece con un fade.
    ///  2) Queda tirado unos segundos.
    ///  3) Una energia violeta se junta sobre el cuerpo...
    ///  4) ...y resurge renovado en su segunda forma: barra de vida NUEVA violeta (con el otro nombre) que aparece con un fade
    ///     y se llena. Despues sigue la segunda fase con normalidad.
    /// </summary>
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

        // Centro del cuerpo para los efectos (no depende de que el collider recien reactivado ya tenga bounds validos).
        Transform room = RoomTransform();
        Vector2 body = (Vector2)transform.position + Vector2.up * 0.1f;
        Color grayTint = new Color(0.55f, 0.55f, 0.6f, 1f);
        Vector3 fallenOffset = Vector3.down * 0.5f;
        float t;

        // ---------------------------------------------------------- 1) MUERTE de la primera fase
        RunBarRoutine(FadeBarOut(0.25f, barFadeOutTime));

        CameraShake.Play(0.8f, 0.34f);
        AudioManager.PlayBossHurt();
        CrowDeathFx.ScreenFlash(Color.white, 0.4f, 0.04f, 0.45f);
        CrowDeathFx.Ring(body, room, new Color(0.9f, 0.8f, 1f, 0.95f), 0.8f, 8f, 0.8f);
        CrowDeathFx.FeatherBurst(body, room, 40, 2.5f, 7f, 1.9f, 4.5f, FeatherDark, FeatherViolet);

        // Se sacude y "grita": se hincha un poco mientras el color se va apagando.
        t = 0f;
        while (t < deathConvulseTime)
        {
            float k = Mathf.Clamp01(t / Mathf.Max(0.01f, deathConvulseTime));
            float swell = 1f + 0.14f * Mathf.Sin(k * Mathf.PI);
            SetVisualScale(new Vector3(swell, swell, 1f));

            if (visual != null)
            {
                visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 55f) * 7f * (1f - 0.4f * k));
                visual.localPosition = baseVisualPos + (Vector3)(Random.insideUnitCircle * 0.06f);
            }

            SetTint(Color.Lerp(Color.white, grayTint, k));
            t += Time.deltaTime;
            yield return null;
        }

        // Cae: la animacion de muerte de siempre (se aplasta y se hunde), ahora mas pesada y con plumas.
        bool collapseFeathers = false;
        t = 0f;
        while (t < deathCollapseTime)
        {
            float k = Mathf.Clamp01(t / Mathf.Max(0.01f, deathCollapseTime));
            float fall = k * k;
            SetVisualScale(new Vector3(1f + 0.1f * fall, 1f - 0.45f * fall, 1f));

            if (visual != null)
            {
                visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 30f) * 3f * (1f - k));
                visual.localPosition = baseVisualPos + fallenOffset * fall;
            }

            SetTint(grayTint);

            if (!collapseFeathers && k >= 0.45f)
            {
                collapseFeathers = true;
                CrowDeathFx.FeatherBurst(body, room, 14, 0.8f, 3f, 1.5f, 5f, FeatherDark, FeatherDark);
            }

            t += Time.deltaTime;
            yield return null;
        }

        if (visual != null)
        {
            visual.localRotation = Quaternion.identity;
            visual.localPosition = baseVisualPos + fallenOffset;
        }

        SetVisualScale(new Vector3(1.1f, 0.55f, 1f));

        // Golpe contra el piso.
        CameraShake.Play(0.35f, 0.2f);
        CrowDeathFx.FeatherBurst(body + Vector2.down * 0.4f, room, 16, 1.5f, 4f, 1.1f, 3f, FeatherDark, FeatherViolet);

        // ---------------------------------------------------------- 2) Queda tirado unos segundos
        yield return new WaitForSeconds(deathLieTime);

        // ---------------------------------------------------------- 3) La energia violeta se junta sobre el cuerpo
        CrowDeathFx.Converge(body, room, 46, 5.2f, rebirthChargeTime, EnergyViolet, EnergyPink);

        Color chargeTint = new Color(0.8f, 0.6f, 1f, 1f);
        float nextShake = 0f;
        t = 0f;
        while (t < rebirthChargeTime)
        {
            float k = Mathf.Clamp01(t / Mathf.Max(0.01f, rebirthChargeTime));

            // Temblores cada vez mas fuertes.
            if (t >= nextShake)
            {
                CameraShake.Play(0.28f, Mathf.Lerp(0.05f, 0.24f, k));
                nextShake += 0.22f;
            }

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * (10f + 14f * k));
            SetTint(Color.Lerp(grayTint, Color.Lerp(chargeTint, Color.white, pulse * 0.5f), k * k));

            if (visual != null)
                visual.localPosition = baseVisualPos + fallenOffset + (Vector3)(Random.insideUnitCircle * (0.02f + 0.07f * k));

            t += Time.deltaTime;
            yield return null;
        }

        // ---------------------------------------------------------- 4) RESURGE renovado en su segunda forma
        // La barra nueva se prepara ANTES de cambiar de fase: invisible y vacia (despues aparece con un fade y se llena).
        barOpacity = 0f;
        barFill = 0f;

        possessed = true;
        if (visualRenderer != null)
            visualRenderer.sprite = CrowArt.Body(true);
        SetTint(possessedTint);

        if (enemyHealth != null)
            enemyHealth.Refill(phaseTwoHealth);

        RunBarRoutine(BarRebirth());

        CameraShake.Play(1f, 0.45f);
        AudioManager.PlayBossHurt();
        CrowDeathFx.ScreenFlash(new Color(0.88f, 0.72f, 1f), 0.65f, 0.05f, 0.55f);
        CrowDeathFx.Ring(body, room, new Color(0.78f, 0.4f, 1f, 0.95f), 0.8f, 12f, 0.95f);
        CrowDeathFx.FeatherBurst(body, room, 55, 4f, 10f, 1.2f, 0f, EnergyViolet, EnergyPink);
        CrowDeathFx.FeatherBurst(body, room, 26, 2f, 6f, 1.8f, 3f, FeatherDark, FeatherViolet);
        StartCoroutine(DelayedRing(0.16f, body, room, new Color(1f, 0.7f, 1f, 0.8f), 0.6f, 8f, 0.8f));

        t = 0f;
        while (t < rebirthRiseTime)
        {
            float k = Mathf.Clamp01(t / Mathf.Max(0.01f, rebirthRiseTime));
            float rise = EaseOutBack(k);
            float pop = 1f + 0.18f * Mathf.Sin(k * Mathf.PI);

            SetVisualScale(new Vector3(Mathf.Lerp(1.1f, 1f, k) * pop, Mathf.Lerp(0.55f, 1f, rise) * pop, 1f));

            if (visual != null)
                visual.localPosition = Vector3.LerpUnclamped(baseVisualPos + fallenOffset, baseVisualPos, rise);

            t += Time.deltaTime;
            yield return null;
        }

        ResetVisual();
        phaseTransitionPending = false;
        RefreshDamageable();

        // Despues el BossRoutine sigue con su pausa normal y arranca los ataques de la segunda fase.
        yield return new WaitForSeconds(0.8f);
    }

    // ---------------------------------------------------------------- barra de vida: fades

    private void RunBarRoutine(IEnumerator routine)
    {
        if (barRoutine != null)
            StopCoroutine(barRoutine);

        barRoutine = StartCoroutine(routine);
    }

    /// <summary>La barra roja de la primera fase se apaga con un fade.</summary>
    private IEnumerator FadeBarOut(float startDelay, float fadeTime)
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        float from = barOpacity;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            barOpacity = Mathf.Lerp(from, 0f, Mathf.Clamp01(t / Mathf.Max(0.01f, fadeTime)));
            yield return null;
        }

        barOpacity = 0f;
        barRoutine = null;
    }

    /// <summary>La barra violeta de la segunda fase aparece con un fade y se llena de 0 al maximo.</summary>
    private IEnumerator BarRebirth()
    {
        barOpacity = 0f;
        barFill = 0f;

        float total = Mathf.Max(barFadeInTime, barFillTime);
        float t = 0f;
        while (t < total)
        {
            t += Time.deltaTime;
            barOpacity = Mathf.Clamp01(t / Mathf.Max(0.01f, barFadeInTime));

            float fill = Mathf.Clamp01(t / Mathf.Max(0.01f, barFillTime));
            barFill = 1f - (1f - fill) * (1f - fill);
            yield return null;
        }

        barOpacity = 1f;
        barFill = -1f; // desde ahora la barra muestra la vida real
        barRoutine = null;
    }

    private IEnumerator DelayedRing(float delay, Vector2 position, Transform parent, Color color, float startDiameter, float endDiameter, float time)
    {
        yield return new WaitForSeconds(delay);
        CrowDeathFx.Ring(position, parent, color, startDiameter, endDiameter, time);
    }

    private void SetTint(Color color)
    {
        if (damageFlash != null)
            damageFlash.SetPersistentTint(color);
        else if (visualRenderer != null)
            visualRenderer.color = color;
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float p = x - 1f;
        return 1f + c3 * p * p * p + c1 * p * p;
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
