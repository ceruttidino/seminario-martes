using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Genera el piso ENTERO de una sola vez, al empezar el nivel (estilo Binding of Isaac): todas las rooms, sus
/// conexiones y a donde lleva cada puerta quedan definidas de entrada. Lo unico que cambia durante la partida es lo que
/// el jugador va DESCUBRIENDO (el minimapa solo muestra las rooms visitadas y las que estan pegadas a ellas).
///
/// Reglas del piso:
///   - Una Start, una Boss y una Shop (siempre una sola de cada), y una Challenge si queda alguna sin jugar.
///   - Boss, Shop y Challenge son "callejones sin salida": tienen UNA sola puerta y cuelgan de una room normal. La Boss
///     queda en el extremo mas lejano del Start.
///   - La cantidad de rooms es la que pide el DungeonManager (se sortea entre un minimo y un maximo por piso).
///   - Cada room normal tiene EXACTAMENTE las puertas que la conectan con sus vecinas, y se le asigna un prefab que
///     tiene esas puertas (RoomInformation.availableDoors). Las puertas nunca cambian despues.
///
/// Todo el sorteo sale de una semilla: con la misma semilla se obtiene el mismo piso (sirve para reproducir bugs).
/// </summary>
public class DungeonLayout
{
    private const int MaxAttempts = 80;
    private const int GridLimitX = 7;
    private const int GridLimitY = 6;

    // Una room normal puede tener hasta 3 vecinas durante el crecimiento: queda un lado libre para colgar una room especial.
    private const int MaxNormalNeighbors = 3;

    private static readonly DoorDirection[] AllDirections =
    {
        DoorDirection.Up, DoorDirection.Down, DoorDirection.Left, DoorDirection.Right
    };

    private readonly List<RoomNode> rooms = new List<RoomNode>();
    private readonly Dictionary<Vector2Int, RoomNode> grid = new Dictionary<Vector2Int, RoomNode>();

    private struct SpecialOption
    {
        public RoomNode host;
        public DoorDirection direction;
        public Vector2Int position;
    }

    public RoomNode StartNode { get; private set; }

    /// <summary>Semilla con la que se genero el piso (para loguearla / reproducirlo).</summary>
    public int Seed { get; private set; }

    /// <summary>Cuantos intentos hizo falta para armar un piso valido.</summary>
    public int Attempts { get; private set; }

    /// <summary>Motivo del ultimo intento fallido (vacio si salio bien).</summary>
    public string LastError { get; private set; } = "";

    // ------------------------------------------------------------------ generacion

    /// <summary>
    /// Arma el piso completo. 'totalRooms' es la cantidad TOTAL (incluye Start, Boss, Shop y Challenge).
    /// Devuelve false solo si falta configuracion (sin prefabs, sin boss...): en ese caso LastError dice por que.
    /// </summary>
    public bool Generate(RoomInformation startRoom, List<RoomInformation> normalRooms, RoomInformation shopRoom,
        RoomInformation bossRoom, List<RoomInformation> challengeRooms, int totalRooms, int seed)
    {
        Seed = seed;
        Attempts = 0;
        LastError = "";

        if (startRoom == null || startRoom.prefab == null)
        {
            LastError = "La Start Room no tiene prefab.";
            return false;
        }

        if (bossRoom == null || bossRoom.prefab == null)
        {
            LastError = "La Boss Room no tiene prefab.";
            return false;
        }

        List<RoomInformation> pool = FilterNormalPool(normalRooms);
        if (pool.Count == 0)
        {
            LastError = "No hay rooms normales validas (con prefab y puertas) para este piso.";
            return false;
        }

        bool wantShop = shopRoom != null && shopRoom.prefab != null;
        List<RoomInformation> challengePool = FilterChallengePool(challengeRooms);

        System.Random rng = new System.Random(seed);

        // Start + Boss siempre; Shop y Challenge solo si hay con que armarlas. El resto son rooms normales.
        int fixedRooms = 2 + (wantShop ? 1 : 0) + (challengePool.Count > 0 ? 1 : 0);
        int normalTarget = Mathf.Clamp(totalRooms - fixedRooms, 2, 16);

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            // Si despues de muchos intentos no entra, se achica un poco el piso (nunca deberia hacer falta).
            if (attempt > 30 && attempt % 10 == 1)
                normalTarget = Mathf.Max(2, normalTarget - 1);

            if (TryGenerate(rng, startRoom, pool, shopRoom, wantShop, bossRoom, challengePool, normalTarget))
            {
                Attempts = attempt;
                LastError = "";
                return true;
            }
        }

        Attempts = MaxAttempts;
        return false;
    }

    /// <summary>
    /// Ultimo recurso (solo si Generate falla): Start conectada directo a la Boss. Evita que el juego quede sin piso.
    /// </summary>
    public bool GenerateFallback(RoomInformation startRoom, RoomInformation bossRoom)
    {
        rooms.Clear();
        grid.Clear();

        if (startRoom == null || startRoom.prefab == null)
            return false;

        StartNode = new RoomNode("Start", startRoom) { gridPosition = Vector2Int.zero };
        AddNode(StartNode);

        if (bossRoom != null && bossRoom.prefab != null)
        {
            RoomNode boss = new RoomNode("Boss", bossRoom) { gridPosition = Vector2Int.up };
            AddNode(boss);
            Link(StartNode, DoorDirection.Up, boss);
        }

        ComputeDepths();
        return true;
    }

    private bool TryGenerate(System.Random rng, RoomInformation startRoom, List<RoomInformation> pool,
        RoomInformation shopRoom, bool wantShop, RoomInformation bossRoom, List<RoomInformation> challengePool,
        int normalTarget)
    {
        rooms.Clear();
        grid.Clear();

        StartNode = new RoomNode("Start", startRoom) { gridPosition = Vector2Int.zero };
        AddNode(StartNode);

        // 1) Las rooms normales: el "esqueleto" del piso.
        List<RoomNode> normals = new List<RoomNode>();
        if (!GrowNormals(normalTarget, rng, normals))
        {
            LastError = "No se pudieron ubicar todas las rooms normales.";
            return false;
        }

        ComputeDepths();

        // 2) Las rooms especiales cuelgan de una normal distinta cada una (callejones sin salida).
        HashSet<RoomNode> usedHosts = new HashSet<RoomNode>();

        if (!PlaceSpecial(RoomType.Boss, bossRoom, normals, usedHosts, rng))
        {
            LastError = "No se encontro lugar para la Boss Room.";
            return false;
        }

        if (wantShop && !PlaceSpecial(RoomType.Shop, shopRoom, normals, usedHosts, rng))
        {
            LastError = "No se encontro lugar para la Shop.";
            return false;
        }

        if (challengePool.Count > 0)
        {
            RoomInformation challenge = challengePool[rng.Next(challengePool.Count)];
            if (!PlaceSpecial(RoomType.Challenge, challenge, normals, usedHosts, rng))
            {
                LastError = "No se encontro lugar para la Challenge Room.";
                return false;
            }
        }

        // 3) Con las conexiones ya definidas, a cada room normal se le asigna un prefab que tenga esas puertas.
        if (!AssignNormalRooms(normals, pool, rng))
        {
            LastError = "Alguna room normal no encontro un prefab con las puertas necesarias.";
            return false;
        }

        ComputeDepths();

        return Validate();
    }

    // ------------------------------------------------------------------ rooms normales

    private bool GrowNormals(int target, System.Random rng, List<RoomNode> normals)
    {
        // Cuantas puertas tiene el Start: 2 o 3 en un piso normal (dos caminos para elegir), 1 si el piso es chiquito.
        int startDoors = target >= 4 ? rng.Next(2, 4) : 1;

        List<RoomNode> growable = new List<RoomNode> { StartNode };
        int guard = 0;

        while (normals.Count < target && guard++ < 800)
        {
            RoomNode host = PickGrowthHost(growable, startDoors, rng);
            if (host == null)
                return false;

            int limit = host == StartNode ? startDoors : MaxNormalNeighbors;
            if (host.GetNeighborsCount() >= limit || !TryAttachNormal(host, rng, out RoomNode created))
            {
                // Esta room ya no puede crecer.
                growable.Remove(host);
                if (growable.Count == 0)
                    return false;
                continue;
            }

            normals.Add(created);
            growable.Add(created);
        }

        return normals.Count >= target;
    }

    private RoomNode PickGrowthHost(List<RoomNode> growable, int startDoors, System.Random rng)
    {
        if (growable.Count == 0)
            return null;

        // Primero el Start recibe sus puertas, asi nunca queda con un unico camino en un piso grande.
        if (growable.Contains(StartNode) && StartNode.GetNeighborsCount() < startDoors)
            return StartNode;

        // Despues el Start ya no crece: el resto del piso sale de las rooms normales.
        List<RoomNode> candidates = new List<RoomNode>();
        foreach (RoomNode node in growable)
        {
            if (node != StartNode)
                candidates.Add(node);
        }

        if (candidates.Count == 0)
            return null;

        // La mayoria de las veces se extiende una de las ultimas rooms (caminos largos); a veces una cualquiera
        // (ramas laterales). Asi salen pisos con forma de camino con bifurcaciones, no un bloque.
        if (candidates.Count > 1 && rng.NextDouble() < 0.6)
        {
            int recent = Mathf.Min(2, candidates.Count);
            return candidates[candidates.Count - 1 - rng.Next(recent)];
        }

        return candidates[rng.Next(candidates.Count)];
    }

    private bool TryAttachNormal(RoomNode host, System.Random rng, out RoomNode created)
    {
        created = null;

        List<DoorDirection> directions = new List<DoorDirection>(AllDirections);
        Shuffle(directions, rng);

        foreach (DoorDirection direction in directions)
        {
            if (host.HasNeighbor(direction))
                continue;

            Vector2Int position = host.gridPosition + Offset(direction);
            if (grid.ContainsKey(position) || !InBounds(position))
                continue;

            // La room nueva solo puede tocar a la que la origina: asi el piso no forma bloques ni puertas "de mas".
            if (CountOccupiedNeighbors(position) != 1)
                continue;

            RoomNode node = new RoomNode("Room_" + rooms.Count, null) { gridPosition = position };
            AddNode(node);
            Link(host, direction, node);

            created = node;
            return true;
        }

        return false;
    }

    // ------------------------------------------------------------------ rooms especiales

    private bool PlaceSpecial(RoomType type, RoomInformation info, List<RoomNode> normals, HashSet<RoomNode> usedHosts,
        System.Random rng)
    {
        if (info == null)
            return false;

        List<SpecialOption> options = new List<SpecialOption>();

        foreach (RoomNode host in normals)
        {
            if (usedHosts.Contains(host))
                continue;

            foreach (DoorDirection direction in AllDirections)
            {
                if (host.HasNeighbor(direction))
                    continue;

                // La especial tiene una sola puerta (la que da a su host): ese prefab tiene que tenerla.
                if (!info.HasDoor(direction.Opposite()))
                    continue;

                Vector2Int position = host.gridPosition + Offset(direction);
                if (grid.ContainsKey(position) || !InBounds(position))
                    continue;

                // Tiene que quedar aislada: la unica room vecina es su host (nunca pegada a otra especial).
                if (CountOccupiedNeighbors(position) != 1)
                    continue;

                options.Add(new SpecialOption { host = host, direction = direction, position = position });
            }
        }

        if (options.Count == 0)
            return false;

        SpecialOption chosen = ChooseOption(type, options, rng);

        RoomNode node = new RoomNode(type + "_" + rooms.Count, info) { gridPosition = chosen.position };
        AddNode(node);
        Link(chosen.host, chosen.direction, node);
        node.depth = chosen.host.depth + 1;

        usedHosts.Add(chosen.host);
        return true;
    }

    private SpecialOption ChooseOption(RoomType type, List<SpecialOption> options, System.Random rng)
    {
        if (type == RoomType.Boss)
        {
            // La Boss va en el extremo del piso: se elige entre las rooms mas lejanas al Start (la capa mas profunda y,
            // si hay una sola, tambien la anterior), asi siempre hay que recorrer el piso para llegar.
            int deepest = 0;
            foreach (SpecialOption option in options)
                deepest = Mathf.Max(deepest, option.host.depth);

            List<RoomNode> hosts = new List<RoomNode>();
            foreach (SpecialOption option in options)
            {
                if (option.host.depth == deepest && !hosts.Contains(option.host))
                    hosts.Add(option.host);
            }

            if (hosts.Count < 2)
            {
                foreach (SpecialOption option in options)
                {
                    if (option.host.depth == deepest - 1 && !hosts.Contains(option.host))
                        hosts.Add(option.host);
                }
            }

            RoomNode host = hosts[rng.Next(hosts.Count)];
            List<SpecialOption> ofHost = options.FindAll(o => o.host == host);
            return ofHost[rng.Next(ofHost.Count)];
        }

        if (type == RoomType.Shop)
        {
            // La Shop no va pegada al Start: para llegar hay que explorar un poco (si el piso lo permite).
            List<SpecialOption> farther = options.FindAll(o => o.host.depth >= 2);
            List<SpecialOption> source = farther.Count > 0 ? farther : options;
            return source[rng.Next(source.Count)];
        }

        return options[rng.Next(options.Count)];
    }

    // ------------------------------------------------------------------ asignacion de prefabs

    private bool AssignNormalRooms(List<RoomNode> normals, List<RoomInformation> pool, System.Random rng)
    {
        // Cada prefab se usa una vez por piso (como antes). Si se agotan los que sirven se repite alguno.
        List<RoomInformation> remaining = new List<RoomInformation>(pool);

        // Primero las que mas puertas necesitan (son las que tienen menos prefabs posibles).
        List<RoomNode> order = new List<RoomNode>(normals);
        Shuffle(order, rng);
        order.Sort((a, b) => b.GetNeighborsCount().CompareTo(a.GetNeighborsCount()));

        foreach (RoomNode node in order)
        {
            RoomInformation chosen = PickRoomFor(node, remaining, pool, rng);
            if (chosen == null)
                return false;

            node.information = chosen;
            remaining.Remove(chosen);
        }

        return true;
    }

    private RoomInformation PickRoomFor(RoomNode node, List<RoomInformation> remaining, List<RoomInformation> pool,
        System.Random rng)
    {
        List<RoomInformation> candidates = remaining.FindAll(info => SupportsAllDoors(info, node));
        if (candidates.Count == 0)
            candidates = pool.FindAll(info => SupportsAllDoors(info, node));

        if (candidates.Count == 0)
            return null;

        // Los prefabs pensados justo para esas puertas (pasillo, T, esquina...) tienen mas chance que uno de 4 puertas.
        int neighbors = node.GetNeighborsCount();
        int totalWeight = 0;
        int[] weights = new int[candidates.Count];
        for (int i = 0; i < candidates.Count; i++)
        {
            weights[i] = candidates[i].availableDoors.Count == neighbors ? 3 : 1;
            totalWeight += weights[i];
        }

        int roll = rng.Next(totalWeight);
        for (int i = 0; i < candidates.Count; i++)
        {
            roll -= weights[i];
            if (roll < 0)
                return candidates[i];
        }

        return candidates[candidates.Count - 1];
    }

    private static bool SupportsAllDoors(RoomInformation info, RoomNode node)
    {
        foreach (KeyValuePair<DoorDirection, RoomNode> pair in node.neighboors)
        {
            if (pair.Value != null && !info.HasDoor(pair.Key))
                return false;
        }

        return true;
    }

    // ------------------------------------------------------------------ validacion

    private bool Validate()
    {
        int bosses = 0;
        int shops = 0;
        int challenges = 0;

        foreach (RoomNode node in rooms)
        {
            if (node.information == null || node.information.prefab == null)
            {
                LastError = "La room " + node.name + " quedo sin prefab.";
                return false;
            }

            switch (node.information.type)
            {
                case RoomType.Boss: bosses++; break;
                case RoomType.Shop: shops++; break;
                case RoomType.Challenge: challenges++; break;
            }

            bool special = IsSpecial(node);
            int neighbors = node.GetNeighborsCount();

            if (neighbors == 0 || (special && neighbors != 1))
            {
                LastError = "La room " + node.name + " tiene una cantidad de puertas invalida (" + neighbors + ").";
                return false;
            }

            foreach (KeyValuePair<DoorDirection, RoomNode> pair in node.neighboors)
            {
                RoomNode other = pair.Value;
                if (other == null)
                    continue;

                // Puerta de ida y de vuelta, y las dos rooms tienen que tener esa puerta en su prefab.
                if (other.GetNeighbor(pair.Key.Opposite()) != node ||
                    !node.information.HasDoor(pair.Key) ||
                    !other.information.HasDoor(pair.Key.Opposite()))
                {
                    LastError = "Conexion inconsistente entre " + node.name + " y " + other.name + ".";
                    return false;
                }

                if (special && IsSpecial(other))
                {
                    LastError = "Dos rooms especiales conectadas entre si.";
                    return false;
                }
            }
        }

        if (bosses != 1 || shops > 1 || challenges > 1)
        {
            LastError = "Cantidad de rooms especiales invalida (boss " + bosses + ", shop " + shops + ", challenge " + challenges + ").";
            return false;
        }

        // Todas las rooms tienen que ser alcanzables desde el Start.
        HashSet<RoomNode> reached = new HashSet<RoomNode>();
        Queue<RoomNode> queue = new Queue<RoomNode>();
        queue.Enqueue(StartNode);
        reached.Add(StartNode);

        while (queue.Count > 0)
        {
            RoomNode current = queue.Dequeue();
            foreach (RoomNode next in current.neighboors.Values)
            {
                if (next != null && reached.Add(next))
                    queue.Enqueue(next);
            }
        }

        if (reached.Count != rooms.Count)
        {
            LastError = "Hay rooms inalcanzables desde el Start.";
            return false;
        }

        return true;
    }

    private static bool IsSpecial(RoomNode node)
    {
        if (node.information == null)
            return false;

        RoomType type = node.information.type;
        return type == RoomType.Shop || type == RoomType.Boss || type == RoomType.Challenge;
    }

    // ------------------------------------------------------------------ utilidades

    private void AddNode(RoomNode node)
    {
        rooms.Add(node);
        grid[node.gridPosition] = node;
    }

    private static void Link(RoomNode from, DoorDirection direction, RoomNode to)
    {
        from.SetNeighbor(direction, to);
        to.SetNeighbor(direction.Opposite(), from);
    }

    private int CountOccupiedNeighbors(Vector2Int position)
    {
        int count = 0;
        foreach (DoorDirection direction in AllDirections)
        {
            if (grid.ContainsKey(position + Offset(direction)))
                count++;
        }

        return count;
    }

    private static bool InBounds(Vector2Int position)
    {
        return Mathf.Abs(position.x) <= GridLimitX && Mathf.Abs(position.y) <= GridLimitY;
    }

    private static Vector2Int Offset(DoorDirection direction)
    {
        switch (direction)
        {
            case DoorDirection.Up: return Vector2Int.up;
            case DoorDirection.Down: return Vector2Int.down;
            case DoorDirection.Right: return Vector2Int.right;
            default: return Vector2Int.left;
        }
    }

    /// <summary>Distancia (en rooms) desde el Start hasta cada room. Se usa para ubicar la Boss lejos y la Shop no pegada.</summary>
    private void ComputeDepths()
    {
        foreach (RoomNode node in rooms)
            node.depth = -1;

        Queue<RoomNode> queue = new Queue<RoomNode>();
        StartNode.depth = 0;
        queue.Enqueue(StartNode);

        while (queue.Count > 0)
        {
            RoomNode current = queue.Dequeue();
            foreach (RoomNode next in current.neighboors.Values)
            {
                if (next != null && next.depth < 0)
                {
                    next.depth = current.depth + 1;
                    queue.Enqueue(next);
                }
            }
        }
    }

    private static void Shuffle<T>(IList<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    private static List<RoomInformation> FilterNormalPool(List<RoomInformation> source)
    {
        List<RoomInformation> pool = new List<RoomInformation>();
        if (source == null)
            return pool;

        // Se conservan los repetidos a proposito: una misma room cargada varias veces puede usarse varias veces.
        foreach (RoomInformation info in source)
        {
            if (info != null && info.prefab != null && info.availableDoors != null && info.availableDoors.Count > 0)
                pool.Add(info);
        }

        return pool;
    }

    private static List<RoomInformation> FilterChallengePool(List<RoomInformation> source)
    {
        List<RoomInformation> pool = new List<RoomInformation>();
        if (source == null)
            return pool;

        foreach (RoomInformation info in source)
        {
            if (info == null || info.prefab == null)
                continue;

            // Un challenge ya jugado en este piso no vuelve a aparecer.
            if (!string.IsNullOrEmpty(info.challengeId) && ChallengeRunState.WasCompleted(info.challengeId))
                continue;

            pool.Add(info);
        }

        return pool;
    }

    // ------------------------------------------------------------------ consulta

    public List<RoomNode> GetAllRooms()
    {
        return rooms;
    }

    public static DoorDirection GetOppositeDirection(DoorDirection direction)
    {
        return direction.Opposite();
    }

    /// <summary>Dibujo del piso en texto para el log: S = Start, o = normal, $ = Shop, B = Boss, C = Challenge.</summary>
    public string ToAscii()
    {
        if (rooms.Count == 0)
            return "(piso vacio)";

        int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
        foreach (RoomNode node in rooms)
        {
            minX = Mathf.Min(minX, node.gridPosition.x);
            maxX = Mathf.Max(maxX, node.gridPosition.x);
            minY = Mathf.Min(minY, node.gridPosition.y);
            maxY = Mathf.Max(maxY, node.gridPosition.y);
        }

        StringBuilder text = new StringBuilder();
        for (int y = maxY; y >= minY; y--)
        {
            StringBuilder row = new StringBuilder();
            StringBuilder links = new StringBuilder();

            for (int x = minX; x <= maxX; x++)
            {
                if (grid.TryGetValue(new Vector2Int(x, y), out RoomNode node))
                {
                    row.Append(Symbol(node));
                    row.Append(node.HasNeighbor(DoorDirection.Right) ? '-' : ' ');
                    links.Append(node.HasNeighbor(DoorDirection.Down) ? '|' : ' ');
                    links.Append(' ');
                }
                else
                {
                    row.Append("  ");
                    links.Append("  ");
                }
            }

            text.AppendLine(row.ToString().TrimEnd());
            if (y > minY)
                text.AppendLine(links.ToString().TrimEnd());
        }

        return text.ToString();
    }

    private static char Symbol(RoomNode node)
    {
        if (node == null || node.information == null)
            return '?';

        switch (node.information.type)
        {
            case RoomType.Start: return 'S';
            case RoomType.Shop: return '$';
            case RoomType.Boss: return 'B';
            case RoomType.Challenge: return 'C';
            default: return 'o';
        }
    }
}
