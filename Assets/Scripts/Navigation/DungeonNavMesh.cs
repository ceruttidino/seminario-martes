using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Activa el NavMesh ya bakeado de la room actual. Solo rebuild en runtime
/// si se rompe una roca/basura (InvalidatePathGrid).
///
/// No dibuja nada en pantalla: el NavMesh es solo datos para el pathfinding de los enemigos. Para verlo hay que
/// abrir el prefab de la room y seleccionar el componente RoomBakedNavMesh (la Scene muestra el piso y los recortes).
/// </summary>
[DefaultExecutionOrder(-50)]
public class DungeonNavMesh : MonoBehaviour
{
    public static DungeonNavMesh Instance { get; private set; }

    [SerializeField] private float sampleRadius = 1.6f;
    [SerializeField] private float cornerSkipDistance = 0.28f;

    private NavMeshPath sharedPath;
    private NavMeshQueryFilter queryFilter;
    private NavMeshDataInstance dataInstance;
    private NavMeshData runtimeData;
    private RoomInstance pendingRoom;
    private Coroutine bakeRoutine;
    private bool ready;

    public RoomInstance CurrentRoom { get; private set; }
    public Bounds InteriorBounds { get; private set; }
    public float AgentRadius => CurrentRoom != null
        ? RoomNavMeshBuild.GetSettings(CurrentRoom).agentRadius
        : RoomBakeSettings.Default.agentRadius;
    public bool IsReady => ready && dataInstance.valid;

    public static DungeonNavMesh Ensure()
    {
        if (Instance != null)
            return Instance;

        DungeonNavMesh existing = FindFirstObjectByType<DungeonNavMesh>();
        if (existing != null)
            return existing;

        GameObject go = new GameObject("NavMesh");
        return go.AddComponent<DungeonNavMesh>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        sharedPath = new NavMeshPath();
        queryFilter = RoomNavMeshBuild.CreateFilter();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        RemoveInstance();
    }

    public void RequestBake(RoomInstance room)
    {
        ActivateRoom(room);
    }

    public void Rebuild(RoomInstance room)
    {
        if (room == null)
            return;

        pendingRoom = room;
        if (bakeRoutine != null)
            StopCoroutine(bakeRoutine);
        bakeRoutine = StartCoroutine(RebuildNextFrame(room));
    }

    private IEnumerator RebuildNextFrame(RoomInstance room)
    {
        yield return null;
        if (pendingRoom != room || room == null || !room.gameObject.activeInHierarchy)
        {
            bakeRoutine = null;
            yield break;
        }

        BakeRuntime(room, false);
        bakeRoutine = null;
    }

    private void ActivateRoom(RoomInstance room)
    {
        if (room == null)
            return;

        CurrentRoom = room;
        RoomBakedNavMesh baked = room.GetComponent<RoomBakedNavMesh>();
        if (baked != null && baked.HasData)
        {
            InteriorBounds = baked.InteriorBounds;
            BindData(baked.Data);
            return;
        }

        BakeRuntime(room, true);
    }

    private void BakeRuntime(RoomInstance room, bool missingBake)
    {
        CurrentRoom = room;
        runtimeData = RoomNavMeshBuild.Build(room, out Bounds interior);
        InteriorBounds = interior;
        BindData(runtimeData);

        if (missingBake)
        {
            Debug.LogWarning($"[NavMesh] La room '{room.name}' no tenía NavMesh pre-bakeado y se bakeó en runtime ({(IsReady ? "OK" : "FALLÓ")}). " +
                             "Bakealo desde el menú Raccgeon > Bakear NavMesh faltantes.");
        }
    }

    private void BindData(NavMeshData data)
    {
        RemoveInstance();
        if (data == null)
        {
            ready = false;
            return;
        }

        dataInstance = NavMesh.AddNavMeshData(data, Vector3.zero, RoomNavMeshBuild.BakeRotation);
        ready = dataInstance.valid;
    }

    private void RemoveInstance()
    {
        if (dataInstance.valid)
            dataInstance.Remove();
        dataInstance = default;
        ready = false;
    }

    public static bool TryGetNextCorner(Vector2 from, Vector2 goal, out Vector2 next)
    {
        next = goal;
        DungeonNavMesh mesh = Instance;
        if (mesh == null || !mesh.IsReady)
            return false;

        Vector3 from3 = from;
        Vector3 goal3 = goal;

        if (!NavMesh.SamplePosition(from3, out NavMeshHit startHit, mesh.sampleRadius, mesh.queryFilter))
            return false;
        if (!NavMesh.SamplePosition(goal3, out NavMeshHit goalHit, mesh.sampleRadius, mesh.queryFilter))
            return false;

        from3 = startHit.position;
        goal3 = goalHit.position;
        next = goal3;

        if (!NavMesh.Raycast(from3, goal3, out _, mesh.queryFilter))
            return true;

        if (mesh.sharedPath == null)
            mesh.sharedPath = new NavMeshPath();

        if (!NavMesh.CalculatePath(from3, goal3, mesh.queryFilter, mesh.sharedPath))
            return false;
        if (mesh.sharedPath.status != NavMeshPathStatus.PathComplete &&
            mesh.sharedPath.status != NavMeshPathStatus.PathPartial)
            return false;
        if (mesh.sharedPath.corners == null || mesh.sharedPath.corners.Length < 2)
            return false;

        float skip = mesh.cornerSkipDistance * mesh.cornerSkipDistance;
        for (int i = 1; i < mesh.sharedPath.corners.Length; i++)
        {
            Vector2 corner = mesh.sharedPath.corners[i];
            if ((corner - from).sqrMagnitude > skip)
            {
                next = corner;
                return true;
            }
        }

        next = mesh.sharedPath.corners[mesh.sharedPath.corners.Length - 1];
        return true;
    }

    public static bool SampleWalkable(Vector2 world, out Vector2 walkable, float radius = -1f)
    {
        walkable = world;
        DungeonNavMesh mesh = Instance;
        if (mesh == null || !mesh.IsReady)
            return false;

        float sample = radius > 0f ? radius : mesh.sampleRadius;
        if (!NavMesh.SamplePosition(world, out NavMeshHit hit, sample, mesh.queryFilter))
            return false;

        walkable = hit.position;
        return true;
    }

    /// <summary>Punto sobre la malla (incluye la Z real del bake). Sirve para colocar un NavMeshAgent.</summary>
    public static bool TryProject(Vector2 world, out Vector3 onMesh, float radius = -1f)
    {
        onMesh = world;
        DungeonNavMesh mesh = Instance;
        if (mesh == null || !mesh.IsReady)
            return false;

        float sample = radius > 0f ? radius : mesh.sampleRadius;
        if (!NavMesh.SamplePosition(world, out NavMeshHit hit, sample, mesh.queryFilter))
            return false;

        onMesh = hit.position;
        return true;
    }

    /// <summary>
    /// Recorta un paso para que no salga de la malla ni atraviese un obstaculo bakeado.
    /// El movimiento de los enemigos tiene que usar esto (posicion), no la velocidad del Rigidbody.
    /// </summary>
    public static bool ClampStep(Vector2 from, Vector2 to, out Vector2 clamped, out Vector2 hitNormal, out bool blocked)
    {
        clamped = to;
        hitNormal = Vector2.zero;
        blocked = false;

        DungeonNavMesh mesh = Instance;
        if (mesh == null || !mesh.IsReady)
            return false;

        float sample = mesh.sampleRadius;
        if (!NavMesh.SamplePosition(from, out NavMeshHit fromHit, sample, mesh.queryFilter))
            return false;

        Vector3 from3 = fromHit.position;
        Vector3 to3 = new Vector3(to.x, to.y, from3.z);
        bool toOnMesh = NavMesh.SamplePosition(to3, out NavMeshHit toHit, sample, mesh.queryFilter);
        if (toOnMesh)
            to3 = toHit.position;

        if (NavMesh.Raycast(from3, to3, out NavMeshHit rayHit, mesh.queryFilter))
        {
            blocked = true;
            clamped = rayHit.position;
            hitNormal = PlanarNormal(rayHit.normal, from - to);
            return true;
        }

        if (!toOnMesh)
        {
            blocked = true;
            clamped = from3;
            hitNormal = PlanarNormal(Vector3.zero, from - to);
            return true;
        }

        clamped = to3;
        return true;
    }

    private static Vector2 PlanarNormal(Vector3 normal, Vector2 fallback)
    {
        Vector2 planar = new Vector2(normal.x, normal.y);
        if (planar.sqrMagnitude < 0.0001f)
            planar = fallback;

        return planar.sqrMagnitude > 0.0001f ? planar.normalized : Vector2.zero;
    }

    public static bool HasClearLine(Vector2 from, Vector2 to)
    {
        if (Instance == null || !Instance.IsReady)
            return false;

        if (!NavMesh.SamplePosition(from, out NavMeshHit startHit, Instance.sampleRadius, Instance.queryFilter))
            return false;
        if (!NavMesh.SamplePosition(to, out NavMeshHit goalHit, Instance.sampleRadius, Instance.queryFilter))
            return false;

        return !NavMesh.Raycast(startHit.position, goalHit.position, out _, Instance.queryFilter);
    }
}
