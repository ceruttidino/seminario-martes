using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RoomPathGrid : MonoBehaviour
{
    [SerializeField] private float cellSize = 0.4f;
    [SerializeField] private float probeRadius = 0.2f;
    [SerializeField] private LayerMask blockedLayers = 1089; // Default + Trash + Wall

    private bool[,] walkable;
    private Vector2 origin;
    private int width;
    private int height;
    private bool dirty = true;
    private float nextRebuildTime;
    private ContactFilter2D blockedFilter;
    private readonly Collider2D[] overlapBuffer = new Collider2D[16];
    private readonly RaycastHit2D[] lineBuffer = new RaycastHit2D[8];
    private Tilemap floorMap;
    private Tilemap voidMap;
    private bool tilemapsCached;
    private readonly List<Vector2Int> open = new List<Vector2Int>(64);
    private readonly Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>(64);
    private readonly Dictionary<Vector2Int, int> cost = new Dictionary<Vector2Int, int>(64);
    private readonly List<Vector2Int> cellPath = new List<Vector2Int>(32);

    private static readonly Vector2Int[] Dirs =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
    };

    public static RoomPathGrid For(Transform t)
    {
        if (t == null) return null;

        RoomInstance room = t.GetComponentInParent<RoomInstance>();
        if (room == null) return null;

        RoomPathGrid grid = room.GetComponent<RoomPathGrid>();
        if (grid == null)
            grid = room.gameObject.AddComponent<RoomPathGrid>();

        grid.EnsureReady();
        return grid;
    }

    public void Invalidate()
    {
        dirty = true;
    }

    private void Awake()
    {
        blockedLayers = LayerMask.GetMask("Default", "Trash", "Wall");
        if (blockedLayers.value == 0)
            blockedLayers = 1089;

        blockedFilter = new ContactFilter2D();
        blockedFilter.useTriggers = false;
        blockedFilter.SetLayerMask(blockedLayers);
        CacheTilemaps();
    }

    private void CacheTilemaps()
    {
        if (tilemapsCached)
            return;

        tilemapsCached = true;
        Tilemap[] maps = GetComponentsInChildren<Tilemap>(true);
        for (int i = 0; i < maps.Length; i++)
        {
            string name = maps[i].gameObject.name;
            if (NameContains(name, "void") || NameContains(name, "vacio") || NameContains(name, "vacío"))
                voidMap = maps[i];
            else if (floorMap == null && NameContains(name, "floor"))
                floorMap = maps[i];
        }
    }

    public bool IsWalkableWorld(Vector2 world)
    {
        EnsureReady();
        if (IsTerrainHole(world) || IsBlocked(world))
            return false;

        Vector2Int cell = WorldToCell(world);
        if (!InBounds(cell) || walkable == null)
            return false;

        return walkable[cell.x, cell.y];
    }

    private void EnsureReady()
    {
        if (!dirty && walkable != null) return;
        if (Time.unscaledTime < nextRebuildTime && walkable != null) return;

        Rebuild();
        dirty = false;
        nextRebuildTime = Time.unscaledTime + 0.25f;
    }

    private void Rebuild()
    {
        CacheTilemaps();
        Bounds bounds = ComputeBounds();
        origin = new Vector2(bounds.min.x, bounds.min.y);
        width = Mathf.Clamp(Mathf.CeilToInt(bounds.size.x / cellSize), 4, 64);
        height = Mathf.Clamp(Mathf.CeilToInt(bounds.size.y / cellSize), 4, 48);
        walkable = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 world = CellToWorld(x, y);
                walkable[x, y] = !IsTerrainHole(world) && !IsBlocked(world);
            }
        }
    }

    private Bounds ComputeBounds()
    {
        Bounds bounds = new Bounds(transform.position, new Vector3(16f, 9f, 1f));
        bool initialized = false;

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D col = colliders[i];
            if (col == null || !col.enabled) continue;
            if (col.gameObject.layer != 10) continue;

            if (!initialized)
            {
                bounds = col.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(col.bounds);
            }
        }

        bounds.Expand(-0.35f);
        return bounds;
    }

    private bool IsIgnoredBlocker(Collider2D hit)
    {
        if (hit == null) return true;
        if (hit.isTrigger) return true;
        if (hit.GetComponent<EnemyHealth>() != null) return true;
        if (hit.GetComponent<RatBody>() != null) return true;
        if (hit.GetComponent<LootPickup>() != null) return true;
        if (hit.GetComponent<UpgradePickup>() != null) return true;
        if (hit.GetComponent<SlimeTile>() != null) return true;
        if (hit.GetComponent<BloodPool>() != null) return true;
        if (hit.GetComponent<DiggingSpot>() != null) return true;
        if (hit.GetComponent<BaseTrap>() != null) return true;
        if (hit.CompareTag("Player")) return true;
        if (hit.GetComponent<Supercontainer>() != null) return true;
        if (hit.GetComponent<TurtleShell>() != null) return true;
        if (NameContains(hit.gameObject.name, "floor")) return true;
        return false;
    }

    private bool IsTerrainHole(Vector2 world)
    {
        if (voidMap != null && voidMap.HasTile(voidMap.WorldToCell(world)))
            return true;

        if (floorMap != null && !floorMap.HasTile(floorMap.WorldToCell(world)))
            return true;

        return false;
    }

    private bool IsBlocked(Vector2 world)
    {
        int count = Physics2D.OverlapCircle(world, probeRadius, blockedFilter, overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            if (IsIgnoredBlocker(overlapBuffer[i])) continue;
            return true;
        }

        return false;
    }

    private static bool NameContains(string name, string token)
    {
        return !string.IsNullOrEmpty(name) && name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public bool HasClearLine(Vector2 from, Vector2 to, float radius)
    {
        Vector2 delta = to - from;
        float distance = delta.magnitude;
        if (distance <= 0.05f) return true;

        int samples = Mathf.Max(2, Mathf.CeilToInt(distance / Mathf.Max(0.2f, cellSize)));
        for (int i = 0; i <= samples; i++)
        {
            Vector2 point = Vector2.Lerp(from, to, i / (float)samples);
            if (IsTerrainHole(point) || IsBlocked(point))
                return false;
        }

        int hits = Physics2D.CircleCast(from, radius, delta / distance, blockedFilter, lineBuffer, distance);
        for (int i = 0; i < hits; i++)
        {
            if (IsIgnoredBlocker(lineBuffer[i].collider)) continue;
            return false;
        }

        return true;
    }

    public bool TryGetLookAhead(Vector2 from, Vector2 goal, float lookAhead, out Vector2 nextStep)
    {
        EnsureReady();
        nextStep = goal;

        if (walkable == null) return false;

        Vector2Int start = WorldToCell(from);
        Vector2Int end = WorldToCell(goal);

        if (!InBounds(start)) return false;
        if (!InBounds(end))
            end = ClampToBounds(end);

        if (!walkable[end.x, end.y])
            end = FindNearestWalkable(end);

        if (!walkable[start.x, start.y])
        {
            start = FindNearestWalkable(start);
            nextStep = CellToWorld(start.x, start.y);
            return true;
        }

        if (start == end)
        {
            nextStep = goal;
            return true;
        }

        if (!BuildPath(start, end))
            return false;

        nextStep = PointAlongPath(from, lookAhead);
        return true;
    }

    private bool BuildPath(Vector2Int start, Vector2Int goal)
    {
        open.Clear();
        cameFrom.Clear();
        cost.Clear();
        cellPath.Clear();

        open.Add(start);
        cost[start] = 0;

        int maxNodes = width * height;
        bool found = false;

        while (open.Count > 0 && cameFrom.Count < maxNodes)
        {
            int best = 0;
            int bestScore = int.MaxValue;
            for (int i = 0; i < open.Count; i++)
            {
                int g = cost[open[i]];
                int h = Mathf.Abs(open[i].x - goal.x) + Mathf.Abs(open[i].y - goal.y);
                int score = g + h;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }

            Vector2Int current = open[best];
            open.RemoveAt(best);

            if (current == goal)
            {
                found = true;
                break;
            }

            for (int i = 0; i < Dirs.Length; i++)
            {
                Vector2Int next = current + Dirs[i];
                if (!InBounds(next) || !walkable[next.x, next.y]) continue;
                if (Mathf.Abs(Dirs[i].x) + Mathf.Abs(Dirs[i].y) == 2)
                {
                    if (!walkable[current.x, next.y] || !walkable[next.x, current.y])
                        continue;
                }

                int stepCost = (Mathf.Abs(Dirs[i].x) + Mathf.Abs(Dirs[i].y) == 2) ? 14 : 10;
                int newCost = cost[current] + stepCost;
                if (cost.TryGetValue(next, out int old) && newCost >= old)
                    continue;

                cost[next] = newCost;
                cameFrom[next] = current;
                if (!open.Contains(next))
                    open.Add(next);
            }
        }

        if (!found) return false;

        Vector2Int walk = goal;
        cellPath.Add(walk);
        while (cameFrom.TryGetValue(walk, out Vector2Int parent))
        {
            cellPath.Add(parent);
            walk = parent;
            if (walk == start) break;
        }

        cellPath.Reverse();
        return cellPath.Count > 0;
    }

    private Vector2 PointAlongPath(Vector2 from, float lookAhead)
    {
        if (cellPath.Count == 0)
            return from;

        float traveled = 0f;
        Vector2 previous = from;

        for (int i = 0; i < cellPath.Count; i++)
        {
            Vector2 point = CellToWorld(cellPath[i].x, cellPath[i].y);
            float segment = Vector2.Distance(previous, point);
            if (traveled + segment >= lookAhead)
            {
                float remain = lookAhead - traveled;
                return previous + (point - previous).normalized * remain;
            }

            traveled += segment;
            previous = point;
        }

        Vector2Int last = cellPath[cellPath.Count - 1];
        return CellToWorld(last.x, last.y);
    }

    private Vector2Int FindNearestWalkable(Vector2Int cell)
    {
        if (InBounds(cell) && walkable[cell.x, cell.y])
            return cell;

        for (int radius = 1; radius <= 8; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    Vector2Int c = new Vector2Int(cell.x + x, cell.y + y);
                    if (InBounds(c) && walkable[c.x, c.y])
                        return c;
                }
            }
        }

        return ClampToBounds(cell);
    }

    private Vector2Int WorldToCell(Vector2 world)
    {
        int x = Mathf.FloorToInt((world.x - origin.x) / cellSize);
        int y = Mathf.FloorToInt((world.y - origin.y) / cellSize);
        return new Vector2Int(x, y);
    }

    private Vector2 CellToWorld(int x, int y)
    {
        return origin + new Vector2((x + 0.5f) * cellSize, (y + 0.5f) * cellSize);
    }

    private bool InBounds(Vector2Int cell)
    {
        return cell.x >= 0 && cell.y >= 0 && cell.x < width && cell.y < height;
    }

    private Vector2Int ClampToBounds(Vector2Int cell)
    {
        return new Vector2Int(Mathf.Clamp(cell.x, 0, width - 1), Mathf.Clamp(cell.y, 0, height - 1));
    }

    public bool TryGetWanderPoint(Vector2 from, Vector2 avoid, float minDistanceFromAvoid, out Vector2 point)
    {
        EnsureReady();
        point = from;

        if (walkable == null)
            return false;

        Vector2 best = from;
        float bestScore = float.MinValue;
        bool found = false;

        int samples = Mathf.Min(48, width * height);
        for (int i = 0; i < samples; i++)
        {
            int x = Random.Range(0, width);
            int y = Random.Range(0, height);
            if (!walkable[x, y])
                continue;

            Vector2 candidate = CellToWorld(x, y);
            float fromHere = Vector2.Distance(from, candidate);
            if (fromHere < 1.1f)
                continue;

            float fromAvoid = Vector2.Distance(avoid, candidate);
            if (fromAvoid < minDistanceFromAvoid)
                continue;

            float score = fromAvoid * 1.25f + fromHere;
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
                found = true;
            }
        }

        if (found)
        {
            point = best;
            return true;
        }

        Vector2Int start = FindNearestWalkable(WorldToCell(from));
        if (InBounds(start) && walkable[start.x, start.y])
        {
            point = CellToWorld(start.x, start.y);
            return true;
        }

        return false;
    }
}
