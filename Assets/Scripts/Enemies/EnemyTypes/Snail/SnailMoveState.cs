using UnityEngine;

// CAMBIADO (archivo completo)
// Antes: elegía una dirección al azar (cualquier ángulo) y la seguía 2-4 segundos. Si llegaba a la pared antes,
// se quedaba empujando contra ella el resto del tiempo y se deslizaba por el borde: por eso vivía en los bordes.
// Ahora:
//  - Solo se mueve en X o en Y (arriba, abajo, izquierda, derecha).
//  - Camina "tramos" de una distancia, no de un tiempo, y nunca elige un tramo que lo lleve a la zona pegada a la pared.
//  - Si queda trabado (no avanza), cambia de dirección enseguida.
public class SnailMoveState : IEnemyState
{
    private const float TrailActiveDuration = 4f;
    private const float TrailPausedDuration = 2.5f;
    private const float TrailCycleDuration = TrailActiveDuration + TrailPausedDuration;

    // NUEVO: ajustes del movimiento
    private const float EdgeMargin = 1.2f;          // distancia a la pared que trata de no cruzar
    private const float MinLegDistance = 1f;        // tramo mínimo en una dirección
    private const float MaxLegDistance = 3f;        // tramo máximo en una dirección
    private const float MaxLegTime = 4f;            // por seguridad: nunca sigue la misma dirección más que esto
    private const float StuckCheckInterval = 0.25f; // cada cuánto revisa si quedó trabado
    private const float StuckMinMove = 0.03f;       // si en ese tiempo avanzó menos que esto, está trabado

    // NUEVO: las únicas 4 direcciones posibles
    private static readonly Vector2[] Cardinals = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
    private static readonly Vector2[] candidateBuffer = new Vector2[4];

    private IMovement movement;
    private Transform enemyTransform;
    // El caracol no hace daño por contacto: solo el rastro de baba.
    private EnemyAttack attack;
    private SlimeTrailSpawner slimeTrailSpawner;

    private Vector2 direction;
    private float legDistanceLeft;   // NUEVO: cuánto le falta recorrer en esta dirección
    private float legTimer;          // NUEVO: reemplaza a changeDirTimer
    private Vector2 lastPosition;    // NUEVO
    private Vector2 stuckCheckPosition; // NUEVO
    private float stuckTimer;        // NUEVO

    private float trailCycleTimer;
    private bool isTrailActive;

    public SnailMoveState(Transform player, IMovement movement, Transform transform, EnemyAttack attack)
    {
        this.movement = movement;
        this.enemyTransform = transform;
        this.attack = attack;
        this.slimeTrailSpawner = transform.GetComponent<SlimeTrailSpawner>();
    }

    public void Enter()
    {
        lastPosition = enemyTransform.position;   // NUEVO
        stuckCheckPosition = lastPosition;        // NUEVO
        stuckTimer = 0f;                          // NUEVO

        PickNewDirection(Vector2.zero);

        trailCycleTimer = 0f;
        SetTrailActive(true);
    }

    public void Exit()
    {
        movement.Move(Vector2.zero);
        SetTrailActive(true);
    }

    public void Tick()
    {
        MoveAlong(direction);

        // CAMBIADO: el tramo termina por distancia recorrida (o por tiempo, como respaldo)
        Vector2 position = enemyTransform.position;
        legDistanceLeft -= Vector2.Distance(position, lastPosition);
        lastPosition = position;

        legTimer -= Time.deltaTime;
        if (legDistanceLeft <= 0f || legTimer <= 0f)
            PickNewDirection(direction);

        CheckStuck(position); // NUEVO

        UpdateTrailCycle();
    }

    public void OnWallHit(Vector2 normal)
    {
        // CAMBIADO: antes usaba la normal tal cual (podía ser diagonal) y le sumaba un poco de ruido.
        // Ahora se aleja de la pared en X o en Y.
        Vector2 away = ToCardinal(normal);
        if (away == Vector2.zero)
            away = -direction;

        StartLeg(away, Mathf.Max(MinLegDistance, SpaceAlong(away, enemyTransform.position)));
        MoveAlong(direction);
    }

    // ---------------- NUEVO: elegir dirección ----------------

    /// <summary>
    /// Elige una de las 4 direcciones que tenga lugar para un tramo completo sin meterse en el borde.
    /// 'avoid' = la dirección que traía (así no repite la misma). Si ninguna tiene lugar
    /// (por ejemplo, está metido en una esquina), va hacia donde haya más espacio.
    /// </summary>
    private void PickNewDirection(Vector2 avoid)
    {
        Vector2 position = enemyTransform.position;

        int count = 0;
        Vector2 roomiest = Vector2.down;
        float roomiestSpace = float.NegativeInfinity;

        foreach (Vector2 dir in Cardinals)
        {
            float space = SpaceAlong(dir, position);

            if (space > roomiestSpace)
            {
                roomiestSpace = space;
                roomiest = dir;
            }

            if (dir != avoid && space >= MinLegDistance)
                candidateBuffer[count++] = dir;
        }

        if (count > 0)
        {
            Vector2 chosen = candidateBuffer[Random.Range(0, count)];
            float space = SpaceAlong(chosen, position);
            StartLeg(chosen, Random.Range(MinLegDistance, Mathf.Min(MaxLegDistance, space)));
        }
        else
        {
            // Sin lugar para un tramo normal: vuelve hacia el centro de la room
            StartLeg(roomiest, Mathf.Clamp(roomiestSpace, 0.5f, MaxLegDistance));
        }
    }

    private void StartLeg(Vector2 dir, float distance)
    {
        direction = dir;
        legDistanceLeft = distance;
        legTimer = MaxLegTime;
        stuckTimer = 0f;
        stuckCheckPosition = enemyTransform.position;
    }

    /// <summary>
    /// Cuánto puede avanzar en esa dirección antes de entrar en la zona pegada a la pared.
    /// Es negativo si ya está adentro de esa zona y esa dirección lo mete más.
    /// </summary>
    private float SpaceAlong(Vector2 dir, Vector2 position)
    {
        if (!TryGetSafeArea(out Rect safe))
            return MaxLegDistance; // sin datos de la room: cualquier dirección vale

        if (dir == Vector2.right) return safe.xMax - position.x;
        if (dir == Vector2.left) return position.x - safe.xMin;
        if (dir == Vector2.up) return safe.yMax - position.y;
        return position.y - safe.yMin;
    }

    /// <summary>Interior de la room (el mismo que usa el NavMesh), achicado por el margen de los bordes.</summary>
    private static bool TryGetSafeArea(out Rect safe)
    {
        safe = default;

        DungeonNavMesh mesh = DungeonNavMesh.Instance;
        if (mesh == null || mesh.CurrentRoom == null)
            return false;

        Bounds interior = mesh.InteriorBounds;
        if (interior.size.x < 1f || interior.size.y < 1f)
            return false;

        // En rooms chicas el margen se achica, así siempre queda lugar para moverse
        float marginX = Mathf.Min(EdgeMargin, interior.size.x * 0.25f);
        float marginY = Mathf.Min(EdgeMargin, interior.size.y * 0.25f);

        safe = Rect.MinMaxRect(
            interior.min.x + marginX,
            interior.min.y + marginY,
            interior.max.x - marginX,
            interior.max.y - marginY);
        return true;
    }

    // NUEVO: si no avanza (piedra, otro enemigo, borde del NavMesh), cambia de dirección
    private void CheckStuck(Vector2 position)
    {
        stuckTimer += Time.deltaTime;
        if (stuckTimer < StuckCheckInterval)
            return;

        bool stuck = Vector2.Distance(position, stuckCheckPosition) < StuckMinMove;
        stuckTimer = 0f;
        stuckCheckPosition = position;

        if (stuck)
            PickNewDirection(direction);
    }

    private static Vector2 ToCardinal(Vector2 v)
    {
        if (v.sqrMagnitude < 0.0001f)
            return Vector2.zero;

        if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
            return v.x > 0f ? Vector2.right : Vector2.left;

        return v.y > 0f ? Vector2.up : Vector2.down;
    }

    private void MoveAlong(Vector2 dir)
    {
        if (movement is EnemyMovement enemyMovement)
            enemyMovement.Move(dir, enemyMovement.Speed, false);
        else
            movement.Move(dir);
    }

    // ---------------- Baba (sin cambios) ----------------

    private void UpdateTrailCycle()
    {
        trailCycleTimer += Time.deltaTime;

        if (trailCycleTimer >= TrailCycleDuration)
        {
            trailCycleTimer -= TrailCycleDuration;
        }

        SetTrailActive(trailCycleTimer < TrailActiveDuration);
    }

    private void SetTrailActive(bool active)
    {
        if (isTrailActive == active) return;

        isTrailActive = active;

        if (slimeTrailSpawner != null)
        {
            slimeTrailSpawner.enabled = active;
        }
    }
}