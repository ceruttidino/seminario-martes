using UnityEngine;

/// <summary>
/// Hormiga Hambrienta (GDD "Hungry Ant" - rework).
///
/// - Se mueve SOLA de forma intermitente: periodos de quietud y periodos de movimiento en los que recorre entre 6 y 7 tiles.
/// - Si el jugador entra en su rango de deteccion (1.5 m) se alerta (estira el cuello) y lo PERSIGUE mientras hace un
///   ataque en area continuo (el cuello gira como loco) durante ~5 segundos.
/// - Al terminar el ataque queda MAREADA / aturdida 3 segundos (es cuando se la puede golpear).
/// - Mientras esta atacando es intocable: si el jugador intenta golpearla recibe un knockback fuerte y NO le hace daño.
///
/// Toda la logica (estados, animacion por sprites, muerte) vive en este componente: no usa Animator ni la maquina de estados
/// de EnemyBehaviour. El estado es por timers en Update (sin corrutinas) para que EnemySummon.Hold/Release (que apaga y
/// prende los componentes durante la nube de aparicion) no deje a la hormiga a medias.
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyMovement))]
public class HungryAnt : MonoBehaviour
{
    private enum AntState { Still, Wandering, Alert, Attacking, Recovering, Dizzy }

    private enum WalkFacing { Front, Back, Left, Right }

    [Header("Movimiento autonomo e intermitente (GDD)")]
    [Tooltip("Tamaño de un tile en unidades de mundo.")]
    [SerializeField] private float tileSize = 1f;
    [Tooltip("GDD: cada tramo de movimiento cubre entre 6 y 7 tiles.")]
    [SerializeField] private float minWanderTiles = 6f;
    [SerializeField] private float maxWanderTiles = 7f;
    [SerializeField] private float wanderSpeed = 2.4f;
    [Tooltip("Periodos de quietud entre tramos de movimiento (segundos).")]
    [SerializeField] private float minStillTime = 1.2f;
    [SerializeField] private float maxStillTime = 2.4f;

    [Header("Deteccion y ataque (GDD)")]
    [Tooltip("GDD: rango de deteccion de 1.5 metros.")]
    [SerializeField] private float detectionRange = 1.5f;
    [Tooltip("Al ver al jugador se alerta: estira el cuello (animacion de preparacion) antes de empezar a girar.")]
    [SerializeField] private float alertTime = 0.5f;
    [Tooltip("GDD: el ataque dura aproximadamente 5 segundos (incluye el aviso).")]
    [SerializeField] private float attackDuration = 5f;
    [SerializeField] private float attackMoveSpeed = 2.8f;
    [Tooltip("Radio del ataque en area (el cuello girando), medido desde el cuerpo de la hormiga hasta el cuerpo del jugador.")]
    [SerializeField] private float aoeRadius = 0.9f;
    [SerializeField] private float aoeTickInterval = 0.4f;
    [Tooltip("GDD: los enemigos le sacan medio corazon al jugador con sus ataques.")]
    [SerializeField] private float aoeDamage = 1f;
    [SerializeField] private float recoverTime = 0.25f;
    [Tooltip("GDD: al terminar el ataque la hormiga queda mareada/aturdida 3 segundos.")]
    [SerializeField] private float dizzyDuration = 3f;
    [Tooltip("Tiempo despues de marearse durante el cual no vuelve a detectar al jugador.")]
    [SerializeField] private float aggroCooldownAfterDizzy = 1f;

    [Header("Golpear a la hormiga mientras ataca (GDD)")]
    [Tooltip("Cuanto sale despedido el jugador (unidades) cuando intenta golpearla estando en estado de ataque.")]
    [SerializeField] private float knockbackDistance = 3.2f;
    [SerializeField] private float knockbackDuration = 0.28f;

    [Header("Animaciones (sprites)")]
    [SerializeField] private Sprite[] frontFrames;
    [SerializeField] private Sprite[] backFrames;
    [SerializeField] private Sprite[] leftFrames;
    [SerializeField] private Sprite[] rightFrames;
    [Tooltip("Hoja completa del ataque: frames 0-5 = estira el cuello (aviso), 6-13 = cuello girando (se repite), 14 = vuelve a la normalidad.")]
    [SerializeField] private Sprite[] attackFrames;
    [SerializeField] private int attackWindupFrameCount = 6;
    [SerializeField] private int attackSpinFrameCount = 8;
    [Tooltip("Hoja 'completa' (frente, derecha, espalda, izquierda): se usa girando para el mareo.")]
    [SerializeField] private Sprite[] dizzyFrames;
    [SerializeField] private Sprite[] deathFrames;
    [SerializeField] private float walkFps = 12f;
    [SerializeField] private float spinFps = 20f;
    [SerializeField] private float dizzyFps = 6f;
    [SerializeField] private float deathFps = 14f;

    private const int ClipWalk = 1;
    private const int ClipWindup = 2;
    private const int ClipSpin = 3;
    private const int ClipRecover = 4;
    private const int ClipDizzy = 5;

    private EnemyHealth health;
    private EnemyMovement movement;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D bodyCollider;
    private Animator legacyAnimator;

    private Transform player;
    private PlayerHealth playerHealth;
    private PlayerMovement playerMovement;
    private QuickAttack playerQuickAttack;
    private AreaAttack playerAreaAttack;

    private AntState state = AntState.Still;
    private float stateTimer;
    private float aggroGrace;
    private float aoeTimer;
    private float nextKnockbackTime;
    private bool autonomous = true;
    private bool started;

    // Movimiento
    private Vector2 wanderTarget;
    private float wanderDistance;
    private float wanderTravelled;
    private Vector2 lastPosition;
    private WalkFacing facing = WalkFacing.Front;

    // Animacion por sprites
    private Sprite[] windupFrames;
    private Sprite[] spinFrames;
    private Sprite[] recoverFrames;
    private Sprite[] currentFrames;
    private int currentClip;
    private float currentFps;
    private bool currentLoop;
    private bool currentPaused;
    private float clipTime;

    // Mareo: estrellitas girando sobre la cabeza
    private Transform starsRoot;
    private SpriteRenderer[] stars;
    private static Sprite cachedStar;

    // Daño en area
    private ContactFilter2D playerFilter;
    private bool playerFilterReady;
    private readonly Collider2D[] hitBuffer = new Collider2D[8];

    /// <summary>Daño del ataque en area (lo usa ChallengeContainerHunter).</summary>
    public float AoeDamage => aoeDamage;

    // ================================================================= ciclo de vida

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        movement = GetComponent<EnemyMovement>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<Collider2D>();
        legacyAnimator = GetComponent<Animator>();
        if (legacyAnimator != null)
            legacyAnimator.enabled = false;

        BuildAttackClips();

        if (health != null)
        {
            health.OnDeath += HandleDeath;
            health.OnDamageBlocked += HandleHitBlocked;
            health.DamageBlocker = ShouldBlockDamage;
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDeath -= HandleDeath;
            health.OnDamageBlocked -= HandleHitBlocked;
            health.DamageBlocker = null;
        }

        if (starsRoot != null)
            Destroy(starsRoot.gameObject);
    }

    private void OnEnable()
    {
        // Se re-ejecuta al terminar la nube de aparicion (EnemySummon.Release): arranca quieta un ratito.
        // En la primera activacion otros componentes (EnemyMovement) puede que todavia no hayan hecho su Awake:
        // por eso aca solo se preparan los datos y el resto se hace en Start / Update.
        ShowStars(false);

        state = AntState.Still;
        stateTimer = Random.Range(0.4f, 1.4f);
        lastPosition = rb != null ? rb.position : (Vector2)transform.position;

        if (started)
            PlayWalk(WalkFacing.Front, true);
    }

    private void Start()
    {
        started = true;
        PlayWalk(WalkFacing.Front, true);
    }

    private void OnDisable()
    {
        ShowStars(false);
    }

    private void BuildAttackClips()
    {
        windupFrames = Slice(attackFrames, 0, attackWindupFrameCount);
        spinFrames = Slice(attackFrames, attackWindupFrameCount, attackSpinFrameCount);

        int recoverStart = attackWindupFrameCount + attackSpinFrameCount;
        recoverFrames = attackFrames != null && attackFrames.Length > recoverStart
            ? Slice(attackFrames, recoverStart, attackFrames.Length - recoverStart)
            : null;
    }

    private static Sprite[] Slice(Sprite[] source, int start, int count)
    {
        if (source == null || count <= 0 || start >= source.Length)
            return null;

        count = Mathf.Min(count, source.Length - start);
        Sprite[] result = new Sprite[count];
        for (int i = 0; i < count; i++)
            result[i] = source[start + i];

        return result;
    }

    // ================================================================= modo pasivo (challenge del Supercontainer)

    /// <summary>
    /// La hormiga deja de ser autonoma (por ejemplo cuando un challenge la convierte en "cazador" del contenedor): solo
    /// sigue animandose segun hacia donde la mueven desde afuera.
    /// </summary>
    public void EnterPassiveMode()
    {
        autonomous = false;
        ShowStars(false);
    }

    // ================================================================= update

    private void Update()
    {
        if (health != null && health.IsDead)
            return;

        float dt = Time.deltaTime;

        if (!autonomous)
        {
            PassiveAnimation();
            TickClip(dt);
            return;
        }

        EnsurePlayer();

        if (aggroGrace > 0f)
            aggroGrace -= dt;

        if (playerHealth != null && playerHealth.IsDead && state != AntState.Still && state != AntState.Wandering)
        {
            // Si el jugador murio se aquieta: no sigue girando sobre un cadaver.
            movement.Move(Vector2.zero);
            ShowStars(false);
            EnterStill(2f);
        }

        switch (state)
        {
            case AntState.Still: UpdateStill(dt); break;
            case AntState.Wandering: UpdateWandering(dt); break;
            case AntState.Alert: UpdateAlert(dt); break;
            case AntState.Attacking: UpdateAttacking(dt); break;
            case AntState.Recovering: UpdateRecovering(dt); break;
            case AntState.Dizzy: UpdateDizzy(dt); break;
        }

        TickClip(dt);
    }

    private void LateUpdate()
    {
        // EnemyMovement espeja el sprite segun hacia donde camina (el arte viejo miraba a un solo lado). Las hojas nuevas ya
        // tienen vista izquierda y derecha propias: no se espeja nunca.
        if (sr != null)
            sr.flipX = false;

        if (legacyAnimator != null && legacyAnimator.enabled)
            legacyAnimator.enabled = false;
    }

    // ================================================================= estados

    private void EnterStill(float duration)
    {
        state = AntState.Still;
        stateTimer = duration;
        if (movement != null)
            movement.Move(Vector2.zero);
        PlayWalk(facing, true);
    }

    private void EnterStill()
    {
        EnterStill(Random.Range(minStillTime, maxStillTime));
    }

    private void UpdateStill(float dt)
    {
        movement.Move(Vector2.zero);

        if (TryDetectPlayer())
        {
            BeginAlert();
            return;
        }

        stateTimer -= dt;
        if (stateTimer <= 0f)
            BeginWander();
    }

    private void BeginWander()
    {
        if (!TryPickWanderTarget(out wanderTarget))
        {
            // No hay lugar libre para un tramo largo: se queda quieta un poco mas y reintenta.
            EnterStill(0.8f);
            return;
        }

        state = AntState.Wandering;
        wanderTravelled = 0f;
        lastPosition = rb != null ? rb.position : (Vector2)transform.position;

        // Tiempo maximo del tramo (por si se traba): el doble de lo que tardaria + un margen.
        stateTimer = (wanderDistance / Mathf.Max(0.1f, wanderSpeed)) * 2f + 1.5f;
    }

    private void UpdateWandering(float dt)
    {
        if (TryDetectPlayer())
        {
            BeginAlert();
            return;
        }

        Vector2 position = rb != null ? rb.position : (Vector2)transform.position;
        wanderTravelled += Vector2.Distance(position, lastPosition);
        lastPosition = position;

        stateTimer -= dt;

        Vector2 toTarget = wanderTarget - position;
        if (toTarget.magnitude <= 0.3f || wanderTravelled >= wanderDistance || stateTimer <= 0f)
        {
            movement.Move(Vector2.zero);
            EnterStill();
            return;
        }

        movement.MoveTowards(wanderTarget, wanderSpeed);

        Vector2 velocity = rb != null ? rb.linearVelocity : Vector2.zero;
        Vector2 heading = velocity.sqrMagnitude > 0.04f ? velocity : toTarget;
        PlayWalk(FacingFor(heading), false);
    }

    private void BeginAlert()
    {
        state = AntState.Alert;
        stateTimer = Mathf.Max(0.05f, alertTime);
        movement.Move(Vector2.zero);

        // Desde que se alerta es intocable para los golpes del jugador (ver ShouldBlockDamage).
        float fps = windupFrames != null && windupFrames.Length > 0 ? windupFrames.Length / stateTimer : 12f;
        PlayClip(ClipWindup, windupFrames, fps, false, false);
    }

    private void UpdateAlert(float dt)
    {
        movement.Move(Vector2.zero);

        stateTimer -= dt;
        if (stateTimer <= 0f)
            BeginAttacking();
    }

    private void BeginAttacking()
    {
        state = AntState.Attacking;
        stateTimer = Mathf.Max(0.5f, attackDuration - alertTime);
        aoeTimer = 0f;
        PlayClip(ClipSpin, spinFrames, spinFps, true, false);
    }

    private void UpdateAttacking(float dt)
    {
        // Persigue al jugador mientras el cuello gira.
        if (player != null)
            movement.MoveTowards(player.position, attackMoveSpeed);
        else
            movement.Move(Vector2.zero);

        // Daño en area continuo (el jugador tiene sus propios frames de invulnerabilidad).
        aoeTimer -= dt;
        if (aoeTimer <= 0f)
        {
            aoeTimer = Mathf.Max(0.05f, aoeTickInterval);
            DoAreaDamage();
        }

        stateTimer -= dt;
        if (stateTimer <= 0f)
            BeginRecover();
    }

    private void BeginRecover()
    {
        state = AntState.Recovering;
        stateTimer = Mathf.Max(0.05f, recoverTime);
        movement.Move(Vector2.zero);
        PlayClip(ClipRecover, recoverFrames != null ? recoverFrames : frontFrames, 8f, false, false);
    }

    private void UpdateRecovering(float dt)
    {
        movement.Move(Vector2.zero);

        stateTimer -= dt;
        if (stateTimer <= 0f)
            BeginDizzy();
    }

    private void BeginDizzy()
    {
        state = AntState.Dizzy;
        stateTimer = Mathf.Max(0.1f, dizzyDuration);
        movement.Move(Vector2.zero);

        // Mareada: ahora SI se la puede golpear (ShouldBlockDamage deja de bloquear).
        PlayClip(ClipDizzy, dizzyFrames != null && dizzyFrames.Length > 0 ? dizzyFrames : frontFrames, dizzyFps, true, false);
        ShowStars(true);
    }

    private void UpdateDizzy(float dt)
    {
        movement.Move(Vector2.zero);
        AnimateStars();

        stateTimer -= dt;
        if (stateTimer <= 0f)
        {
            ShowStars(false);
            aggroGrace = aggroCooldownAfterDizzy;
            facing = WalkFacing.Front;
            EnterStill();
        }
    }

    // ================================================================= deteccion y daño

    private void EnsurePlayer()
    {
        if (player != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
            return;

        player = playerObject.transform;
        playerHealth = playerObject.GetComponent<PlayerHealth>();
        playerMovement = playerObject.GetComponent<PlayerMovement>();
        playerQuickAttack = playerObject.GetComponent<QuickAttack>();
        playerAreaAttack = playerObject.GetComponent<AreaAttack>();
    }

    private Vector2 BodyCenter()
    {
        return bodyCollider != null && bodyCollider.enabled ? (Vector2)bodyCollider.bounds.center : (Vector2)transform.position;
    }

    private bool TryDetectPlayer()
    {
        if (aggroGrace > 0f || player == null)
            return false;

        if (playerHealth != null && playerHealth.IsDead)
            return false;

        Vector2 delta = (Vector2)player.position - BodyCenter();
        return delta.sqrMagnitude <= detectionRange * detectionRange;
    }

    private void DoAreaDamage()
    {
        if (!playerFilterReady)
        {
            playerFilter = new ContactFilter2D();
            playerFilter.useLayerMask = true;
            playerFilter.layerMask = LayerMask.GetMask("Player");
            playerFilter.useTriggers = true;
            playerFilterReady = true;
        }

        // El jugador recibe daño si su cuerpo toca el circulo del ataque (el cuello barre todo ese radio).
        int count = Physics2D.OverlapCircle(BodyCenter(), aoeRadius, playerFilter, hitBuffer);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = hitBuffer[i];
            if (hit == null)
                continue;

            PlayerHealth target = hit.GetComponentInParent<PlayerHealth>();
            if (target == null)
                continue;

            target.TakeDamage(aoeDamage);
            return;
        }
    }

    // ================================================================= golpear a la hormiga mientras ataca

    private bool IsInAttackState()
    {
        return state == AntState.Alert || state == AntState.Attacking || state == AntState.Recovering;
    }

    private bool PlayerIsAttacking()
    {
        return (playerQuickAttack != null && playerQuickAttack.IsAttacking)
            || (playerAreaAttack != null && playerAreaAttack.IsAttacking);
    }

    /// <summary>
    /// GDD: mientras la hormiga esta en estado de ataque los golpes del jugador NO le hacen daño. Solo se bloquea el daño
    /// que llega mientras el jugador esta en medio de un ataque propio (rapido o pesado): cualquier otra fuente de daño,
    /// como la explosion del Erizo, sigue afectandola.
    /// </summary>
    private bool ShouldBlockDamage()
    {
        if (!autonomous || !IsInAttackState())
            return false;

        EnsurePlayer();
        return PlayerIsAttacking();
    }

    /// <summary>Se bloqueo un golpe del jugador: el jugador rebota con fuerza (knockback) y la hormiga no recibe nada.</summary>
    private void HandleHitBlocked()
    {
        if (!autonomous || !IsInAttackState())
            return;

        if (Time.time < nextKnockbackTime)
            return;

        EnsurePlayer();
        if (player == null)
            return;

        nextKnockbackTime = Time.time + 0.25f;

        Vector2 away = (Vector2)player.position - BodyCenter();
        if (away.sqrMagnitude < 0.0001f)
            away = Vector2.down;

        if (playerMovement != null)
            playerMovement.ApplyKnockback(away, knockbackDistance, knockbackDuration);

        CameraShake.Play(0.12f, 0.1f);
    }
    // ================================================================= movimiento autonomo

    private bool TryPickWanderTarget(out Vector2 target)
    {
        Vector2 from = rb != null ? rb.position : (Vector2)transform.position;
        RoomPathGrid grid = RoomPathGrid.For(transform);
        float baseDistance = Random.Range(minWanderTiles, maxWanderTiles) * Mathf.Max(0.1f, tileSize);

        // Primero se busca un tramo completo de 6-7 tiles; si la sala no da en ninguna direccion se acorta de a poco.
        for (float factor = 1f; factor >= 0.45f; factor -= 0.2f)
        {
            float distance = baseDistance * factor;
            float startAngle = Random.Range(0f, 360f);

            for (int i = 0; i < 12; i++)
            {
                float angle = (startAngle + i * 30f) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 candidate = from + direction * distance;
                if (DungeonNavMesh.SampleWalkable(candidate, out Vector2 snapped, 0.8f))
                    candidate = snapped;

                bool blocked = DungeonNavMesh.Instance != null && DungeonNavMesh.Instance.IsReady
                    ? !DungeonNavMesh.HasClearLine(from, candidate)
                    : grid != null && (!grid.IsWalkableWorld(candidate) || !grid.HasClearLine(from, candidate, 0.3f));
                if (blocked)
                    continue;

                target = candidate;
                wanderDistance = distance;
                return true;
            }
        }

        target = from;
        wanderDistance = 0f;
        return false;
    }

    // ================================================================= animacion por sprites

    /// <summary>Hoja de caminata segun hacia donde va. Con histeresis para que no parpadee entre izquierda/atras al ir en diagonal.</summary>
    private WalkFacing FacingFor(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return facing;

        float absX = Mathf.Abs(direction.x);
        float absY = Mathf.Abs(direction.y);
        bool wasHorizontal = facing == WalkFacing.Left || facing == WalkFacing.Right;
        bool horizontal = wasHorizontal ? absX > absY * 0.8f : absX > absY * 1.25f;

        if (horizontal)
            return direction.x > 0f ? WalkFacing.Right : WalkFacing.Left;

        return direction.y > 0f ? WalkFacing.Back : WalkFacing.Front;
    }

    private Sprite[] WalkFramesFor(WalkFacing walkFacing)
    {
        switch (walkFacing)
        {
            case WalkFacing.Back: return backFrames;
            case WalkFacing.Left: return leftFrames;
            case WalkFacing.Right: return rightFrames;
            default: return frontFrames;
        }
    }

    /// <summary>Camina en la direccion indicada (si 'standStill' queda en el primer frame, sin animar).</summary>
    private void PlayWalk(WalkFacing walkFacing, bool standStill)
    {
        facing = walkFacing;
        Sprite[] frames = WalkFramesFor(walkFacing);
        if (frames == null || frames.Length == 0)
            frames = frontFrames;
        if (frames == null || frames.Length == 0)
            return;

        // Al cambiar de hoja se mantiene la fase de la caminata (las hojas tienen 4 o 5 frames).
        float phase = 0f;
        bool sameClip = currentClip == ClipWalk && ReferenceEquals(currentFrames, frames);
        if (!sameClip && currentClip == ClipWalk && currentFrames != null && currentFrames.Length > 0)
            phase = Mathf.Repeat(clipTime, currentFrames.Length) / currentFrames.Length;

        if (!sameClip)
        {
            currentClip = ClipWalk;
            currentFrames = frames;
            currentFps = walkFps;
            currentLoop = true;
            clipTime = phase * frames.Length;
        }

        if (standStill)
        {
            currentPaused = true;
            clipTime = 0f;
        }
        else
        {
            currentPaused = false;
        }

        ApplyFrame();
    }

    private void PlayClip(int clip, Sprite[] frames, float fps, bool loop, bool paused)
    {
        if (frames == null || frames.Length == 0)
            return;

        currentClip = clip;
        currentFrames = frames;
        currentFps = Mathf.Max(0.1f, fps);
        currentLoop = loop;
        currentPaused = paused;
        clipTime = 0f;
        ApplyFrame();
    }

    private void TickClip(float dt)
    {
        if (currentFrames == null || currentFrames.Length == 0)
            return;

        if (!currentPaused)
            clipTime += dt * currentFps;

        ApplyFrame();
    }

    private void ApplyFrame()
    {
        if (sr == null || currentFrames == null || currentFrames.Length == 0)
            return;

        int index = Mathf.FloorToInt(clipTime);
        index = currentLoop ? index % currentFrames.Length : Mathf.Min(index, currentFrames.Length - 1);
        if (index < 0)
            index = 0;

        Sprite frame = currentFrames[index];
        if (frame != null && sr.sprite != frame)
            sr.sprite = frame;
    }

    /// <summary>Modo pasivo: la hormiga camina animada segun la velocidad que le imponen desde afuera.</summary>
    private void PassiveAnimation()
    {
        Vector2 velocity = rb != null ? rb.linearVelocity : Vector2.zero;
        if (velocity.sqrMagnitude > 0.04f)
            PlayWalk(FacingFor(velocity), false);
        else
            PlayWalk(facing, true);
    }

    // ================================================================= mareo: estrellitas

    private void ShowStars(bool show)
    {
        if (show && starsRoot == null)
            BuildStars();

        if (starsRoot != null)
            starsRoot.gameObject.SetActive(show);
    }

    private void BuildStars()
    {
        GameObject root = new GameObject("DizzyStars");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = new Vector3(0f, 0.58f, 0f);
        starsRoot = root.transform;

        const int count = 3;
        stars = new SpriteRenderer[count];
        for (int i = 0; i < count; i++)
        {
            GameObject star = new GameObject("Star" + i);
            star.transform.SetParent(starsRoot, false);
            star.transform.localScale = Vector3.one * 1.5f;

            SpriteRenderer renderer = star.AddComponent<SpriteRenderer>();
            renderer.sprite = StarSprite();
            renderer.sortingOrder = sr != null ? sr.sortingOrder + 2 : 6;
            if (sr != null)
                renderer.sharedMaterial = sr.sharedMaterial;

            stars[i] = renderer;
        }

        AnimateStars();
    }

    private void AnimateStars()
    {
        if (stars == null)
            return;

        float time = Time.time * 4f;
        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] == null)
                continue;

            float angle = time + i * (Mathf.PI * 2f / stars.Length);
            stars[i].transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.3f, Mathf.Sin(angle) * 0.1f, 0f);
            float twinkle = 0.85f + Mathf.Sin(time * 2.2f + i) * 0.15f;
            stars[i].transform.localScale = Vector3.one * (1.5f * twinkle);
        }
    }

    private static Sprite StarSprite()
    {
        if (cachedStar != null)
            return cachedStar;

        string[] rows =
        {
            "...Y...",
            "...Y...",
            "YYYYYYY",
            ".YYYYY.",
            "..YYY..",
            ".YY.YY.",
            ".Y...Y."
        };

        Texture2D texture = new Texture2D(7, 7, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 yellow = new Color32(255, 226, 74, 255);
        Color32 outline = new Color32(120, 70, 10, 255);
        Color32[] pixels = new Color32[49];

        for (int y = 0; y < 7; y++)
        {
            string row = rows[6 - y];
            for (int x = 0; x < 7; x++)
                pixels[y * 7 + x] = row[x] == 'Y' ? yellow : clear;
        }

        // Contorno de 1 px para que se lea sobre cualquier fondo.
        Color32[] copy = (Color32[])pixels.Clone();
        for (int y = 0; y < 7; y++)
        {
            for (int x = 0; x < 7; x++)
            {
                if (copy[y * 7 + x].a != 0)
                    continue;

                bool near = (x > 0 && copy[y * 7 + x - 1].a != 0) || (x < 6 && copy[y * 7 + x + 1].a != 0)
                    || (y > 0 && copy[(y - 1) * 7 + x].a != 0) || (y < 6 && copy[(y + 1) * 7 + x].a != 0);
                if (near)
                    pixels[y * 7 + x] = outline;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        cachedStar = Sprite.Create(texture, new Rect(0f, 0f, 7f, 7f), new Vector2(0.5f, 0.5f), 64f);
        return cachedStar;
    }

    // ================================================================= muerte

    private void HandleDeath()
    {
        if (deathFrames == null || deathFrames.Length == 0)
            return;

        // La hormiga se destruye al morir: la animacion de muerte la reproduce un objeto aparte que se limpia solo.
        Transform parent = GetComponentInParent<RoomInstance>() != null ? GetComponentInParent<RoomInstance>().transform : transform.parent;
        AntDeathFx.Spawn(deathFrames, deathFps, transform.position, transform.lossyScale, sr, parent);
    }
}
