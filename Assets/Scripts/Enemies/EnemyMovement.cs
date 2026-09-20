using System;
using UnityEngine;

public class EnemyMovement : MonoBehaviour, IMovement
{
    [SerializeField] private float speed = 3f;
    public float Speed => speed;
    private Rigidbody2D rb;

    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Obstacle Avoidance")]
    [Tooltip("Capas solidas (paredes, piedras, bolsas de basura, etc.) que el enemigo esquiva en vez de empujar contra ellas. Los triggers (puertas, zonas de interaccion) se ignoran siempre, sin importar la capa.")]
    [SerializeField] private LayerMask obstacleLayers = 1088; // Wall (1024) + Trash (64)
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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (spriteRenderer  == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Las puertas son triggers en capa Wall: si las contamos, los enemigos las "esquivan" y no cruzan.
        obstacleFilter = new ContactFilter2D();
        obstacleFilter.useTriggers = false;
        obstacleFilter.SetLayerMask(obstacleLayers);

        stuckWindowStartPos = rb != null ? rb.position : (Vector2)transform.position;
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
        Vector2 steered = direction;
        if (avoidObstacles)
            steered = ApplyPathfinding(direction);

        steered = SmoothDirection(steered);

        if (avoidObstacles && IsImmediatelyBlocked(steered))
            steered = ApplyObstacleAvoidance(steered);

        rb.linearVelocity = steered * speedOverride;

        UpdateAnimator(steered);

        if (avoidObstacles)
            UpdateStuckTracking(direction);
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

        hasExplicitGoal = true;
        explicitGoal = worldTarget;
        Move(toTarget.normalized, speedOverride, true);
        hasExplicitGoal = false;
    }

    public void Face(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) return;

        lastFacingDirection = direction.normalized;

        if (spriteRenderer != null)
        {
            if (direction.x > 0) spriteRenderer.flipX = true;
            else if (direction.x < 0) spriteRenderer.flipX = false;
        }

        if (animator != null)
        {
            animator.SetFloat("X", lastFacingDirection.x);
            animator.SetFloat("Y", lastFacingDirection.y);
        }
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

        if (direction != Vector2.zero)
        {
            lastFacingDirection = direction.normalized;

            if (spriteRenderer != null)
            {
                if (direction.x > 0) spriteRenderer.flipX = true;
                else if (direction.x < 0) spriteRenderer.flipX = false;
            }
        }

        animator.SetFloat("X", lastFacingDirection.x);
        animator.SetFloat("Y", lastFacingDirection.y);
    }
}
