using System;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour, IMovement
{
    [SerializeField] private float speed = 3f;
    public float Speed => speed;
    private Rigidbody2D rb;
    private HitReaction hitReaction; // NUEVO

    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Obstacle Avoidance")]
    [Tooltip("Capas solidas (paredes, piedras, bolsas de basura, etc.) que el enemigo esquiva en vez de empujar contra ellas. Los triggers (puertas, zonas de interaccion) se ignoran siempre, sin importar la capa.")]
    [SerializeField] private LayerMask obstacleLayers = 1089; // Default + Trash + Wall
    [Tooltip("Radio del sondeo circular usado para detectar obstaculos por delante.")]
    [SerializeField] private float avoidanceRadius = 0.3f;
    [Tooltip("Distancia hacia adelante que se sondea antes de moverse.")]
    [SerializeField] private float avoidanceLookahead = 0.6f;

    [Header("Stuck Escape")]
    [Tooltip("Si el enemigo intenta moverse pero avanza menos que esto en StuckTimeWindow segundos, se lo considera trabado (tipico entre grupos de rocas) y se fuerza una direccion de escape distinta a la habitual.")]
    [SerializeField] private float stuckDistanceThreshold = 0.15f;
    [SerializeField] private float stuckTimeWindow = 0.4f;
    [SerializeField] private float escapeDuration = 0.5f;

    private static readonly float[] AvoidanceProbeAngles =
        {
            0f, 20f, -20f, 40f, -40f, 60f, -60f, 80f, -80f,
            100f, -100f, 120f, -120f, 140f, -140f, 160f, -160f, 180f
        };

    private static readonly RaycastHit2D[] AvoidanceHitBuffer = new RaycastHit2D[1];
    private readonly Collider2D[] overlapBuffer = new Collider2D[8];

    private ContactFilter2D obstacleFilter;
    private Vector2 lastFacingDirection = Vector2.down;

    private Vector2 stuckWindowStartPos;
    private float stuckWindowTimer;
    private float escapeTimer;
    private float escapeAngleJitter;

    private Vector2 cachedPathStep;
    private Vector2 lastPathGoal;
    private float nextRepathTime;
    private Vector2 smoothedDirection;
    private bool hasSmoothedDirection;
    private bool hasExplicitGoal;
    private Vector2 explicitGoal;

    // El NavMeshAgent solo calcula el camino y el obstacle avoidance. No mueve el transform:
    // updatePosition queda en false y el paso se aplica a mano, recortado a la malla.
    // Un Rigidbody dinamico pisa ese resultado (por eso el agente va en kinematic).
    private NavMeshAgent agent;
    private Vector2 pendingDirection;
    private float pendingSpeed;
    private bool reportWalls;
    private int moveStamp = -1;
    private bool wasBlocked;
    private Vector2 separation;
    private Vector2 pendingGoal;
    private bool pendingGoalValid;
    private int wallSlideSign = 1;
    private float bodyRadius = 0.32f;
    // La rata tiene una hoja por dirección: no se espeja, y el blend solo recibe un eje (arriba, abajo, izquierda o derecha).
    private bool directionalSheets;
    // Rata y búho rodean las piedras por la malla. El resto sigue con el agente de antes.
    private bool steerAroundRocks;

    private Vector2 slideVelocity;
    private float slideTimer;
    private float slideDuration;

    /// <summary>Velocidad de mundo de este frame. La animacion la lee aca: el Rigidbody queda en cero a proposito.</summary>
    public Vector2 Velocity { get; private set; }

    public Vector2 Facing => lastFacingDirection;

    public bool IsSliding => slideTimer > 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hitReaction = GetComponent<HitReaction>(); // NUEVO

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Las puertas son triggers en capa Wall: si las contamos, los enemigos las "esquivan" y no cruzan.
        obstacleLayers = LayerMask.GetMask("Default", "Trash", "Wall");
        if (obstacleLayers.value == 0)
            obstacleLayers = 1089;

        obstacleFilter = new ContactFilter2D();
        obstacleFilter.useTriggers = false;
        obstacleFilter.SetLayerMask(obstacleLayers);

        stuckWindowStartPos = rb != null ? rb.position : (Vector2)transform.position;

        // La rata y el búho se traban contra las piedras con el agente chico.
        // Los dos rodean por una esquina libre. El resto conserva el recorrido anterior.
        directionalSheets = GetComponent<RegeneratingRat>() != null;
        steerAroundRocks = directionalSheets || GetComponent<VanishingOwl>() != null;
        if (steerAroundRocks)
        {
            bodyRadius = MeasureBodyRadius();
            NavMeshAgent existing = GetComponent<NavMeshAgent>();
            if (existing != null)
                existing.enabled = false;
        }
        else
        {
            ConfigureAgent();
        }
    }

    private void ConfigureAgent()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
            agent = gameObject.AddComponent<NavMeshAgent>();

        agent.enabled = false;
        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.autoBraking = false;
        agent.autoRepath = true;
        agent.angularSpeed = 0f;
        agent.acceleration = 80f;
        agent.stoppingDistance = 0.05f;
        agent.baseOffset = 0f;
        agent.height = 1f;
        agent.radius = 0.02f;
    }

    private float MeasureBodyRadius()
    {
        float radius = 0.28f;
        Collider2D[] cols = GetComponents<Collider2D>();
        for (int i = 0; i < cols.Length; i++)
        {
            Collider2D col = cols[i];
            if (col == null || col.isTrigger)
                continue;

            radius = Mathf.Max(radius, col.bounds.extents.x, col.bounds.extents.y);
        }

        return Mathf.Clamp(radius, 0.2f, 0.55f);
    }

    private void Start()
    {
        // Despues de EnemyBodyCollision.Awake: el cuerpo no puede seguir siendo dinamico
        // si el recorrido lo marca el NavMesh. Un dinamico y el avoidance se pelean la posicion.
        if (rb != null && GetComponent<BossBase>() == null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    public void Move(Vector2 direction)
    {
        Move(direction, speed, true);
    }

    public void Move(Vector2 direction, float speedOverride)
    {
        Move(direction, speedOverride, true);
    }

    public void Move(Vector2 direction, float speedOverride, bool avoidObstacles)
    {
        // Mientras esta aturdido no persigue. El golpe, si lo hay, lo mueve BeginKnockback por posicion.
        if (hitReaction != null && hitReaction.IsStunned)
        {
            pendingDirection = Vector2.zero;
            Velocity = Vector2.zero;
            moveStamp = -1;
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
            if (animator != null)
                animator.SetFloat("AnimSpeed", 0f);
            return;
        }

        if (IsSliding)
        {
            if (animator != null)
                animator.SetFloat("AnimSpeed", 0f);
            return;
        }

        Vector2 steered = direction;
        bool navReady = DungeonNavMesh.Instance != null && DungeonNavMesh.Instance.IsReady;
        Vector2 from = rb != null ? rb.position : (Vector2)transform.position;
        pendingGoalValid = false;

        if (steerAroundRocks && avoidObstacles && navReady && direction.sqrMagnitude > 0.0001f)
        {
            Vector2 goal = hasExplicitGoal
                ? explicitGoal
                : from + direction.normalized * 4f;

            if (DungeonNavMesh.TryGetSteerTarget(from, goal, out Vector2 steerPoint))
            {
                Vector2 toSteer = steerPoint - from;
                if (toSteer.sqrMagnitude > 0.0004f)
                    steered = toSteer.normalized * Mathf.Max(direction.magnitude, 0.01f);

                pendingGoal = steerPoint;
                pendingGoalValid = true;
            }
            else
            {
                steered = ApplyPathfinding(direction);
            }
        }
        else if (avoidObstacles && navReady && TryAgentDirection(direction, speedOverride, out Vector2 agentDirection))
        {
            steered = agentDirection;
        }
        else if (avoidObstacles)
        {
            steered = ApplyPathfinding(direction);
        }

        if (!navReady)
        {
            steered = SmoothDirection(steered);
            if (avoidObstacles && IsImmediatelyBlocked(steered))
                steered = ApplyObstacleAvoidance(steered);
        }

        pendingDirection = steered;
        pendingSpeed = speedOverride;
        reportWalls = !avoidObstacles;
        moveStamp = Time.frameCount;
        Velocity = steered.sqrMagnitude > 0.0001f ? steered.normalized * speedOverride : Vector2.zero;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        UpdateAnimator(steered);

        if (avoidObstacles)
            UpdateStuckTracking(direction);
    }

    /// <summary>
    /// Golpe: un desplazamiento en el mundo, recortado a la malla. No se escribe linearVelocity.
    /// </summary>
    public void BeginKnockback(Vector2 worldVelocity, float duration)
    {
        if (worldVelocity.sqrMagnitude < 0.0001f || duration <= 0f)
            return;

        slideVelocity = worldVelocity;
        slideDuration = duration;
        slideTimer = duration;
        pendingDirection = Vector2.zero;
        Velocity = Vector2.zero;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.ResetPath();
    }

    /// <summary>Separacion entre enemigos, sumada al paso de este fixed frame (no es una fuerza del Rigidbody).</summary>
    public void AddSeparation(Vector2 offset)
    {
        separation += offset;
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        if (slideTimer > 0f)
        {
            ApplySlide();
            return;
        }

        if (Time.frameCount - moveStamp > 1)
        {
            pendingDirection = Vector2.zero;
            Velocity = Vector2.zero;
        }

        ApplyPlanarStep(pendingDirection, pendingSpeed, reportWalls);
    }

    private void ApplySlide()
    {
        float duration = Mathf.Max(0.05f, slideDuration);
        slideTimer -= Time.fixedDeltaTime;

        float t = 1f - Mathf.Clamp01(slideTimer / duration);
        float strength = (1f - t) * (1f - t);
        Vector2 delta = slideVelocity * strength * Time.fixedDeltaTime;

        Vector2 from = rb.position;
        Vector2 desired = from + delta;
        Vector2 next = desired;
        bool blocked = false;

        if (DungeonNavMesh.ClampStep(from, desired, out Vector2 clamped, out _, out blocked))
            next = clamped;

        if (blocked)
            slideTimer = 0f;

        // La malla es mas fina que el cuerpo. El golpe puede dejar el centro adentro de la piedra.
        if (steerAroundRocks)
        {
            Vector2 free = ExtrudeFromRocks(next);
            if ((free - next).sqrMagnitude > 0.0001f)
                slideTimer = 0f;
            next = free;
        }

        rb.linearVelocity = Vector2.zero;
        rb.MovePosition(next);
        Velocity = (next - from) / Mathf.Max(0.0001f, Time.fixedDeltaTime);
        SyncAgent(next);
        separation = Vector2.zero;
    }

    private void ApplyPlanarStep(Vector2 steered, float speedOverride, bool reportWallHits)
    {
        rb.linearVelocity = Vector2.zero;

        Vector2 from = rb.position;
        if (steerAroundRocks)
        {
            Vector2 freed = ExtrudeFromRocks(from);
            if ((freed - from).sqrMagnitude > 0.0001f)
            {
                separation = Vector2.zero;
                rb.MovePosition(freed);
                Velocity = Vector2.zero;
                wasBlocked = true;
                return;
            }
        }

        Vector2 extra = separation * 0.35f;
        separation = Vector2.zero;
        if (extra.magnitude > 0.12f)
            extra = extra.normalized * 0.12f;

        if (steered.sqrMagnitude < 0.0001f || speedOverride <= 0f)
        {
            if (extra.sqrMagnitude < 0.0001f)
            {
                wasBlocked = false;
                return;
            }

            steered = extra.normalized;
            speedOverride = extra.magnitude / Mathf.Max(0.0001f, Time.fixedDeltaTime);
            extra = Vector2.zero;
        }

        Vector2 desired = from + steered.normalized * speedOverride * Time.fixedDeltaTime + extra;
        Vector2 next = desired;
        Vector2 normal = Vector2.zero;
        bool blocked = false;
        bool handled = false;

        if (DungeonNavMesh.Instance != null && DungeonNavMesh.Instance.IsReady)
            handled = DungeonNavMesh.ClampStep(from, desired, out next, out normal, out blocked);

        if (!handled)
            blocked = PhysicsClamp(from, desired, out next, out normal);

        if (steerAroundRocks && !reportWallHits)
            next = KeepBodyOffObstacles(from, next, speedOverride);

        if (steerAroundRocks && BodyInsideRock(next))
            next = from;

        rb.MovePosition(next);
        Velocity = (next - from) / Mathf.Max(0.0001f, Time.fixedDeltaTime);
        SyncAgent(next);

        if (reportWallHits && blocked && !wasBlocked && normal.sqrMagnitude > 0.0001f)
        {
            EnemyBehaviour behaviour = GetComponent<EnemyBehaviour>();
            if (behaviour != null)
                behaviour.OnWallHit(normal);
        }

        wasBlocked = blocked;
    }

    private bool BodyInsideRock(Vector2 position)
    {
        return TryGetOverlappedRock(position, bodyRadius * 0.72f, out _);
    }

    /// <summary>
    /// Saca el centro de la piedra a un punto libre de la malla. ClosestPoint no sirve
    /// adentro del collider: devuelve el mismo punto y el cuerpo se queda clavado.
    /// </summary>
    private Vector2 ExtrudeFromRocks(Vector2 position)
    {
        if (!steerAroundRocks || obstacleLayers.value == 0)
            return position;

        for (int pass = 0; pass < 3; pass++)
        {
            if (!TryGetOverlappedRock(position, bodyRadius * 0.72f, out Collider2D rock))
                return position;

            Vector2 away = position - (Vector2)rock.bounds.center;
            if (away.sqrMagnitude < 0.0001f)
                away = Vector2.up;
            away.Normalize();
            Vector2 side = new Vector2(-away.y, away.x);

            Vector2[] dirs =
            {
                away,
                (away + side).normalized,
                (away - side).normalized,
                side,
                -side
            };

            bool freed = false;
            for (int d = 0; d < dirs.Length && !freed; d++)
            {
                for (float distance = 0.08f; distance <= bodyRadius * 3.2f; distance += 0.08f)
                {
                    Vector2 candidate = position + dirs[d] * distance;
                    if (TryGetOverlappedRock(candidate, bodyRadius * 0.55f, out _))
                        continue;

                    if (DungeonNavMesh.Instance != null && DungeonNavMesh.Instance.IsReady)
                    {
                        if (!DungeonNavMesh.SampleWalkable(candidate, out Vector2 onMesh, 0.3f))
                            continue;
                        if (TryGetOverlappedRock(onMesh, bodyRadius * 0.55f, out _))
                            continue;
                        candidate = onMesh;
                    }

                    position = candidate;
                    freed = true;
                    break;
                }
            }

            if (!freed)
                return position + away * (bodyRadius + 0.05f);
        }

        return position;
    }

    private bool TryGetOverlappedRock(Vector2 position, float radius, out Collider2D rock)
    {
        rock = null;
        if (obstacleLayers.value == 0)
            return false;

        int count = Physics2D.OverlapCircle(position, radius, obstacleFilter, overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            Collider2D col = overlapBuffer[i];
            if (col == null || col.isTrigger)
                continue;
            if (col.transform == transform || col.transform.IsChildOf(transform))
                continue;

            rock = col;
            return true;
        }

        return false;
    }

    /// <summary>
    /// La malla esta bakeada con un radio diminuto, y la rata es mucho mas ancha.
    /// El camino puede pasar pegado a la piedra: este paso empuja el cuerpo afuera
    /// y, si el frente esta ocupado, lo desliza por el costado hacia la esquina.
    /// </summary>
    private Vector2 KeepBodyOffObstacles(Vector2 from, Vector2 next, float speedOverride)
    {
        if (obstacleLayers.value == 0)
            return next;

        float travel = Mathf.Max(0.04f, speedOverride * Time.fixedDeltaTime);
        float probe = bodyRadius * 0.92f;

        // Solo si el centro ya esta adentro de la piedra. Si apenas la roza, se desliza por el costado.
        if (TryGetOverlappedRock(from, bodyRadius * 0.35f, out Collider2D rock))
        {
            Vector2 away = from - (Vector2)rock.ClosestPoint(from);
            if (away.sqrMagnitude < 0.0001f)
                away = from - (Vector2)rock.bounds.center;
            if (away.sqrMagnitude < 0.0001f)
                away = Vector2.up;

            return from + away.normalized * travel;
        }

        Vector2 step = next - from;
        float distance = step.magnitude;
        if (distance < 0.0001f)
            return next;

        Vector2 dir = step / distance;
        int hits = Physics2D.CircleCast(from, probe, dir, obstacleFilter, AvoidanceHitBuffer, distance + 0.04f);
        if (hits <= 0)
            return next;

        Vector2 hitNormal = AvoidanceHitBuffer[0].normal;
        if (hitNormal.sqrMagnitude < 0.0001f)
            hitNormal = -dir;

        Vector2 tangent = new Vector2(-hitNormal.y, hitNormal.x);
        Vector2 prefer = pendingGoalValid ? pendingGoal - from : dir;
        if (Vector2.Dot(tangent, prefer) < 0f)
            tangent = -tangent;
        if (wallSlideSign < 0)
            tangent = -tangent;

        tangent.Normalize();
        if (SlideIsClear(from, tangent, travel, probe))
            return from + tangent * travel;

        tangent = -tangent;
        wallSlideSign = -wallSlideSign;
        if (SlideIsClear(from, tangent, travel, probe))
            return from + tangent * travel;

        return from;
    }

    private bool SlideIsClear(Vector2 from, Vector2 dir, float distance, float probe)
    {
        int hits = Physics2D.CircleCast(from, probe * 0.92f, dir, obstacleFilter, AvoidanceHitBuffer, distance);
        return hits <= 0;
    }

    private bool TryAgentDirection(Vector2 desiredDirection, float speedOverride, out Vector2 steered)
    {
        steered = desiredDirection;
        float magnitude = desiredDirection.magnitude;
        if (magnitude < 0.0001f || !EnsureAgentOnMesh())
            return false;

        Vector2 from = rb.position;
        Vector2 goal = hasExplicitGoal
            ? explicitGoal
            : from + (desiredDirection / magnitude) * 2.4f;

        if (!DungeonNavMesh.TryProject(goal, out Vector3 goalOnMesh))
            return false;

        agent.speed = Mathf.Max(0.1f, speedOverride);
        agent.SetDestination(goalOnMesh);

        Vector3 steer = agent.desiredVelocity;
        if (steer.sqrMagnitude < 0.04f && agent.hasPath)
            steer = agent.steeringTarget - agent.nextPosition;

        Vector2 planar = new Vector2(steer.x, steer.y);
        if (planar.sqrMagnitude < 0.0001f)
            return false;

        steered = planar.normalized * magnitude;
        return true;
    }

    private bool EnsureAgentOnMesh()
    {
        if (agent == null || rb == null)
            return false;

        if (DungeonNavMesh.Instance == null || !DungeonNavMesh.Instance.IsReady)
        {
            if (agent.enabled)
                agent.enabled = false;
            return false;
        }

        if (!agent.enabled)
        {
            float baked = DungeonNavMesh.Instance != null ? DungeonNavMesh.Instance.AgentRadius : 0.02f;
            agent.radius = Mathf.Max(0.01f, baked * 0.9f);
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.updateUpAxis = false;
            agent.enabled = true;
        }

        if (!agent.isOnNavMesh)
        {
            if (!DungeonNavMesh.TryProject(rb.position, out Vector3 onMesh) || !agent.Warp(onMesh))
            {
                agent.enabled = false;
                return false;
            }
        }

        return agent.isOnNavMesh;
    }

    private void SyncAgent(Vector2 planar)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        if (DungeonNavMesh.TryProject(planar, out Vector3 onMesh))
            agent.nextPosition = onMesh;
    }

    private bool PhysicsClamp(Vector2 from, Vector2 to, out Vector2 clamped, out Vector2 normal)
    {
        clamped = to;
        normal = Vector2.zero;

        Vector2 delta = to - from;
        float distance = delta.magnitude;
        if (distance < 0.0001f || obstacleLayers.value == 0)
            return false;

        Vector2 dir = delta / distance;
        int hitCount = Physics2D.CircleCast(from, avoidanceRadius, dir, obstacleFilter, AvoidanceHitBuffer, distance);
        if (hitCount <= 0)
            return false;

        float allowed = Mathf.Max(0f, AvoidanceHitBuffer[0].distance - 0.02f);
        clamped = from + dir * allowed;
        normal = AvoidanceHitBuffer[0].normal;
        return true;
    }

    public void MoveTowards(Vector2 worldTarget)
    {
        MoveTowards(worldTarget, speed);
    }

    public void MoveTowards(Vector2 worldTarget, float speedOverride)
    {
        Vector2 from = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 toTarget = worldTarget - from;
        if (toTarget.sqrMagnitude < 0.0001f)
        {
            Move(Vector2.zero, speedOverride, true);
            return;
        }

        if (toTarget.magnitude <= 0.45f && DungeonNavMesh.HasClearLine(from, worldTarget))
        {
            Move(toTarget.normalized, speedOverride, false);
            return;
        }

        hasExplicitGoal = true;
        explicitGoal = worldTarget;
        Move(toTarget.normalized, speedOverride, true);
        hasExplicitGoal = false;
    }

    public void Face(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) return;

        Vector2 facing = directionalSheets ? Cardinal(direction) : direction.normalized;
        lastFacingDirection = facing.normalized;

        ApplyFacingFlip(facing);

        if (animator != null)
        {
            animator.SetFloat("X", lastFacingDirection.x);
            animator.SetFloat("Y", lastFacingDirection.y);
        }
    }

    private void ApplyFacingFlip(Vector2 direction)
    {
        if (spriteRenderer == null || Mathf.Abs(direction.x) < 0.01f)
            return;

        // La rata tiene hoja propia para cada lado. Espejarla la deja mirando al revés.
        if (directionalSheets)
        {
            spriteRenderer.flipX = false;
            return;
        }

        spriteRenderer.flipX = direction.x > 0f;
    }

    private void UpdateStuckTracking(Vector2 desiredDirection)
    {
        Vector2 currentPos = rb != null ? rb.position : (Vector2)transform.position;

        stuckWindowTimer += Time.deltaTime;

        if (stuckWindowTimer < stuckTimeWindow)
            return;

        bool wasTryingToMove = desiredDirection.magnitude > 0.1f;
        float traveled = Vector2.Distance(currentPos, stuckWindowStartPos);

        if (wasTryingToMove && traveled < stuckDistanceThreshold)
        {
            escapeTimer = escapeDuration;
            escapeAngleJitter = UnityEngine.Random.Range(60f, 150f) * (UnityEngine.Random.value < 0.5f ? 1f : -1f);
            if (steerAroundRocks)
                wallSlideSign = -wallSlideSign;

            Vector2 side = RotateVector(desiredDirection.sqrMagnitude > 0.01f
                ? desiredDirection.normalized
                : lastFacingDirection, escapeAngleJitter);
            Vector2 probe = currentPos + side.normalized * 1.1f;
            if (DungeonNavMesh.SampleWalkable(probe, out Vector2 walkable, 1.4f))
            {
                cachedPathStep = walkable;
                lastPathGoal = walkable;
            }
        }

        stuckWindowStartPos = currentPos;
        stuckWindowTimer = 0f;
    }

    private Vector2 ApplyPathfinding(Vector2 desiredDirection)
    {
        float magnitude = desiredDirection.magnitude;
        if (magnitude < 0.0001f)
            return desiredDirection;

        Vector2 from = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 goal = hasExplicitGoal
            ? explicitGoal
            : from + (desiredDirection / magnitude) * 2.4f;

        if (DungeonNavMesh.TryGetNextCorner(from, goal, out Vector2 navStep))
        {
            cachedPathStep = navStep;
            lastPathGoal = goal;
            Vector2 toNav = navStep - from;
            if (toNav.sqrMagnitude >= 0.04f)
                return toNav.normalized * magnitude;

            return desiredDirection;
        }

        RoomPathGrid grid = RoomPathGrid.For(transform);
        if (grid == null)
            return desiredDirection;

        if (grid.HasClearLine(from, goal, avoidanceRadius * 0.85f))
        {
            cachedPathStep = goal;
            lastPathGoal = goal;
            return desiredDirection;
        }

        if (Time.time >= nextRepathTime || (goal - lastPathGoal).sqrMagnitude > 0.85f)
        {
            if (grid.TryGetLookAhead(from, goal, 1.15f, out Vector2 step))
                cachedPathStep = step;
            else
                cachedPathStep = goal;

            lastPathGoal = goal;
            nextRepathTime = Time.time + 0.18f;
        }

        Vector2 toStep = cachedPathStep - from;
        if (toStep.sqrMagnitude < 0.04f)
            return desiredDirection;

        return toStep.normalized * magnitude;
    }

    private Vector2 SmoothDirection(Vector2 desiredDirection)
    {
        if (desiredDirection.sqrMagnitude < 0.0001f)
        {
            hasSmoothedDirection = false;
            return desiredDirection;
        }

        Vector2 target = desiredDirection.normalized;
        if (!hasSmoothedDirection || smoothedDirection.sqrMagnitude < 0.0001f)
        {
            smoothedDirection = target;
            hasSmoothedDirection = true;
            return desiredDirection;
        }

        smoothedDirection = Vector3.Slerp(smoothedDirection, target, 1f - Mathf.Exp(-9f * Time.deltaTime));
        if (smoothedDirection.sqrMagnitude < 0.0001f)
            smoothedDirection = target;
        else
            smoothedDirection.Normalize();

        return smoothedDirection * desiredDirection.magnitude;
    }

    private bool IsImmediatelyBlocked(Vector2 desiredDirection)
    {
        if (desiredDirection.sqrMagnitude < 0.0001f || obstacleLayers.value == 0)
            return false;

        Vector2 origin = rb != null ? rb.position : (Vector2)transform.position;
        int hitCount = Physics2D.CircleCast(
            origin,
            avoidanceRadius,
            desiredDirection.normalized,
            obstacleFilter,
            AvoidanceHitBuffer,
            0.38f);

        return hitCount > 0;
    }

    private Vector2 ApplyObstacleAvoidance(Vector2 desiredDirection)
    {
        float magnitude = desiredDirection.magnitude;
        if (magnitude < 0.0001f || obstacleLayers.value == 0)
            return desiredDirection;

        Vector2 baseDirection = desiredDirection / magnitude;

        if (escapeTimer > 0f)
        {
            escapeTimer -= Time.deltaTime;
            baseDirection = RotateVector(baseDirection, escapeAngleJitter);
        }

        Vector2 origin = rb != null ? rb.position : (Vector2)transform.position;

        Vector2 bestDirection = Vector2.zero;
        float bestClearance = -1f;

        foreach (float angle in AvoidanceProbeAngles)
        {
            Vector2 probeDirection = RotateVector(baseDirection, angle);

            int hitCount = Physics2D.CircleCast(origin, avoidanceRadius, probeDirection, obstacleFilter, AvoidanceHitBuffer, avoidanceLookahead);
            if (hitCount == 0)
                return probeDirection * magnitude;

            float clearance = AvoidanceHitBuffer[0].distance;
            if (clearance > bestClearance)
            {
                bestClearance = clearance;
                bestDirection = probeDirection;
            }
        }

        if (bestDirection.sqrMagnitude > 0.0001f)
            return bestDirection * magnitude * 0.75f;

        return desiredDirection * 0.35f;
    }

    private static Vector2 RotateVector(Vector2 vector, float degrees)
    {
        if (degrees == 0f) return vector;

        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }

    private void UpdateAnimator(Vector2 direction)
    {
        if (animator == null) return;

        float currentSpeed = direction.magnitude;

        float animationPlaybackSpeed = currentSpeed > 0.01f ? 1f : 0f;
        animator.SetFloat("AnimSpeed", animationPlaybackSpeed);

        if (direction.sqrMagnitude > 0.0001f)
        {
            Vector2 facing = direction;
            // El blend 2D mezcla las cuatro caminatas si el vector es diagonal, y el cuerpo
            // parece no girar hasta que el eje es casi puro. En la rata se elige un solo eje.
            if (directionalSheets)
                facing = Cardinal(direction);

            lastFacingDirection = facing.normalized;
            ApplyFacingFlip(facing);
        }

        animator.SetFloat("X", lastFacingDirection.x);
        animator.SetFloat("Y", lastFacingDirection.y);
    }

    private static Vector2 Cardinal(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            return direction.x >= 0f ? Vector2.right : Vector2.left;

        return direction.y >= 0f ? Vector2.up : Vector2.down;
    }
}