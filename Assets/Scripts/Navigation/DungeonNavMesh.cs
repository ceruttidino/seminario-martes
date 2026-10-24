using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

/// <summary>
/// Activa el NavMesh ya bakeado de la room actual. Solo rebuild en runtime
/// si se rompe una roca/basura (InvalidatePathGrid).
/// </summary>
[DefaultExecutionOrder(-50)]
public class DungeonNavMesh : MonoBehaviour
{
    public static DungeonNavMesh Instance { get; private set; }

    [SerializeField] private float sampleRadius = 1.6f;
    [SerializeField] private float cornerSkipDistance = 0.28f;
    [SerializeField] private bool showOverlay = true;

    private NavMeshPath sharedPath;
    private NavMeshQueryFilter queryFilter;
    private NavMeshDataInstance dataInstance;
    private NavMeshData runtimeData;
    private RoomInstance pendingRoom;
    private Coroutine bakeRoutine;
    private bool ready;
    private Mesh overlayMesh;
    private MeshFilter overlayFilter;
    private MeshRenderer overlayRenderer;

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
        EnsureOverlay();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        RemoveInstance();

        GameObject overlay = GameObject.Find("NavMeshDebugOverlay");
        if (overlay != null)
            Destroy(overlay);
        if (overlayMesh != null)
            Destroy(overlayMesh);
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

        BakeRuntime(room);
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
            RefreshOverlay();
            Debug.Log($"[NavMesh] Room '{room.name}': mesh pre-bakeado. Tecla N = overlay.");
            return;
        }

        BakeRuntime(room);
    }

    private void BakeRuntime(RoomInstance room)
    {
        CurrentRoom = room;
        runtimeData = RoomNavMeshBuild.Build(room, out Bounds interior);
        InteriorBounds = interior;
        BindData(runtimeData);
        RefreshOverlay();

        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        int triangles = triangulation.indices != null ? triangulation.indices.Length / 3 : 0;
        Debug.Log($"[NavMesh] Bake runtime de '{room.name}': {(IsReady ? "OK" : "FALLÓ")} · {triangles} triángulos · " +
                  $"piso {InteriorBounds.size.x:0.0}x{InteriorBounds.size.y:0.0}. Tecla N = overlay.");
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

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame)
        {
            showOverlay = !showOverlay;
            if (overlayRenderer != null)
                overlayRenderer.enabled = showOverlay;
        }
    }

    private void EnsureOverlay()
    {
        GameObject overlay = GameObject.Find("NavMeshDebugOverlay");
        if (overlay == null)
            overlay = new GameObject("NavMeshDebugOverlay");
        overlay.transform.SetParent(null, false);
        overlay.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        overlayFilter = overlay.GetComponent<MeshFilter>();
        if (overlayFilter == null)
            overlayFilter = overlay.AddComponent<MeshFilter>();

        overlayRenderer = overlay.GetComponent<MeshRenderer>();
        if (overlayRenderer == null)
            overlayRenderer = overlay.AddComponent<MeshRenderer>();

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        Material material = new Material(shader);
        material.color = new Color(0.15f, 0.85f, 1f, 0.38f);
        overlayRenderer.sharedMaterial = material;
        overlayRenderer.sortingOrder = 40;
        overlayRenderer.enabled = showOverlay;
    }

    private void RefreshOverlay()
    {
        if (overlayFilter == null)
            EnsureOverlay();

        if (overlayMesh == null)
            overlayMesh = new Mesh { name = "NavMeshOverlay" };
        else
            overlayMesh.Clear();

        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        if (triangulation.vertices == null || triangulation.indices == null || triangulation.indices.Length < 3)
        {
            overlayFilter.sharedMesh = overlayMesh;
            if (overlayRenderer != null)
                overlayRenderer.enabled = false;
            return;
        }

        Bounds clip = InteriorBounds;
        clip.Expand(0.4f);

        List<Vector3> vertices = new List<Vector3>(triangulation.vertices.Length);
        List<int> triangles = new List<int>(triangulation.indices.Length);
        Dictionary<int, int> remap = new Dictionary<int, int>(triangulation.vertices.Length);

        for (int i = 0; i + 2 < triangulation.indices.Length; i += 3)
        {
            int ia = triangulation.indices[i];
            int ib = triangulation.indices[i + 1];
            int ic = triangulation.indices[i + 2];
            Vector3 a = triangulation.vertices[ia];
            Vector3 b = triangulation.vertices[ib];
            Vector3 c = triangulation.vertices[ic];
            Vector3 mid = (a + b + c) / 3f;
            if (!clip.Contains(new Vector3(mid.x, mid.y, clip.center.z)))
                continue;

            triangles.Add(RemapVertex(vertices, remap, ia, a));
            triangles.Add(RemapVertex(vertices, remap, ib, b));
            triangles.Add(RemapVertex(vertices, remap, ic, c));
        }

        overlayMesh.SetVertices(vertices);
        overlayMesh.SetTriangles(triangles, 0);
        overlayMesh.RecalculateBounds();
        overlayMesh.RecalculateNormals();
        overlayFilter.sharedMesh = overlayMesh;
        if (overlayRenderer != null)
            overlayRenderer.enabled = showOverlay && triangles.Count > 0;
    }

    private static int RemapVertex(List<Vector3> vertices, Dictionary<int, int> remap, int sourceIndex, Vector3 vertex)
    {
        if (remap.TryGetValue(sourceIndex, out int mapped))
            return mapped;

        vertex.z = -0.05f;
        mapped = vertices.Count;
        vertices.Add(vertex);
        remap.Add(sourceIndex, mapped);
        return mapped;
    }

    private void OnDrawGizmos()
    {
        if (!ready)
            return;

        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.8f);
        Gizmos.DrawWireCube(InteriorBounds.center, InteriorBounds.size);

        if (overlayMesh == null || overlayMesh.vertexCount == 0)
            return;

        Gizmos.color = new Color(0.15f, 0.9f, 1f, 0.35f);
        Vector3[] vertices = overlayMesh.vertices;
        int[] indices = overlayMesh.triangles;
        for (int i = 0; i + 2 < indices.Length; i += 3)
        {
            Gizmos.DrawLine(vertices[indices[i]], vertices[indices[i + 1]]);
            Gizmos.DrawLine(vertices[indices[i + 1]], vertices[indices[i + 2]]);
            Gizmos.DrawLine(vertices[indices[i + 2]], vertices[indices[i]]);
        }
    }
}
