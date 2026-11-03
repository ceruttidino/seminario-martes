using UnityEngine;

/// <summary>
/// Serpiente Venenosa (GDD 1.15, rework completo): enemigo a distancia que escupe veneno.
///
/// - Deambula por la room "acechando" al jugador sin perseguirlo (SnakeRoamState).
/// - Si lo ve a 10 tiles o menos (linea de vision libre) apunta y dispara (SnakeAttackState). Una vez que empieza a
///   atacar sigue disparando hasta que un disparo le pega al jugador.
/// - Si el jugador se le acerca a menos de 3 tiles se aleja hasta quedar a una distancia "comoda" de disparo
///   (SnakeRetreatState). Si la acorralan dispara igual.
/// - El veneno que le pega al jugador lo envenena; si ya estaba envenenado le hace el dano del mordisco.
/// - Siempre tiene que ir acompañada de un enemigo que no sea serpiente (EnsureNonSnakeCompany).
///
/// El resto del comportamiento (estados, animacion, proyectil) vive en SnakeRoamState / SnakeAttackState /
/// SnakeRetreatState, SnakeAnimator y SnakeVenomProjectile.
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyBehaviour))]
public class PoisonousSnake : MonoBehaviour
{
    /// <summary>Que tipo de punto se busca al reubicarse (ver TryPickPoint).</summary>
    public enum PickMode { Roam, Retreat, Reposition }

    /// <summary>
    /// Detecta cuando la serpiente quiere moverse pero no avanza (pegada a una pared o a una roca). Mide el avance
    /// neto en una ventana de tiempo, asi que no depende de los FPS ni de los pasos de la fisica.
    /// </summary>
    public struct StuckWatch
    {
        private Vector2 startPosition;
        private float timer;

        public void Reset(Vector2 position)
        {
            startPosition = position;
            timer = 0f;
        }

        /// <summary>true cuando paso 'window' segundos y en ese tiempo avanzo menos de 'minDistance'.</summary>
        public bool Tick(Vector2 position, float deltaTime, float window = 0.5f, float minDistance = 0.12f)
        {
            timer += deltaTime;
            if (timer < window)
                return false;

            bool stuck = Vector2.Distance(position, startPosition) < minDistance;
            startPosition = position;
            timer = 0f;
            return stuck;
        }
    }

    [Header("Alcance (en tiles)")]
    [Tooltip("GDD: detecta al jugador con linea de vision hasta 10 tiles. Es tambien la distancia maxima de disparo.")]
    [SerializeField] private float fireRangeTiles = 10f;
    [Tooltip("GDD: si el jugador se acerca a menos de 3 tiles, la serpiente se aleja.")]
    [SerializeField] private float tooCloseTiles = 3f;
    [Tooltip("Si el jugador ya esta envenenado y se le acerca (la persigue) dentro de esta distancia, huye y no escupe.")]
    [SerializeField] private float fleeIfChasedTiles = 8f;
    [Tooltip("Distancia 'apropiada' de disparo: al alejarse, se detiene cuando queda a esta distancia del jugador.")]
    [SerializeField] private float comfortDistanceTiles = 6f;
    [Tooltip("Cuantas unidades de Unity mide 1 tile.")]
    [SerializeField] private float tileSize = 1f;

    [Header("Movimiento")]
    [Tooltip("Velocidad mientras deambula (acecha).")]
    [SerializeField] private float roamSpeed = 1.7f;
    [Tooltip("Velocidad al reubicarse para volver a tener linea de disparo.")]
    [SerializeField] private float repositionSpeed = 2.4f;
    [Tooltip("Velocidad al alejarse del jugador.")]
    [SerializeField] private float retreatSpeed = 3.4f;

    [Header("Ataque")]
    [Tooltip("Segundos que tiene que ver al jugador antes de apuntar (le da una chance de reaccionar).")]
    [SerializeField] private float detectionDelay = 0.55f;
    [Tooltip("Pausa despues de cada escupitajo antes de preparar el siguiente.")]
    [SerializeField] private float shotCooldown = 1.3f;
    [Tooltip("Despues de acertarle al jugador deja de atacar este tiempo (sigue acechando).")]
    [SerializeField] private float calmAfterHit = 4f;
    [SerializeField] private float projectileSpeed = 6.5f;
    [SerializeField] private float projectileLifetime = 2.6f;
    [Tooltip("Dano del mordisco si el jugador YA estaba envenenado (medio corazon = 1).")]
    [SerializeField] private float attackDamage = 1f;
    [Tooltip("Distancia de la boca al centro del cuerpo cuando ataca de costado.")]
    [SerializeField] private float muzzleSideDistance = 0.95f;
    [Tooltip("Distancia de la boca al centro del cuerpo cuando ataca de frente.")]
    [SerializeField] private float muzzleFrontDistance = 0.45f;
    [SerializeField] private float muzzleHeight = 0.12f;

    [Header("Company")]
    [SerializeField] private GameObject companionPrefab;

    // Cuanto se puede correr un punto candidato hacia el NavMesh antes de considerarlo "dentro de una pared".
    private const float MaxSnapDistance = 0.75f;
    // Margen extra (sobre el radio de la gota) al comprobar la linea de disparo.
    private const float SightMargin = 0.1f;

    private EnemyMovement movement;
    private EnemyHealth health;
    private SnakeAnimator snakeAnimator;
    private SpriteRenderer spriteRenderer;

    private bool engaged;
    private bool shotBlocked;
    private float calmUntil;
    private float retreatSuppressedUntil;
    private float lastPlayerDistance = -1f;

    private ContactFilter2D sightFilter;
    private readonly RaycastHit2D[] sightBuffer = new RaycastHit2D[10];
    private Bounds roamArea;
    private bool hasRoamArea;

    public float AttackDamage => attackDamage;
    public GameObject CompanionPrefab => companionPrefab;

    public float FireRange => fireRangeTiles * tileSize;
    public float TooCloseRange => tooCloseTiles * tileSize;
    public float ComfortDistance => comfortDistanceTiles * tileSize;
    public float FleeIfChasedRange => fleeIfChasedTiles * tileSize;
    public float RoamSpeed => roamSpeed;
    public float RepositionSpeed => repositionSpeed;
    public float RetreatSpeed => retreatSpeed;
    public float DetectionDelay => detectionDelay;
    public float ShotCooldown => shotCooldown;

    /// <summary>true mientras sigue atacando (empezo a disparar y todavia no le acerto al jugador).</summary>
    public bool Engaged => engaged;
    /// <summary>true durante el descanso posterior a acertar un disparo.</summary>
    public bool IsCalm => Time.time < calmUntil;
    /// <summary>true si esta acorralada y no debe volver a huir por un momento (dispara en vez de alejarse).</summary>
    public bool RetreatSuppressed => Time.time < retreatSuppressedUntil;
    public SnakeAnimator Animator => snakeAnimator;

    private void Awake()
    {
        movement = GetComponent<EnemyMovement>();
        health = GetComponent<EnemyHealth>();
        snakeAnimator = GetComponent<SnakeAnimator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // La linea de vision / disparo la frenan las mismas capas que esquiva el movimiento (paredes, rocas, basura).
        sightFilter = new ContactFilter2D();
        sightFilter.useTriggers = false;
        sightFilter.SetLayerMask(LayerMask.GetMask("Default", "Trash", "Wall"));

        if (health != null)
            health.OnDeath += HandleDeath;
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnDeath -= HandleDeath;
    }

    // ------------------------------------------------------------------ estado de combate

    public void Engage()
    {
        engaged = true;
        shotBlocked = false;
    }

    /// <summary>Lo llama el proyectil cuando le acierta al jugador: deja de atacar un rato y vuelve a acechar.</summary>
    public void NotifyVenomHit()
    {
        engaged = false;
        calmUntil = Time.time + calmAfterHit;
        shotBlocked = false;
    }

    /// <summary>Lo llama el proyectil cuando se estrella contra una pared / roca / basura antes de llegar al jugador.</summary>
    public void NotifyVenomBlocked()
    {
        shotBlocked = true;
    }

    /// <summary>
    /// true si el ultimo escupitajo se estrello contra algo (se lee una sola vez). Si pasa, la serpiente tiene que
    /// cambiar de lugar en vez de seguir disparando contra lo mismo desde el mismo punto.
    /// </summary>
    public bool ConsumeBlockedShot()
    {
        bool blocked = shotBlocked;
        shotBlocked = false;
        return blocked;
    }

    /// <summary>Acorralada: durante 'seconds' no se aleja (si el jugador esta cerca, dispara).</summary>
    public void SuppressRetreat(float seconds)
    {
        retreatSuppressedUntil = Time.time + seconds;
    }

    public bool IsPlayerPoisoned(Transform player)
    {
        if (player == null)
            return false;

        PlayerPoisonStatus poison = player.GetComponent<PlayerPoisonStatus>();
        return poison != null && poison.IsPoisoned;
    }

    /// <summary>
    /// true si el jugador se esta acercando. Se llama una vez por frame desde el estado activo.
    /// </summary>
    public bool NotePlayerDistance(float distance)
    {
        bool closing = lastPlayerDistance >= 0f && distance < lastPlayerDistance - 0.02f;
        lastPlayerDistance = distance;
        return closing;
    }

    /// <summary>El jugador ya esta envenenado y la esta persiguiendo: hay que huir, no escupir.</summary>
    public bool ShouldFleeBecauseChased(Transform player, float distance, bool closingIn)
    {
        if (!IsPlayerPoisoned(player))
            return false;

        if (distance <= TooCloseRange)
            return true;

        return closingIn && distance <= FleeIfChasedRange;
    }

    // ------------------------------------------------------------------ disparo

    /// <summary>Posicion de la boca (de donde sale el veneno) para un disparo hacia 'aimDirection'.</summary>
    public Vector2 MuzzlePosition(Vector2 aimDirection)
    {
        Vector2 dir = aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : Vector2.down;
        float distance = SnakeAnimator.IsHorizontal(dir) ? muzzleSideDistance : muzzleFrontDistance;
        return (Vector2)transform.position + Vector2.up * muzzleHeight + dir * distance;
    }

    /// <summary>Escupe una gota de veneno hacia 'aimDirection'.</summary>
    public void Fire(Vector2 aimDirection)
    {
        RoomInstance room = GetComponentInParent<RoomInstance>();
        Transform parent = room != null ? room.transform : transform.parent;

        Sprite[] frames = snakeAnimator != null ? snakeAnimator.ProjectileFrames : null;
        float fps = snakeAnimator != null ? snakeAnimator.ProjectileFps : 14f;

        Vector2 aim = aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : Vector2.down;
        Vector2 origin = (Vector2)transform.position + Vector2.up * muzzleHeight;
        Vector2 muzzle = MuzzlePosition(aim);

        // Si la boca quedaria del otro lado de una pared (la serpiente esta pegada a ella y le apunta), el veneno nace
        // junto a la serpiente y se deshace contra la pared en vez de atravesarla.
        if (SegmentBlocked(origin, muzzle))
            muzzle = origin + aim * 0.1f;

        SnakeVenomProjectile.Spawn(muzzle, aim, frames, fps, projectileSpeed, projectileLifetime, attackDamage, this, parent);
    }

    /// <summary>
    /// Lo que pasa cuando el veneno le pega al jugador: lo envenena y, si ya estaba envenenado, le hace el dano del mordisco.
    /// </summary>
    public static void ApplyVenomHit(GameObject playerObject, GameObject source, float damage)
    {
        if (playerObject == null)
            return;

        PlayerPoisonStatus poison = playerObject.GetComponent<PlayerPoisonStatus>();
        if (poison == null)
            poison = playerObject.AddComponent<PlayerPoisonStatus>();

        if (poison.IsPoisoned)
        {
            IDamageable damageable = playerObject.GetComponent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamageFrom(damage, source);
        }
        else
        {
            poison.ApplyPoison();
        }
    }

    // ------------------------------------------------------------------ vision

    /// <summary>true si entre 'from' y 'to' no hay paredes, rocas ni basura que frenen el escupitajo.</summary>
    public bool HasLineOfFire(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        float distance = delta.magnitude;
        if (distance < 0.05f)
            return true;

        // Un poco mas ancho que la gota de veneno: si la gota pasa raspando una roca, la serpiente no lo da por libre.
        int count = Physics2D.CircleCast(from, SnakeVenomProjectile.HitRadius + SightMargin, delta / distance, sightFilter, sightBuffer, distance);
        for (int i = 0; i < count; i++)
        {
            if (IsSightBlocker(sightBuffer[i].collider))
                return false;
        }

        return true;
    }

    /// <summary>true si un segmento fino entre 'from' y 'to' cruza una pared, roca o basura.</summary>
    private bool SegmentBlocked(Vector2 from, Vector2 to)
    {
        if ((to - from).sqrMagnitude < 0.0001f)
            return false;

        int count = Physics2D.Linecast(from, to, sightFilter, sightBuffer);
        for (int i = 0; i < count; i++)
        {
            if (IsSightBlocker(sightBuffer[i].collider))
                return true;
        }

        return false;
    }

    /// <summary>Paredes, rocas y basura frenan el escupitajo; el jugador, los enemigos y los restos de rata no.</summary>
    private bool IsSightBlocker(Collider2D col)
    {
        if (col == null || col.isTrigger)
            return false;
        if (col.transform.IsChildOf(transform))
            return false;
        if (col.GetComponentInParent<PlayerHealth>() != null)
            return false;
        if (col.GetComponentInParent<EnemyHealth>() != null || col.GetComponentInParent<RatBody>() != null)
            return false;
        if (col.gameObject.name.IndexOf("floor", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return false;

        return true;
    }

    // ------------------------------------------------------------------ elegir a donde ir

    /// <summary>
    /// Busca un punto caminable de la room segun lo que necesite la serpiente:
    ///   Roam: a distancia media del jugador (acechar sin perseguir) y, de preferencia, con linea de vision.
    ///   Retreat: a distancia 'comoda' o mas, cerca de donde esta y, de preferencia, con linea de disparo.
    ///   Reposition: con linea de disparo y dentro del alcance.
    /// Devuelve false si no encontro ninguno.
    /// </summary>
    public bool TryPickPoint(Vector2 playerPosition, PickMode mode, out Vector2 point)
    {
        EnsureRoamArea();

        Vector2 self = transform.position;
        float tooClose = TooCloseRange;
        float comfort = ComfortDistance;
        float range = FireRange;

        point = self;
        bool found = false;
        float bestScore = float.NegativeInfinity;

        // El camino hacia el punto no tiene que pasar por encima del jugador: si pasa a menos de "demasiado cerca" la
        // serpiente se dispararia sola el estado de huida a mitad de camino (y quedaria yendo y viniendo).
        float pathClearance = Mathf.Min(tooClose + 0.4f, Vector2.Distance(self, playerPosition) - 0.1f);

        // Segunda pasada (relajada) por si la primera no encuentra nada, por ejemplo cuando esta casi acorralada.
        for (int pass = 0; pass < 2 && !found; pass++)
        {
            bool strict = pass == 0;

            for (int i = 0; i < 30; i++)
            {
                Vector2 candidate;

                if (mode != PickMode.Retreat && (i & 1) == 0)
                {
                    // La mitad de los puntos se prueba en un anillo alrededor del jugador a distancia de disparo: ahi es
                    // donde mas probable es tener linea libre aunque el jugador se esconda detras de algo.
                    float angle = Random.value * Mathf.PI * 2f;
                    float minRadius = tooClose + 1.5f;
                    float maxRadius = Mathf.Max(minRadius + 0.5f, Mathf.Min(range - 1f, comfort + 2f));
                    float radius = Random.Range(Mathf.Min(minRadius, comfort - 1.5f), maxRadius);
                    candidate = playerPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    candidate.x = Mathf.Clamp(candidate.x, roamArea.min.x, roamArea.max.x);
                    candidate.y = Mathf.Clamp(candidate.y, roamArea.min.y, roamArea.max.y);
                }
                else
                {
                    candidate = new Vector2(
                        Random.Range(roamArea.min.x, roamArea.max.x),
                        Random.Range(roamArea.min.y, roamArea.max.y));
                }

                if (!IsWalkable(candidate, out Vector2 snapped))
                    continue;

                candidate = snapped;
                float travel = Vector2.Distance(self, candidate);
                float distance = Vector2.Distance(candidate, playerPosition);
                float score;

                if (strict && mode != PickMode.Retreat && DistanceToSegment(playerPosition, self, candidate) < pathClearance)
                    continue;

                switch (mode)
                {
                    case PickMode.Retreat:
                        if (strict && distance < comfort)
                            continue;
                        if (!strict && distance <= Vector2.Distance(self, playerPosition))
                            continue;

                        score = Mathf.Min(distance, range) * (strict ? 0.2f : 1f) - travel * 0.25f + Random.value * 0.5f;
                        if (strict && HasLineOfFire(candidate + Vector2.up * muzzleHeight, playerPosition))
                            score += 2f;
                        break;

                    case PickMode.Reposition:
                        if (strict && (distance < tooClose + 1.2f || distance > range - 0.8f))
                            continue;
                        if (!strict && distance < tooClose + 0.8f)
                            continue;

                        score = -travel * 0.3f + Random.value * 0.5f;
                        if (HasLineOfFire(candidate + Vector2.up * muzzleHeight, playerPosition))
                            score += 4f;
                        break;

                    default: // Roam
                        if (travel < 1.5f)
                            continue;
                        if (strict && distance < tooClose + 1f)
                            continue;
                        if (!strict && distance < tooClose + 0.5f)
                            continue;

                        // Acechar: prefiere los puntos desde donde ve al jugador (asi lo termina encontrando aunque se
                        // esconda detras de una roca), sin acercarse mas de la distancia comoda.
                        score = -Mathf.Abs(distance - comfort) * 0.5f + Random.value * 1.2f;
                        if (strict && HasLineOfFire(candidate + Vector2.up * muzzleHeight, playerPosition))
                            score += 2.2f;
                        break;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    point = candidate;
                    found = true;
                }
            }
        }

        return found;
    }

    private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lengthSq = ab.sqrMagnitude;
        if (lengthSq < 0.0001f)
            return Vector2.Distance(point, a);

        float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSq);
        return Vector2.Distance(point, a + ab * t);
    }

    private void EnsureRoamArea()
    {
        if (hasRoamArea)
            return;

        hasRoamArea = true;
        RoomInstance room = GetComponentInParent<RoomInstance>();
        RoomBakedNavMesh baked = room != null ? room.GetComponent<RoomBakedNavMesh>() : null;

        Vector3 center = room != null ? room.transform.position : transform.position;
        roamArea = baked != null ? baked.InteriorBounds : new Bounds(center, new Vector3(14f, 6f, 1f));

        // Lejos de las paredes: asi no se pega a los bordes ni elige puntos donde se traba.
        roamArea.Expand(new Vector3(-1.4f, -1.4f, 0f));
        if (roamArea.size.x < 1f || roamArea.size.y < 1f)
            roamArea = new Bounds(center, new Vector3(8f, 3f, 1f));
    }

    private bool IsWalkable(Vector2 point, out Vector2 snapped)
    {
        snapped = point;

        DungeonNavMesh mesh = DungeonNavMesh.Instance;
        if (mesh != null && mesh.IsReady)
        {
            // Radio de busqueda por defecto del NavMesh: la superficie bakeada queda ~0.5 por detras del plano XY del
            // juego, asi que un radio chico nunca encuentra nada. Si hubo que correr el punto mas de MaxSnap, el punto
            // original estaba metido en una pared o una roca.
            if (!DungeonNavMesh.SampleWalkable(point, out snapped))
                return false;

            return Vector2.Distance(point, snapped) <= MaxSnapDistance;
        }

        RoomPathGrid grid = RoomPathGrid.For(transform);
        return grid != null && grid.IsWalkableWorld(point);
    }

    // ------------------------------------------------------------------ muerte

    private void HandleDeath()
    {
        if (snakeAnimator == null)
            return;

        RoomInstance room = GetComponentInParent<RoomInstance>();
        Transform parent = room != null ? room.transform : transform.parent;
        SnakeDeathFx.Spawn(snakeAnimator.DeathFrames, snakeAnimator.DeathFps, transform.position, transform.lossyScale, spriteRenderer, parent);
    }

    // ------------------------------------------------------------------ compañia

    /// <summary>GDD: la serpiente nunca esta sola. Si en la room no hay otro enemigo que no sea serpiente, aparece uno.</summary>
    public static void EnsureNonSnakeCompany(RoomInstance room)
    {
        if (room == null)
            return;

        EnemyBehaviour[] behaviours = room.GetComponentsInChildren<EnemyBehaviour>(true);
        bool hasSnake = false;
        bool hasNonSnake = false;
        PoisonousSnake snake = null;

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null)
                continue;

            if (behaviours[i].Type == EnemyType.Snake)
            {
                hasSnake = true;
                if (snake == null)
                    snake = behaviours[i].GetComponent<PoisonousSnake>();
            }
            else
            {
                hasNonSnake = true;
            }
        }

        if (!hasSnake || hasNonSnake || snake == null || snake.companionPrefab == null)
            return;

        Vector3 spawnPos = snake.transform.position + Vector3.right * 1.1f;
        RoomPathGrid grid = RoomPathGrid.For(room.transform);
        if (grid != null && !grid.IsWalkableWorld(spawnPos))
            spawnPos = snake.transform.position + Vector3.left * 1.1f;

        GameObject companion = Instantiate(snake.companionPrefab, spawnPos, Quaternion.identity, room.transform);
        room.RegisterSpawnedCombatEnemy(companion);
    }
}
