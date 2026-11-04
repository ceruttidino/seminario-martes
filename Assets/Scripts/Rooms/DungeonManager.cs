using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// CAMBIADO (archivo completo): un solo sistema de rooms.
// Todo lo que sale en cada nivel se define en los DungeonFloorConfig de la lista "Floors".
// Se sacaron: las listas generales de rooms (start, normales, shop, bosses, boss final, challenges, buff),
// el filtro por 'level', maxFloor y la referencia al componente DungeonProgression.
public class DungeonManager : MonoBehaviour
{
    public static DungeonManager Instance;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform roomParent;
    [SerializeField] private ScreenFader screenFader;

    [Header("Niveles")] // CAMBIADO: única configuración de rooms
    [Tooltip("Un FloorConfig por nivel, en orden (elemento 0 = nivel 1). La cantidad de elementos define cuántos niveles tiene el juego.")]
    [SerializeField] private List<DungeonFloorConfig> floors = new List<DungeonFloorConfig>();

    [Header("Transition")]
    [SerializeField] private float transitionDelay = 0.5f;

    [Header("UI")]
    [SerializeField] private MinimapUI minimapUI;

    private readonly DungeonProgression progression = new DungeonProgression(); // CAMBIADO: ya no es un componente

    private DungeonLayout dungeonLayout;
    private RoomNode currentNode;
    private RoomInstance currentRoomInstance;
    private bool isTransitioning;
    private int currentFloor = 1;
    private bool challengeSpawned;
    private bool buffRoomSpawned;

    private DoorDirection? lastEntryDirection;

    // Bosses que ya salieron en esta partida: no se repiten en los niveles siguientes (si hay alternativa)
    private readonly List<RoomInformation> usedBosses = new List<RoomInformation>();

    public int CurrentFloor => currentFloor;
    public int MaxFloor => Mathf.Max(1, floors != null ? floors.Count : 0); // CAMBIADO: la cantidad de niveles manda
    public bool IsFinalFloor => currentFloor >= MaxFloor;
    public RoomInstance CurrentRoom => currentRoomInstance;

    public DungeonFloorConfig CurrentFloorConfig =>
        floors != null && currentFloor >= 1 && currentFloor <= floors.Count ? floors[currentFloor - 1] : null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuffPool.Reset();
        ChallengeRunState.Reset();
        DungeonNavMesh.Ensure();
    }

    private void Start()
    {
        InitializeDungeon();
    }

    private void InitializeDungeon()
    {
        BuildNewDungeon();

        if (minimapUI != null)
            minimapUI.Initialize(this);
    }

    // ---------------- GENERACIÓN DEL NIVEL ----------------

    private void BuildNewDungeon()
    {
        challengeSpawned = false;
        buffRoomSpawned = false;
        ChallengeRunState.Reset();

        DungeonFloorConfig config = CurrentFloorConfig;
        if (config == null)
        {
            Debug.LogError($"[DungeonManager] Falta el FloorConfig del nivel {currentFloor} en la lista 'Floors'.", this);
            return;
        }

        if (!config.Validate(out string problems))
            Debug.LogError($"[DungeonManager] '{config.name}' tiene problemas:\n{problems}", config);

        if (config.startRoom == null)
            return;

        progression.Configure(config.roomsBeforeShop, config.roomsBeforeBoss, config.shopRoom != null, config.extraRoomsAfterBoss); // CAMBIADO: + tope después del boss

        RoomInformation boss = PickBoss(config);

        dungeonLayout = new DungeonLayout();
        dungeonLayout.Build(config.startRoom, config.normalRooms, config.shopRoom, boss, config.challengeRooms, config.buffRoom);

        currentNode = dungeonLayout.StartNode;
        currentNode.isCurrentRoom = true;

        RoomNode firstRoom = CreateFirstRoom(config.startRoom);

        EnterRoomSafely(currentNode, null);

        // La primera room ya deja colgadas Shop, Boss, Challenge y Buff si hay puerta libre.
        if (firstRoom != null)
            GenerateConnections(firstRoom);

        if (currentNode.spawnedInstance != null)
            currentNode.spawnedInstance.ConfigureDoors(currentNode);
    }

    // CAMBIADO: antes la primera room se creaba siempre por arriba. Ahora usa Up si la start room
    // la tiene y si no, cualquier otra puerta de la start room.
    private RoomNode CreateFirstRoom(RoomInformation startRoom)
    {
        List<DoorDirection> order = new List<DoorDirection> { DoorDirection.Up };
        foreach (DoorDirection door in startRoom.Doors)
        {
            if (!order.Contains(door))
                order.Add(door);
        }

        foreach (DoorDirection dir in order)
        {
            if (!startRoom.HasDoor(dir))
                continue;

            RoomNode node = dungeonLayout.CreateNextRoom(currentNode, dir, RoomType.Normal, out bool createdNew);
            if (node == null)
                continue;

            if (createdNew)
                progression.RegisterGeneratedRoom(RoomType.Normal);

            return node;
        }

        Debug.LogError($"[DungeonManager] No se pudo conectar ninguna room normal a la start room '{startRoom.ID}'. ¿Las rooms normales tienen la puerta opuesta?", this);
        return null;
    }

    // CAMBIADO: un solo método para elegir boss (antes había boss por defecto, pool y boss final aparte).
    private RoomInformation PickBoss(DungeonFloorConfig config)
    {
        List<RoomInformation> pool = new List<RoomInformation>();
        foreach (RoomInformation info in config.bossRooms)
        {
            if (info != null && info.prefab != null && !pool.Contains(info))
                pool.Add(info);
        }

        if (pool.Count == 0)
            return null;

        List<RoomInformation> available = pool.FindAll(info => !usedBosses.Contains(info));
        if (available.Count == 0)
            available = pool;

        RoomInformation chosen = available[Random.Range(0, available.Count)];
        if (!usedBosses.Contains(chosen))
            usedBosses.Add(chosen);

        return chosen;
    }

    // BuffPool no se resetea: los power-ups ya usados no vuelven a salir en el piso siguiente.
    public void StartNextFloor()
    {
        if (IsFinalFloor)
            return;

        if (isTransitioning)
            return;

        StartCoroutine(NextFloorRoutine());
    }

    private IEnumerator NextFloorRoutine()
    {
        isTransitioning = true;

        if (GamePause.IsPaused)
            GamePause.SetPaused(false);

        if (screenFader != null)
            yield return StartCoroutine(screenFader.FadeOutOver(0.9f));

        try
        {
            DestroyCurrentDungeon();
            UpgradeRegistry.ClearReservations();

            currentFloor++;

            BuildNewDungeon();

            if (minimapUI != null)
                minimapUI.Initialize(this);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception, this);
        }

        if (screenFader != null)
        {
            yield return StartCoroutine(screenFader.HoldBlack(0.7f));
            yield return StartCoroutine(screenFader.FadeInOver(0.95f));
        }

        isTransitioning = false;
    }

    private void DestroyCurrentDungeon()
    {
        if (dungeonLayout == null)
            return;

        foreach (RoomNode node in dungeonLayout.GetAllRooms())
        {
            if (node.spawnedInstance != null)
                Destroy(node.spawnedInstance.gameObject);
        }

        currentRoomInstance = null;
        currentNode = null;
    }

    // ---------------- MOVERSE ENTRE ROOMS ----------------

    public void TryMoveToNextRoom(DoorDirection exitDirection)
    {
        if (isTransitioning || dungeonLayout == null)
            return;

        StartCoroutine(TransitionToRoomCoroutine(exitDirection));
    }

    private IEnumerator TransitionToRoomCoroutine(DoorDirection exitDirection)
    {
        isTransitioning = true;

        try
        {
            RoomType nextRoomType = progression.GetNextRoomType();

            bool alreadyLinked = currentNode != null && currentNode.HasNeighbor(exitDirection);
            RoomNode nextNode = TryCreateRoom(exitDirection, nextRoomType, out RoomType finalType);

            if (nextNode == null)
                yield break;

            // Solo se cuenta una room la primera vez que se crea. Volver por la misma puerta
            // no tiene que adelantar la Shop/Boss.
            if (!alreadyLinked)
                progression.RegisterGeneratedRoom(finalType);

            if (screenFader != null)
                yield return StartCoroutine(screenFader.FadeOut());

            DoorDirection entryDirection = DungeonLayout.GetOppositeDirection(exitDirection);

            if (currentRoomInstance != null)
                currentRoomInstance.gameObject.SetActive(false);

            yield return new WaitForSecondsRealtime(transitionDelay);

            RoomNode previousNode = currentNode;

            if (previousNode != null)
                previousNode.isCurrentRoom = false;

            currentNode = nextNode;
            currentNode.isCurrentRoom = true;

            EnterRoomSafely(currentNode, entryDirection);

            if (previousNode != null && previousNode.spawnedInstance != null)
                previousNode.spawnedInstance.ConfigureDoors(previousNode);

            RefreshMinimap();

            if (screenFader != null)
                yield return StartCoroutine(screenFader.FadeIn());
        }
        finally
        {
            isTransitioning = false;
        }
    }

    /// <summary>
    /// Solo crea/usa la room que está DETRÁS de la puerta cruzada. Nunca se cambia de dirección:
    /// eso era lo que dejaba al jugador en el medio sin la puerta de vuelta.
    /// </summary>
    private RoomNode TryCreateRoom(DoorDirection exitDirection, RoomType requestedType, out RoomType finalType)
    {
        finalType = requestedType;

        RoomNode node = dungeonLayout.CreateNextRoom(currentNode, exitDirection, requestedType);
        if (node != null)
            return node;

        if (requestedType != RoomType.Normal)
        {
            node = dungeonLayout.CreateNextRoom(currentNode, exitDirection, RoomType.Normal);
            if (node != null)
            {
                finalType = RoomType.Normal;
                return node;
            }
        }

        return null;
    }

    private void EnterRoomSafely(RoomNode node, DoorDirection? entryDirection)
    {
        try
        {
            EnterRoom(node, entryDirection);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }

    private void EnterRoom(RoomNode node, DoorDirection? entryDirection)
    {
        if (node == null || node.information == null || node.information.prefab == null)
            return;

        lastEntryDirection = entryDirection;

        if (node.spawnedInstance == null)
        {
            node.spawnedInstance = Instantiate(node.information.prefab, Vector3.zero, Quaternion.identity, roomParent);
            node.spawnedInstance.ConfigureDoors(node, entryDirection);
        }
        else
        {
            node.spawnedInstance.gameObject.SetActive(true);
        }

        currentRoomInstance = node.spawnedInstance;

        MovePlayerToCorrectSpawn(entryDirection);

        PlayerHealth playerHealth = player != null ? player.GetComponent<PlayerHealth>() : null;
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        playerHealth?.GetComponent<PlayerPoisonStatus>()?.NotifyRoomEntered();

        bool firstVisit = !node.hasBeenVisited;

        if (firstVisit)
        {
            if (node.information.type != RoomType.Challenge)
                currentRoomInstance.SpawnEnemies();

            if (node.information.type == RoomType.Boss)
                currentRoomInstance.LockDoors();

            if (node.information.type == RoomType.Shop)
                currentRoomInstance.UnlockDoorsInstant();

            if (node.information.type == RoomType.Challenge)
            {
                ChallengeRoomController challengeController = currentRoomInstance.GetComponent<ChallengeRoomController>();
                if (challengeController != null)
                    challengeController.Prepare();

                ChallengeRoomBase challenge = currentRoomInstance.GetComponentInChildren<ChallengeRoomBase>();
                if (challenge != null)
                    challenge.Prepare();
            }

            node.hasBeenVisited = true;

            if (node.uniqueNodeID != "Start")
                GenerateConnections(node);
        }
        else
        {
            currentRoomInstance.UnlockDoorsInstant();
        }

        AudioManager.PlayMusicForRoom(node.information.type, currentRoomInstance);

        if (currentRoomInstance != null && currentRoomInstance.IsInCombat)
            TabMenuUI.CloseCurrent();

        DungeonNavMesh.Ensure().RequestBake(currentRoomInstance);
    }

    private void MovePlayerToCorrectSpawn(DoorDirection? entryDirection)
    {
        if (player == null || currentRoomInstance == null)
            return;

        Transform spawnPoint = entryDirection.HasValue
            ? currentRoomInstance.GetSpawnPointFromEntry(entryDirection.Value)
            : currentRoomInstance.DefaultSpawnPoint;

        if (spawnPoint != null)
        {
            player.position = spawnPoint.position;
            player.GetComponent<PlayerWalkableClamp>()?.ResetTo(spawnPoint.position);
            Physics2D.SyncTransforms();
        }
    }

    private void RefreshMinimap()
    {
        if (minimapUI == null)
            return;

        minimapUI.BuildMap();
        minimapUI.RefreshMap();
    }

    // ---------------- CONEXIONES Y ROOMS OBLIGATORIAS ----------------

    private void GenerateConnections(RoomNode node)
    {
        if (IsDeadEnd(node))
            return;

        int maxNewRooms = Random.Range(1, 3);
        int created = 0;

        dungeonLayout.SetPreferSpareDoors(MissingEssentialRoom());

        foreach (DoorDirection dir in ShuffledDirections())
        {
            if (created >= maxNewRooms)
                break;

            if (node.HasNeighbor(dir) || !node.information.HasDoor(dir))
                continue;

            RoomType type = progression.GetNextRoomType();

            // NUEVO: si el nivel ya llegó a su tope, no se crean más rooms normales (esa puerta queda cerrada)
            if (type == RoomType.Normal && !progression.CanCreateNormalRoom)
                continue;

            RoomNode newNode = dungeonLayout.CreateNextRoom(node, dir, type, out bool createdNew);

            if (newNode == null && progression.CanCreateNormalRoom) // CAMBIADO: el respaldo a Normal también respeta el tope
                newNode = dungeonLayout.CreateNextRoom(node, dir, RoomType.Normal, out createdNew);

            if (newNode != null && createdNew)
            {
                progression.RegisterGeneratedRoom(newNode.information.type);
                created++;
            }
        }

        dungeonLayout.SetPreferSpareDoors(false);
        EnsureEssentialRooms();
    }

    private bool MissingEssentialRoom()
    {
        DungeonFloorConfig config = CurrentFloorConfig;

        bool shopMissing = !progression.HasSpawnedShop();
        bool bossMissing = !progression.HasSpawnedBoss();
        bool challengeMissing = HasPendingChallenges() && !challengeSpawned;
        bool buffMissing = config != null && config.buffRoom != null && !buffRoomSpawned;

        return shopMissing || bossMissing || challengeMissing || buffMissing;
    }

    /// <summary>
    /// Shop, Boss, Challenge y Buff Room (las que tenga el nivel) tienen que existir. Se cuelgan de una
    /// puerta libre (la del prefab, nunca una inventada) y se reintenta en cada room nueva hasta que entren.
    /// </summary>
    private void EnsureEssentialRooms()
    {
        if (!buffRoomSpawned)
            TryForceBuffRoom();

        if (!progression.HasSpawnedShop())
            TryAttachEssential(RoomType.Shop, farthestFirst: true);

        if (!challengeSpawned)
            TryForceChallengeRoom();

        // CAMBIADO: antes el boss se forzaba con 10 rooms fijas. Ahora cuando el nivel ya llegó a su largo.
        if (progression.IsBossDue)
            TryAttachEssential(RoomType.Boss, farthestFirst: true);
    }

    private bool TryForceBuffRoom()
    {
        DungeonFloorConfig config = CurrentFloorConfig;
        if (buffRoomSpawned || config == null || config.buffRoom == null)
            return false;

        foreach (RoomNode host in HostsForEssentials(farthestFirst: false))
        {
            foreach (DoorDirection dir in ShuffledDirections())
            {
                if (host.HasNeighbor(dir))
                    continue;

                RoomNode buffNode = dungeonLayout.CreateBuffRoom(host, dir, out bool createdNew);
                if (!createdNew || buffNode == null)
                    continue;

                buffRoomSpawned = true;
                progression.RegisterGeneratedRoom(RoomType.Normal);
                RefreshDoors(host);
                return true;
            }
        }

        return false;
    }

    private bool TryAttachEssential(RoomType type, bool farthestFirst)
    {
        foreach (RoomNode host in HostsForEssentials(farthestFirst))
        {
            foreach (DoorDirection dir in ShuffledDirections())
            {
                if (host.HasNeighbor(dir))
                    continue;

                RoomNode createdNode = dungeonLayout.CreateNextRoom(host, dir, type, out bool createdNew);
                if (!createdNew || createdNode == null || createdNode.information == null || createdNode.information.type != type)
                    continue;

                progression.RegisterGeneratedRoom(type);
                RefreshDoors(host);
                return true;
            }
        }

        return false;
    }

    private bool TryForceChallengeRoom()
    {
        if (!HasPendingChallenges() || challengeSpawned)
            return false;

        List<RoomNode> candidates = new List<RoomNode>();
        foreach (RoomNode node in dungeonLayout.GetAllRooms())
        {
            if (node == null || node.information == null || node.uniqueNodeID == "Start")
                continue;

            if (node.information.type == RoomType.Normal)
                candidates.Add(node);
        }

        Shuffle(candidates);

        foreach (RoomNode node in candidates)
        {
            foreach (DoorDirection dir in ShuffledDirections())
            {
                if (node.HasNeighbor(dir))
                    continue;

                RoomNode challengeNode = dungeonLayout.CreateNextRoom(node, dir, RoomType.Challenge, out bool createdNew);
                if (createdNew && challengeNode != null && challengeNode.information.type == RoomType.Challenge)
                {
                    challengeSpawned = true;
                    RefreshDoors(node);
                    return true;
                }
            }
        }

        return false;
    }

    private bool HasPendingChallenges()
    {
        DungeonFloorConfig config = CurrentFloorConfig;
        if (config == null || config.challengeRooms == null)
            return false;

        foreach (RoomInformation info in config.challengeRooms)
        {
            if (info == null || info.prefab == null)
                continue;

            if (!ChallengeRunState.WasCompleted(info.challengeId))
                return true;
        }

        return false;
    }

    private List<RoomNode> HostsForEssentials(bool farthestFirst)
    {
        List<RoomNode> hosts = new List<RoomNode>();

        foreach (RoomNode node in dungeonLayout.GetAllRooms())
        {
            if (node != null && node.information != null && !IsDeadEnd(node))
                hosts.Add(node);
        }

        if (farthestFirst)
            hosts.Sort((a, b) => b.gridPosition.sqrMagnitude.CompareTo(a.gridPosition.sqrMagnitude));
        else
            Shuffle(hosts);

        return hosts;
    }

    // ---------------- HELPERS ----------------

    private static bool IsDeadEnd(RoomNode node)
    {
        RoomType type = node.information.type;
        return type == RoomType.Shop || type == RoomType.Boss || type == RoomType.Challenge;
    }

    private static void RefreshDoors(RoomNode node)
    {
        if (node != null && node.spawnedInstance != null)
            node.spawnedInstance.ConfigureDoors(node);
    }

    private static List<DoorDirection> ShuffledDirections()
    {
        List<DoorDirection> directions = new List<DoorDirection>
        {
            DoorDirection.Up,
            DoorDirection.Down,
            DoorDirection.Left,
            DoorDirection.Right
        };

        Shuffle(directions);
        return directions;
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int swap = Random.Range(i, list.Count);
            (list[i], list[swap]) = (list[swap], list[i]);
        }
    }

    // ---------------- EXPULSAR (challenge "Steal the Item") ----------------

    public void ExpelFromCurrentRoom(bool sealRoom = true)
    {
        if (isTransitioning || currentNode == null)
            return;

        DoorDirection? exit = null;

        if (lastEntryDirection.HasValue && currentNode.HasNeighbor(lastEntryDirection.Value))
            exit = lastEntryDirection;

        if (!exit.HasValue)
        {
            foreach (DoorDirection dir in System.Enum.GetValues(typeof(DoorDirection)))
            {
                if (currentNode.HasNeighbor(dir))
                {
                    exit = dir;
                    break;
                }
            }
        }

        if (!exit.HasValue)
            return;

        StartCoroutine(ExpelRoutine(exit.Value, sealRoom));
    }

    private IEnumerator ExpelRoutine(DoorDirection exitDirection, bool sealRoom)
    {
        isTransitioning = true;

        try
        {
            RoomNode targetNode = currentNode.GetNeighbor(exitDirection);
            if (targetNode == null)
                yield break;

            if (screenFader != null)
                yield return StartCoroutine(screenFader.FadeOut());

            DoorDirection entryDirection = DungeonLayout.GetOppositeDirection(exitDirection);

            if (currentRoomInstance != null)
                currentRoomInstance.gameObject.SetActive(false);

            yield return new WaitForSecondsRealtime(transitionDelay);

            RoomNode previousNode = currentNode;
            previousNode.isCurrentRoom = false;

            if (sealRoom)
                previousNode.isSealed = true;

            currentNode = targetNode;
            currentNode.isCurrentRoom = true;

            EnterRoomSafely(currentNode, entryDirection);
            RefreshDoors(currentNode);
            RefreshMinimap();

            if (screenFader != null)
                yield return StartCoroutine(screenFader.FadeIn());
        }
        finally
        {
            isTransitioning = false;
        }
    }

    public List<RoomNode> GetAllRooms()
    {
        if (dungeonLayout == null)
            return new List<RoomNode>();

        return dungeonLayout.GetAllRooms();
    }
}