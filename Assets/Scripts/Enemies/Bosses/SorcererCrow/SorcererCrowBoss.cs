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
///   1) Orbes espectrales: 5 orbes (0.7 s entre cada una). Solo la ULTIMA se puede desviar con el ATAQUE
///      PESADO (E); si vuelve y le pega, el cuervo recibe daño y queda aturdido 2.5 s (ahi se lo golpea).
///   2) Portal y brazo: se escapa por un portal, abre un portal en el piso donde esta el player (aviso con
///      temblor) y saca un brazo. Hay 0.7 s para golpear el brazo antes de que se retraiga.
///   3) Chillido: desaparece y suelta cuervitos desde los costados que vuelan por el eje X. Los negros no
///      se pueden golpear; los blancos (versiones chicas del jefe) si, y dañan al cuervo.
/// Estado Poseido: 8 orbes al doble de velocidad (0.5 s entre cada una), 5 portales/brazos (0.7 s entre cada
/// uno, todos quedan hasta que aparece el ultimo) y muchos mas cuervitos a la vez.
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
    [SerializeField] private float orbDelay = 0.7f;
    [SerializeField] private float orbSpeed = 5.5f;
    [SerializeField] private float orbScale = 1.3f;
    [SerializeField] private int orbDamage = 1;
    [SerializeField] private float orbTellTime = 0.6f;
    [Tooltip("Que tan cerca del player tiene que estar la orbe para poder desviarla con el ataque pesado.")]
    [SerializeField] private float deflectRadius = 1.5f;
    [SerializeField] private float reflectedOrbSpeed = 13f;
    [Tooltip("Daño que recibe el cuervo cuando le devuelven la orbe.")]
    [SerializeField] private float deflectDamage = 30f;
    [SerializeField] private float stunDuration = 2.5f;

    [Header("Ataque 2 - Portal y brazo")]
    [SerializeField] private float armTellTime = 0.9f;
    [SerializeField] private float armWindow = 0.7f;
    [SerializeField] private float armSpawnDelay = 0.7f;
    [SerializeField] private int armContactDamage = 2;
    [SerializeField] private float armShake = 0.07f;

    [Header("Ataque 3 - Chillido y cuervitos")]
    [SerializeField] private float ravenDuration = 6f;
    [SerializeField] private float ravenInterval = 0.45f;
    [SerializeField] private float ravenSpeed = 6.5f;
    [SerializeField] private int ravenDamage = 1;
    [SerializeField] private float ravenTellTime = 0.45f;
    [Range(0f, 1f)]
    [SerializeField] private float whiteRavenChance = 0.2f;
    [Tooltip("Como minimo sale un cuervito blanco cada tantos cuervitos.")]
    [SerializeField] private int whiteRavenAtLeastEvery = 5;

    [Header("Estado Poseido (versiones mas fuertes)")]
    [SerializeField] private int possessedOrbCount = 8;
    [SerializeField] private float possessedOrbDelay = 0.5f;
    [SerializeField] private float possessedOrbSpeedMultiplier = 2f;
    [SerializeField] private int possessedArmCount = 5;
    [SerializeField] private float possessedRavenDuration = 7f;
    [SerializeField] private float possessedRavenInterval = 0.3f;
    [SerializeField] private int possessedRavenBurst = 3;
    [SerializeField] private float possessedRavenSpeedMultiplier = 1.35f;
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

        CrowOrb orb = CrowOrb.Spawn(this, mouth, direction, speed, orbDamage, deflectable, deflectRadius,
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

        yield return EscapeThroughPortal();
        if (ShouldAbort) yield break;

        List<CrowArm> arms = new List<CrowArm>();

        for (int i = 0; i < armCount; i++)
        {
            if (ShouldAbort) yield break;

            Vector2 target = ClampToArena((Vector2)player.position + Vector2.down * 0.35f);

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
        Vector2 portalPosition = (Vector2)transform.position + Vector2.down * 1.25f;
        CrowFx.Portal(portalPosition, RoomTransform(), 0.35f, 1.0f, 0.3f, 2.8f);
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
    }

    private IEnumerator ReturnThroughPortal()
    {
        Vector2 portalPosition = (Vector2)transform.position + Vector2.down * 1.25f;
        CrowFx.Portal(portalPosition, RoomTransform(), 0.35f, 0.6f, 0.3f, 2.8f);
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
    }

    // ================================================================= ataque 3: chillido y cuervitos

    private IEnumerator RavenAttack()
    {
        float duration = possessed ? possessedRavenDuration : ravenDuration;
        float interval = possessed ? possessedRavenInterval : ravenInterval;
        int burst = possessed ? possessedRavenBurst : 1;
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

        // Enjambre
        float end = Time.time + duration;
        int sinceWhite = 0;

        while (Time.time < end && !ShouldAbort)
        {
            for (int i = 0; i < burst; i++)
            {
                bool white = sinceWhite >= whiteRavenAtLeastEvery || Random.value < whiteRavenChance;
                sinceWhite = white ? 0 : sinceWhite + 1;
                pendingRavenLaunches++;
                StartCoroutine(LaunchRaven(white, speed));
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

    private IEnumerator LaunchRaven(bool white, float speed)
    {
        Vector2 center = RoomCenter();
        float side = Random.value < 0.5f ? -1f : 1f;

        // Muchos van hacia donde esta el player para obligarlo a moverse.
        float y;
        if (player != null && Random.value < 0.6f)
            y = player.position.y + Random.Range(-0.7f, 0.7f);
        else
            y = center.y + Random.Range(arenaMinY, arenaMaxY);

        y = Mathf.Clamp(y, center.y + arenaMinY, center.y + arenaMaxY);

        float startX = center.x + side * (arenaHalfWidth + 0.4f);
        float travelHalfWidth = arenaHalfWidth + 1.6f;

        // Aviso: barra translucida por donde va a pasar.
        float tell = ravenTellTime;
        Color warn = white ? new Color(1f, 1f, 1f, 0.28f) : new Color(1f, 0.15f, 0.15f, 0.3f);
        GameObject bar = CrowFx.Bar(new Vector2(center.x, y), arenaHalfWidth * 2f + 1f, 0.45f, warn, tell * 0.4f, tell * 0.6f, 0.15f, RoomTransform());
        Track(bar);

        yield return new WaitForSeconds(tell);

        if (ShouldAbort)
        {
            pendingRavenLaunches = Mathf.Max(0, pendingRavenLaunches - 1);
            yield break;
        }

        CrowRaven raven = CrowRaven.Spawn(this, white, new Vector2(startX, y), -side, speed, ravenDamage,
            center.x, travelHalfWidth, RoomTransform());
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
