using UnityEngine;

/// <summary>
/// Guardia del challenge "Steal the Item".
///
/// Patrulla entre puntos fijos con un cono de visión. Si ve al jugador el tiempo
/// suficiente pasa a perseguirlo, y si lo alcanza avisa al controller (el jugador
/// pierde un corazón y es expulsado).
///
/// NO usa EnemyHealth a propósito: estos guardias son invulnerables, así la room
/// nunca entra en estado de combate y el desafío es puramente de sigilo.
/// </summary>
[DisallowMultipleComponent]
public class StealChallengeGuard : MonoBehaviour
{
    private enum GuardState { Idle, Patrol, Suspicious, Chase, Returning }

    [Header("Patrulla")]
    [Tooltip("Puntos de patrulla. Ponelos como hijos de la ROOM, no del guardia.")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float waitAtPoint = 0.75f;
    [SerializeField] private float arriveDistance = 0.12f;
    [Tooltip("True: recorre ida y vuelta. False: loop cerrado.")]
    [SerializeField] private bool pingPong = true;

    [Header("Visión")]
    [SerializeField] private float viewRadius = 4.5f;
    [SerializeField, Range(10f, 360f)] private float viewAngle = 90f;
    [Tooltip("Layers que cortan la línea de visión (paredes, props altos).")]
    [SerializeField] private LayerMask obstacleMask;
    [Tooltip("Segundos que el jugador tiene que estar dentro del cono para que el guardia se alerte.")]
    [SerializeField] private float timeToDetect = 0.35f;

    [Header("Persecución")]
    [SerializeField] private float chaseSpeed = 3.1f;
    [Tooltip("Distancia a la que el guardia conecta el golpe y el jugador pierde el challenge.")]
    [SerializeField] private float catchDistance = 0.6f;
    [SerializeField] private float timeToLoseTarget = 2.5f;

    [Header("Feedback (opcional)")]
    [SerializeField] private GameObject suspiciousIcon;
    [SerializeField] private GameObject alertIcon;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool flipSpriteWithMovement = true;
    [Tooltip("Opcional: Animator con parámetros AnimSpeed / X / Y como el resto de tus enemigos.")]
    [SerializeField] private Animator animator;

    private StealTheItemChallenge controller;
    private Transform player;
    private GuardState state = GuardState.Idle;
    private Vector2 facing = Vector2.down;
    private Vector3 homePosition;
    private int currentPoint;
    private int step = 1;
    private float waitTimer;
    private float detectionTimer;
    private float lostTimer;
    private bool active;

    public bool IsAlerted => state == GuardState.Chase;
    public float DetectionProgress => timeToDetect <= 0f ? 1f : Mathf.Clamp01(detectionTimer / timeToDetect);

    private void Awake()
    {
        homePosition = transform.position;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        ShowIcons(false, false);
    }

    public void Prepare(StealTheItemChallenge owner)
    {
        controller = owner;

        transform.position = homePosition;
        currentPoint = 0;
        step = 1;
        waitTimer = 0f;
        detectionTimer = 0f;
        lostTimer = 0f;

        CachePlayer();
        ShowIcons(false, false);

        state = HasPatrolPoints() ? GuardState.Patrol : GuardState.Idle;
        active = true;
    }

    public void Deactivate()
    {
        active = false;
        state = GuardState.Idle;
        ShowIcons(false, false);
        UpdateAnimator(Vector2.zero);
    }

    /// <summary>Lo usan las trampas tipo alarma: el guardia pasa directo a perseguir.</summary>
    public void ForceAlert(Vector3 origin)
    {
        if (!active)
            return;

        CachePlayer();
        detectionTimer = timeToDetect;
        lostTimer = 0f;
        state = GuardState.Chase;
        ShowIcons(false, true);
    }

    private void Update()
    {
        if (!active)
            return;

        if (GamePause.IsGameplayFrozen)
            return;

        float dt = Time.deltaTime;

        switch (state)
        {
            case GuardState.Patrol:
                TickPatrol(dt);
                break;

            case GuardState.Suspicious:
                TickSuspicious(dt);
                break;

            case GuardState.Chase:
                TickChase(dt);
                break;

            case GuardState.Returning:
                TickReturning(dt);
                break;

            default:
                TickIdle(dt);
                break;
        }
    }

    private void TickIdle(float dt)
    {
        UpdateAnimator(Vector2.zero);

        if (SeesPlayer())
            EnterSuspicious();
    }

    private void TickPatrol(float dt)
    {
        if (SeesPlayer())
        {
            EnterSuspicious();
            return;
        }

        if (!HasPatrolPoints())
        {
            state = GuardState.Idle;
            return;
        }

        if (waitTimer > 0f)
        {
            waitTimer -= dt;
            UpdateAnimator(Vector2.zero);
            return;
        }

        Transform target = patrolPoints[currentPoint];

        if (target == null)
        {
            AdvancePoint();
            return;
        }

        if (MoveTowards(target.position, patrolSpeed, dt))
        {
            waitTimer = waitAtPoint;
            AdvancePoint();
        }
    }

    private void TickSuspicious(float dt)
    {
        if (!SeesPlayer())
        {
            detectionTimer -= dt * 1.5f;

            if (detectionTimer <= 0f)
            {
                detectionTimer = 0f;
                ShowIcons(false, false);
                state = GuardState.Returning;
            }

            UpdateAnimator(Vector2.zero);
            return;
        }

        FaceTowards(player.position);
        UpdateAnimator(Vector2.zero);

        detectionTimer += dt;

        if (detectionTimer >= timeToDetect)
        {
            lostTimer = 0f;
            ShowIcons(false, true);
            state = GuardState.Chase;
        }
    }

    private void TickChase(float dt)
    {
        if (player == null)
        {
            state = GuardState.Returning;
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= catchDistance)
        {
            active = false;
            ShowIcons(false, false);
            UpdateAnimator(Vector2.zero);

            if (controller != null)
                controller.NotifyPlayerHit(player.gameObject);

            return;
        }

        MoveTowards(player.position, chaseSpeed, dt);

        if (SeesPlayer())
        {
            lostTimer = 0f;
            return;
        }

        lostTimer += dt;

        if (lostTimer >= timeToLoseTarget)
        {
            detectionTimer = 0f;
            ShowIcons(false, false);
            state = GuardState.Returning;
        }
    }

    private void TickReturning(float dt)
    {
        if (SeesPlayer())
        {
            EnterSuspicious();
            return;
        }

        Vector3 target = HasPatrolPoints() && patrolPoints[currentPoint] != null
            ? patrolPoints[currentPoint].position
            : homePosition;

        if (MoveTowards(target, patrolSpeed, dt))
        {
            waitTimer = waitAtPoint;
            state = HasPatrolPoints() ? GuardState.Patrol : GuardState.Idle;
        }
    }

    private void EnterSuspicious()
    {
        if (state == GuardState.Suspicious || state == GuardState.Chase)
            return;

        state = GuardState.Suspicious;
        ShowIcons(true, false);
    }

    private bool HasPatrolPoints()
    {
        return patrolPoints != null && patrolPoints.Length > 0;
    }

    private void AdvancePoint()
    {
        if (!HasPatrolPoints())
            return;

        if (pingPong)
        {
            if (patrolPoints.Length == 1)
                return;

            currentPoint += step;

            if (currentPoint >= patrolPoints.Length)
            {
                currentPoint = patrolPoints.Length - 2;
                step = -1;
            }
            else if (currentPoint < 0)
            {
                currentPoint = 1;
                step = 1;
            }

            return;
        }

        currentPoint = (currentPoint + 1) % patrolPoints.Length;
    }

    private bool MoveTowards(Vector3 target, float speed, float dt)
    {
        Vector3 flatTarget = new Vector3(target.x, target.y, transform.position.z);
        Vector3 delta = flatTarget - transform.position;

        if (delta.sqrMagnitude > 0.0001f)
            facing = ((Vector2)delta).normalized;

        transform.position = Vector3.MoveTowards(transform.position, flatTarget, speed * dt);

        ApplyFlip();
        UpdateAnimator(facing * speed);

        return (flatTarget - transform.position).sqrMagnitude <= arriveDistance * arriveDistance;
    }

    private void FaceTowards(Vector3 target)
    {
        Vector2 delta = target - transform.position;

        if (delta.sqrMagnitude > 0.0001f)
            facing = delta.normalized;

        ApplyFlip();
    }

    private bool SeesPlayer()
    {
        CachePlayer();

        if (player == null)
            return false;

        Vector2 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        if (distance > viewRadius)
            return false;

        if (viewAngle < 360f && Vector2.Angle(facing, toPlayer) > viewAngle * 0.5f)
            return false;

        if (obstacleMask.value != 0 && distance > 0.01f)
        {
            RaycastHit2D blocked = Physics2D.Raycast(transform.position, toPlayer / distance, distance, obstacleMask);
            if (blocked.collider != null)
                return false;
        }

        return true;
    }

    private void CachePlayer()
    {
        if (player != null)
            return;

        GameObject found = GameObject.FindGameObjectWithTag("Player");
        if (found != null)
            player = found.transform;
    }

    private void ApplyFlip()
    {
        if (!flipSpriteWithMovement || spriteRenderer == null)
            return;

        if (Mathf.Abs(facing.x) > 0.05f)
            spriteRenderer.flipX = facing.x < 0f;
    }

    private void UpdateAnimator(Vector2 velocity)
    {
        if (animator == null)
            return;

        animator.SetFloat("AnimSpeed", velocity.magnitude);

        if (velocity.sqrMagnitude > 0.0001f)
        {
            animator.SetFloat("X", velocity.normalized.x);
            animator.SetFloat("Y", velocity.normalized.y);
        }
    }

    private void ShowIcons(bool suspicious, bool alerted)
    {
        if (suspiciousIcon != null)
            suspiciousIcon.SetActive(suspicious);

        if (alertIcon != null)
            alertIcon.SetActive(alerted);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position;
        Vector2 dir = Application.isPlaying ? facing : Vector2.down;

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.85f);

        float half = viewAngle * 0.5f;
        Vector3 left = Quaternion.Euler(0f, 0f, half) * dir * viewRadius;
        Vector3 right = Quaternion.Euler(0f, 0f, -half) * dir * viewRadius;

        Gizmos.DrawLine(origin, origin + left);
        Gizmos.DrawLine(origin, origin + right);

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.25f);
        Gizmos.DrawWireSphere(origin, viewRadius);

        if (patrolPoints == null)
            return;

        Gizmos.color = Color.cyan;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null)
                continue;

            Gizmos.DrawWireSphere(patrolPoints[i].position, 0.15f);

            int next = i + 1;
            if (next < patrolPoints.Length && patrolPoints[next] != null)
                Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[next].position);
        }
    }
}

