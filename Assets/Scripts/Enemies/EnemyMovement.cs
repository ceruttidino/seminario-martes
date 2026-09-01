using System;
using UnityEngine;

public class EnemyMovement : MonoBehaviour, IMovement
{
    [SerializeField] private float speed = 3f;
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

    // Angulos de sondeo en abanico, alternando lados, para encontrar el desvio
    // mas chico posible respecto de la direccion deseada original. Cubre los
    // 360 grados (no solo +-100) para que, si el camino directo esta bloqueado,
    // siempre haya alguna direccion (aunque sea hacia atras) para no quedar
    // parado en seco contra rocas/paredes agrupadas.
    private static readonly float[] AvoidanceProbeAngles =
        {
            0f, 20f, -20f, 40f, -40f, 60f, -60f, 80f, -80f,
            100f, -100f, 120f, -120f, 140f, -140f, 160f, -160f, 180f
        };

    private static readonly RaycastHit2D[] AvoidanceHitBuffer = new RaycastHit2D[1];

    private ContactFilter2D obstacleFilter;
    private Vector2 lastFacingDirection = Vector2.down;

    // Deteccion de "trabado": si hay poco avance real pese a intentar moverse
    // (comun al quedar encajonado entre varias rocas juntas, donde el sondeo en
    // abanico encuentra una mejora minima cada frame pero nunca una salida real),
    // se fuerza durante un rato una direccion de escape distinta a la habitual.
    private Vector2 stuckWindowStartPos;
    private float stuckWindowTimer;
    private float escapeTimer;
    private float escapeAngleJitter;

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

        // useTriggers=false es clave: puertas y zonas de interaccion son triggers
        // en la capa Wall y NO deben tratarse como obstaculos a esquivar.
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

    // avoidObstacles=false preserva el comportamiento fisico "crudo" para casos
    // donde chocar/rebotar contra el entorno es parte intencional del ataque
    // (ej. la embestida del Turtle, que rebota con OnWallHit a proposito).
    public void Move(Vector2 direction, float speedOverride, bool avoidObstacles)
    {
        Vector2 finalDirection = avoidObstacles ? ApplyObstacleAvoidance(direction) : direction;

        rb.linearVelocity = finalDirection * speedOverride;

        UpdateAnimator(finalDirection);

        if (avoidObstacles)
        {
            UpdateStuckTracking(direction);
        }
    }

    // Orienta al enemigo (flip + parametros del Animator) sin moverlo.
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

    // Mide el avance real cada StuckTimeWindow segundos. Si el enemigo estaba
    // intentando moverse (direccion deseada no nula) pero se desplazo menos que
    // stuckDistanceThreshold en toda la ventana, arma un escape: durante
    // escapeDuration se rota la direccion base antes de buscar el angulo libre,
    // en vez de repetir siempre el mismo orden de sondeo que lo dejo trabado.
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

    // Desvia la direccion pedida por el estado del enemigo (perseguir, huir,
    // deambular, acercarse) para esquivar paredes/piedras/objetos solidos ANTES
    // de chocar, en vez de quedar empujando en vano contra el obstaculo. Esto NO
    // cambia a quien persigue o ataca cada enemigo, solo como se traduce esa
    // decision en movimiento fisico real.
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

        // Ningun angulo esta completamente libre (tipico entre rocas agrupadas o
        // rodeado por otros enemigos): en vez de frenar en seco y quedar trabado
        // en el lugar, avanza despacio hacia el angulo menos obstruido, siempre
        // que haya un minimo de margen real. Si esta realmente encajonado (casi
        // sin margen en ninguna direccion), ahi si se detiene.
        if (bestClearance > avoidanceRadius * 0.5f)
        {
            return bestDirection * magnitude * 0.5f;
        }

        return Vector2.zero;
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

            // Flip the sprite based on horizontal direction
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
