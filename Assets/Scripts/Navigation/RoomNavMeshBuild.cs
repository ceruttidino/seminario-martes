using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Tilemaps;

[System.Serializable]
public struct RoomBakeSettings
{
    public bool useManualInterior;
    public Vector3 interiorCenter;
    public Vector3 interiorSize;
    public float floorInset;
    public float agentRadius;
    public float voxelSize;
    public bool carveWalls;

    public static RoomBakeSettings Default => new RoomBakeSettings
    {
        useManualInterior = false,
        interiorCenter = Vector3.zero,
        interiorSize = new Vector3(14f, 7f, 1f),
        floorInset = 0f,
        agentRadius = 0.02f,
        voxelSize = 0.03f,
        carveWalls = true
    };
}

/// <summary>
/// Bake 2D de una room: piso = interior entre paredes, obstáculos = collider exacto.
/// </summary>
public static class RoomNavMeshBuild
{
    public const int WalkableArea = 0;
    public const int NotWalkableArea = 1;
    public const int AgentTypeId = 0;

    public static readonly Quaternion BakeRotation = Quaternion.Euler(270f, 0f, 0f);

    public static RoomBakeSettings GetSettings(RoomInstance room)
    {
        RoomBakedNavMesh baked = room != null ? room.GetComponent<RoomBakedNavMesh>() : null;
        return baked != null ? baked.ToSettings() : RoomBakeSettings.Default;
    }

    public static NavMeshBuildSettings CreateSettings(RoomBakeSettings bake)
    {
        NavMeshBuildSettings settings = NavMesh.GetSettingsByID(AgentTypeId);
        settings.agentTypeID = AgentTypeId;
        settings.agentRadius = Mathf.Max(0.01f, bake.agentRadius);
        settings.agentHeight = 1f;
        settings.agentClimb = 0.4f;
        settings.agentSlope = 60f;
        settings.overrideVoxelSize = true;
        settings.voxelSize = Mathf.Max(0.015f, bake.voxelSize);
        settings.minRegionArea = 0f;
        return settings;
    }

    public static NavMeshQueryFilter CreateFilter()
    {
        return new NavMeshQueryFilter
        {
            agentTypeID = AgentTypeId,
            areaMask = NavMesh.AllAreas
        };
    }

    public static NavMeshData Build(RoomInstance room, out Bounds interior)
    {
        return Build(room, GetSettings(room), out interior);
    }

    public static NavMeshData Build(RoomInstance room, RoomBakeSettings bake, out Bounds interior)
    {
        List<Mesh> temps = new List<Mesh>();
        List<NavMeshBuildSource> sources = PrepareSources(room, bake, out interior, temps);
        Bounds local = ToBakeBounds(interior);
        NavMeshData data = NavMeshBuilder.BuildNavMeshData(CreateSettings(bake), sources, local, Vector3.zero, BakeRotation);
        DestroyTemps(temps);
        return data;
    }

    public static bool Update(NavMeshData data, RoomInstance room, out Bounds interior)
    {
        return Update(data, room, GetSettings(room), out interior);
    }

    public static bool Update(NavMeshData data, RoomInstance room, RoomBakeSettings bake, out Bounds interior)
    {
        if (data == null)
        {
            interior = default;
            return false;
        }

        List<Mesh> temps = new List<Mesh>();
        List<NavMeshBuildSource> sources = PrepareSources(room, bake, out interior, temps);
        bool ok = NavMeshBuilder.UpdateNavMeshData(data, CreateSettings(bake), sources, ToBakeBounds(interior));
        DestroyTemps(temps);
        return ok;
    }

    public static List<NavMeshBuildSource> PrepareSources(
        RoomInstance room,
        RoomBakeSettings bake,
        out Bounds interior,
        List<Mesh> temps)
    {
        Physics2D.SyncTransforms();
        interior = ResolveInterior(room, bake);

        List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>(64);
        CollectSources(room, interior, bake, sources, temps);
        return sources;
    }

    public static Bounds ResolveInterior(RoomInstance room, RoomBakeSettings bake)
    {
        if (bake.useManualInterior && bake.interiorSize.x > 1f && bake.interiorSize.y > 1f)
        {
            return new Bounds(
                new Vector3(bake.interiorCenter.x, bake.interiorCenter.y, 0f),
                new Vector3(bake.interiorSize.x, bake.interiorSize.y, 1f));
        }

        return ComputeInteriorBounds(room, bake.floorInset);
    }

    public static void CollectPreviewHoles(RoomInstance room, RoomBakeSettings bake, List<Bounds> holes)
    {
        if (room == null || holes == null)
            return;

        Collider2D[] colliders = room.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D col = colliders[i];
            if (col == null)
                continue;

            bool wall = IsRoomBorderWall(col);
            if (wall && !bake.carveWalls)
                continue;
            if (!wall && ShouldIgnoreCollider(col))
                continue;

            holes.Add(GetColliderBounds(col));
        }
    }

    public static void CollectSources(
        RoomInstance room,
        Bounds interior,
        RoomBakeSettings bake,
        List<NavMeshBuildSource> sources,
        List<Mesh> temps)
    {
        sources.Add(MakeBox(interior.center, interior.size, WalkableArea));
        if (room == null)
            return;

        Collider2D[] colliders = room.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D col = colliders[i];
            if (col == null)
                continue;

            bool wall = IsRoomBorderWall(col);
            if (wall)
            {
                if (bake.carveWalls)
                    AddColliderSource(sources, col, temps);
                continue;
            }

            if (ShouldIgnoreCollider(col))
                continue;

            AddColliderSource(sources, col, temps);
        }

        Tilemap[] tilemaps = room.GetComponentsInChildren<Tilemap>(true);
        for (int i = 0; i < tilemaps.Length; i++)
        {
            Tilemap tilemap = tilemaps[i];
            if (tilemap == null)
                continue;

            string name = tilemap.gameObject.name;
            bool isVoid = NameContains(name, "void")
                          || NameContains(name, "vacio")
                          || NameContains(name, "vacío");
            if (!isVoid)
                continue;

            BoundsInt cells = tilemap.cellBounds;
            Vector3 cellSize = tilemap.layoutGrid != null ? tilemap.layoutGrid.cellSize : Vector3.one;
            for (int x = cells.xMin; x < cells.xMax; x++)
            {
                for (int y = cells.yMin; y < cells.yMax; y++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    if (!tilemap.HasTile(cell))
                        continue;

                    sources.Add(MakeBox(tilemap.GetCellCenterWorld(cell), cellSize, NotWalkableArea));
                }
            }
        }
    }

    public static Bounds ComputeInteriorBounds(RoomInstance room, float inset)
    {
        Bounds bounds = room != null
            ? ComputeInnerWallBounds(room)
            : new Bounds(Vector3.zero, new Vector3(16f, 8f, 1f));

        if (inset > 0f)
            bounds.Expand(new Vector3(-inset * 2f, -inset * 2f, 0f));

        if (bounds.size.x < 4f || bounds.size.y < 3f)
            bounds = new Bounds(bounds.center, new Vector3(14f, 7f, 1f));

        bounds.center = new Vector3(bounds.center.x, bounds.center.y, 0f);
        bounds.size = new Vector3(bounds.size.x, bounds.size.y, 1f);
        return bounds;
    }

    public static bool IsRoomBorderWall(Collider2D col)
    {
        if (col == null || !(col is BoxCollider2D))
            return false;
        if (col is TilemapCollider2D || col.GetComponent<Tilemap>() != null)
            return false;

        string name = col.gameObject.name;
        if (string.IsNullOrEmpty(name))
            return false;

        return name.StartsWith("Wall_", System.StringComparison.OrdinalIgnoreCase)
               || name.Equals("Wall", System.StringComparison.OrdinalIgnoreCase);
    }

    public static bool ShouldIgnoreCollider(Collider2D col)
    {
        if (col == null || !col.enabled) return true;
        if (!col.gameObject.activeSelf) return true;
        if (col.isTrigger) return true;
        if (col is TilemapCollider2D || col is CompositeCollider2D) return true;
        if (col.GetComponent<Tilemap>() != null) return true;
        if (NameContains(col.gameObject.name, "floor")) return true;
        if (NameContains(col.gameObject.name, "wall")) return true;
        if (col.GetComponent<RoomDoor>() != null) return true;
        if (col.GetComponent<EnemyHealth>() != null) return true;
        if (col.GetComponent<RatBody>() != null) return true;
        if (col.GetComponent<PlayerHealth>() != null) return true;
        if (col.CompareTag("Player")) return true;
        if (col.GetComponent<LootPickup>() != null) return true;
        if (col.GetComponent<UpgradePickup>() != null) return true;
        if (col.GetComponent<SlimeTile>() != null) return true;
        if (col.GetComponent<BloodPool>() != null) return true;
        if (col.GetComponent<DiggingSpot>() != null) return true;
        if (col.GetComponent<BaseTrap>() != null) return true;
        if (col.GetComponent<TurtleShell>() != null) return true;
        if (col.GetComponent<PlayerFeetMarker>() != null) return true;
        return false;
    }

    public static bool NameContains(string name, string token)
    {
        return !string.IsNullOrEmpty(name) &&
               name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static Bounds ComputeInnerWallBounds(RoomInstance room)
    {
        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;
        bool found = false;

        Collider2D[] colliders = room.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D col = colliders[i];
            if (!IsRoomBorderWall(col) || !col.enabled)
                continue;

            Bounds b = GetColliderBounds(col);
            if (b.size.x >= b.size.y)
            {
                minX = Mathf.Min(minX, b.min.x);
                maxX = Mathf.Max(maxX, b.max.x);
                if (b.center.y > room.transform.position.y)
                    maxY = Mathf.Min(maxY, b.min.y);
                else
                    minY = Mathf.Max(minY, b.max.y);
            }
            else
            {
                minY = Mathf.Min(minY, b.min.y);
                maxY = Mathf.Max(maxY, b.max.y);
                if (b.center.x > room.transform.position.x)
                    maxX = Mathf.Min(maxX, b.min.x);
                else
                    minX = Mathf.Max(minX, b.max.x);
            }

            found = true;
        }

        if (!found || maxX <= minX || maxY <= minY)
            return FallbackFloorBounds(room);

        Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        return new Bounds(center, new Vector3(maxX - minX, maxY - minY, 1f));
    }

    private static Bounds FallbackFloorBounds(RoomInstance room)
    {
        SpriteRenderer[] sprites = room.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] == null || !NameContains(sprites[i].gameObject.name, "floor"))
                continue;
            Bounds b = sprites[i].bounds;
            b.center = new Vector3(b.center.x, b.center.y, 0f);
            b.size = new Vector3(Mathf.Max(1f, b.size.x - 4f), Mathf.Max(1f, b.size.y - 4f), 1f);
            return b;
        }

        return new Bounds(room.transform.position, new Vector3(14f, 6f, 1f));
    }

    private static void AddColliderSource(List<NavMeshBuildSource> sources, Collider2D col, List<Mesh> temps)
    {
        if (col is BoxCollider2D box)
        {
            Vector3 center = col.transform.TransformPoint(box.offset);
            Vector3 size = Vector3.Scale(box.size, col.transform.lossyScale);
            sources.Add(new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                transform = Matrix4x4.TRS(
                    new Vector3(center.x, center.y, 0f),
                    Quaternion.Euler(0f, 0f, col.transform.eulerAngles.z),
                    Vector3.one),
                size = new Vector3(Mathf.Max(0.05f, Mathf.Abs(size.x)), Mathf.Max(0.05f, Mathf.Abs(size.y)), 1f),
                area = NotWalkableArea
            });
            return;
        }

        Mesh mesh = col.CreateMesh(false, false);
        if (mesh != null && mesh.vertexCount > 2)
        {
            temps?.Add(mesh);
            sources.Add(new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Mesh,
                sourceObject = mesh,
                transform = col.transform.localToWorldMatrix,
                area = NotWalkableArea
            });
            return;
        }

        Bounds b = GetColliderBounds(col);
        sources.Add(MakeBox(b.center, b.size, NotWalkableArea));
    }

    public static Bounds GetColliderBounds(Collider2D col)
    {
        if (col is BoxCollider2D box)
        {
            Vector3 center = col.transform.TransformPoint(box.offset);
            Vector3 size = Vector3.Scale(box.size, col.transform.lossyScale);
            return new Bounds(
                new Vector3(center.x, center.y, 0f),
                new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), 1f));
        }

        Bounds b = col.bounds;
        b.center = new Vector3(b.center.x, b.center.y, 0f);
        b.size = new Vector3(Mathf.Max(0.05f, b.size.x), Mathf.Max(0.05f, b.size.y), 1f);
        return b;
    }

    private static NavMeshBuildSource MakeBox(Vector3 center, Vector3 size, int area)
    {
        return new NavMeshBuildSource
        {
            shape = NavMeshBuildSourceShape.Box,
            transform = Matrix4x4.Translate(new Vector3(center.x, center.y, 0f)),
            size = new Vector3(Mathf.Max(0.05f, size.x), Mathf.Max(0.05f, size.y), 1f),
            area = area
        };
    }

    private static Bounds ToBakeBounds(Bounds interior)
    {
        Bounds world = interior;
        world.Expand(new Vector3(0.4f, 0.4f, 1f));

        Matrix4x4 worldToLocal = Matrix4x4.TRS(Vector3.zero, BakeRotation, Vector3.one).inverse;
        Vector3 absX = Abs(worldToLocal.MultiplyVector(Vector3.right));
        Vector3 absY = Abs(worldToLocal.MultiplyVector(Vector3.up));
        Vector3 absZ = Abs(worldToLocal.MultiplyVector(Vector3.forward));
        Vector3 center = worldToLocal.MultiplyPoint(world.center);
        Vector3 size = absX * world.size.x + absY * world.size.y + absZ * world.size.z;
        Bounds local = new Bounds(center, size);
        local.Expand(0.05f);
        return local;
    }

    private static Vector3 Abs(Vector3 v)
    {
        return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }

    private static void DestroyTemps(List<Mesh> temps)
    {
        if (temps == null)
            return;

        for (int i = 0; i < temps.Count; i++)
        {
            if (temps[i] == null)
                continue;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                Object.DestroyImmediate(temps[i]);
            else
                Object.Destroy(temps[i]);
#else
            Object.Destroy(temps[i]);
#endif
        }

        temps.Clear();
    }
}
