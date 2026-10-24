using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NavMesh 2D bakeado en el editor. El recuadro verde se puede ajustar a mano
/// en la Scene (componente seleccionado) y después bakear esa room.
/// </summary>
[DisallowMultipleComponent]
[ExecuteAlways]
public class RoomBakedNavMesh : MonoBehaviour
{
    [SerializeField] private NavMeshData navMeshData;

    [Header("Área caminable")]
    [SerializeField] private bool useManualInterior;
    [SerializeField] private Vector3 interiorCenter;
    [SerializeField] private Vector3 interiorSize = new Vector3(14f, 7f, 1f);
    [SerializeField] [Min(0f)] private float floorInset;

    [Header("Precisión del bake")]
    [SerializeField] [Range(0.01f, 0.2f)] private float agentRadius = 0.02f;
    [SerializeField] [Range(0.015f, 0.1f)] private float voxelSize = 0.03f;
    [SerializeField] private bool carveWalls = true;

    public NavMeshData Data => navMeshData;
    public bool HasData => navMeshData != null;
    public bool UseManualInterior => useManualInterior;
    public Bounds InteriorBounds => new Bounds(interiorCenter, new Vector3(interiorSize.x, interiorSize.y, 1f));

    public RoomBakeSettings ToSettings()
    {
        return new RoomBakeSettings
        {
            useManualInterior = useManualInterior,
            interiorCenter = interiorCenter,
            interiorSize = interiorSize,
            floorInset = floorInset,
            agentRadius = agentRadius,
            voxelSize = voxelSize,
            carveWalls = carveWalls
        };
    }

    public void Assign(NavMeshData data, Bounds interior)
    {
        navMeshData = data;
        if (!useManualInterior)
        {
            interiorCenter = interior.center;
            interiorSize = new Vector3(interior.size.x, interior.size.y, 1f);
        }
    }

    public void SetManualInterior(Vector3 center, Vector3 size)
    {
        useManualInterior = true;
        interiorCenter = new Vector3(center.x, center.y, 0f);
        interiorSize = new Vector3(Mathf.Max(1f, Mathf.Abs(size.x)), Mathf.Max(1f, Mathf.Abs(size.y)), 1f);
    }

    public void RecalculateInteriorFromWalls()
    {
        useManualInterior = false;
        RoomInstance room = GetComponent<RoomInstance>();
        Bounds interior = RoomNavMeshBuild.ComputeInteriorBounds(room, floorInset);
        interiorCenter = interior.center;
        interiorSize = new Vector3(interior.size.x, interior.size.y, 1f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.15f, 1f, 0.35f, 0.9f);
        Gizmos.DrawWireCube(interiorCenter, new Vector3(interiorSize.x, interiorSize.y, 0.05f));

        RoomInstance room = GetComponent<RoomInstance>();
        if (room == null)
            return;

        var holes = new System.Collections.Generic.List<Bounds>(32);
        RoomNavMeshBuild.CollectPreviewHoles(room, ToSettings(), holes);
        Gizmos.color = new Color(1f, 0.2f, 0.15f, 0.85f);
        for (int i = 0; i < holes.Count; i++)
            Gizmos.DrawWireCube(holes[i].center, holes[i].size);
    }
}
