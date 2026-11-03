using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonManager : MonoBehaviour
{
    public static DungeonManager Instance;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform roomParent;
    [SerializeField] private ScreenFader screenFader;
    [SerializeField] private DungeonProgression dungeonProgression;

    private bool challengeSpawned = false;
    private bool buffRoomSpawned = false;

    [Header("Dungeon Configuration")]
    [SerializeField] private RoomInformation startRoomInformation;
    [SerializeField] private List<RoomInformation> normalRoomInformations = new List<RoomInformation>();
    [SerializeField] private RoomInformation shopRoomInformation;
    [Tooltip("Boss por defecto (se usa solo si el pool de abajo esta vacio).")]
    [SerializeField] private RoomInformation bossRoomInformation;
    [Tooltip("Bosses de los pisos NO finales (1 y 2): en el piso 1 sale UNO al azar (Araña o Sapo) y en el piso 2 sale el que NO salio en el piso 1 (nunca se repiten).")]
    [SerializeField] private List<RoomInformation> bossRoomInformations = new List<RoomInformation>();
    [Tooltip("Boss del ULTIMO piso (nivel 3): siempre el mismo, el Cuervo.")]
    [SerializeField] private RoomInformation finalBossRoomInformation;
    [SerializeField] private List<RoomInformation> challengeRoomInformations = new List<RoomInformation>();
    [Tooltip("Room de buff (Connection Room). Una por piso, en todos los niveles.")]
    [SerializeField] private RoomInformation buffRoomInformation;

    [Header("Transition")]
    [SerializeField] private float transitionDelay = 0.5f;

    [Header("Progression")]
    [Tooltip("Último nivel jugable. Al matar el boss en este nivel, el juego termina en vez de generar el siguiente piso.")]
    [SerializeField] private int maxFloor = 3;

    [Header("UI")]
    [SerializeField] private MinimapUI minimapUI;

    private DungeonLayout dungeonLayout;
    private RoomNode currentNode;
    private RoomInstance currentRoomInstance;
    private bool isTransitioning;
    private int currentFloor = 1;

    private DoorDirection? lastEntryDirection;

    // Bosses "random" (Araña / Sapo) que ya salieron en esta partida: no se repiten en el piso siguiente.
    private readonly List<RoomInformation> usedRandomBosses = new List<RoomInformation>();

    public int CurrentFloor => currentFloor;
    public int MaxFloor => maxFloor;
    public bool IsFinalFloor => currentFloor >= maxFloor;
    public RoomInstance CurrentRoom => currentRoomInstance;

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

    private List<RoomInformation> GetNormalRoomsForFloor(int floor)
    {
        List<RoomInformation> filtered = normalRoomInformations.FindAll(
            r => r != null && r.level == floor
        );

        if (filtered.Count == 0)
        {
            Debug.LogWarning($"No hay Room Informations normales configuradas para el nivel {floor}. Se usará el pool completo como fallback.");
            return new List<RoomInformation>(normalRoomInformations);
        }

        return filtered;
    }

    /// <summary>
    /// Orden de bosses:
    ///  - Piso 1: UNO al azar entre los bosses del pool (Araña o Sapo).
    ///  - Piso 2: el que NO salio en el piso 1 (los bosses nunca se repiten: Araña -> Sapo o Sapo -> Araña).
    ///  - Ultimo piso (nivel 3): SIEMPRE el Cuervo.
    /// </summary>
    private RoomInformation PickBossRoomInformationForFloor()
    {
        if (IsFinalFloor && finalBossRoomInformation != null && finalBossRoomInformation.prefab != null)
        {
            Debug.Log($"[DungeonManager] Piso {currentFloor} (final): boss = {finalBossRoomInformation.roomID}");
            return finalBossRoomInformation;
        }

        List<RoomInformation> pool = new List<RoomInformation>();
        foreach (RoomInformation info in bossRoomInformations)
        {
            if (info != null && info.prefab != null && !pool.Contains(info))
                pool.Add(info);
        }

        if (pool.Count == 0)
            return bossRoomInformation;

        List<RoomInformation> available = pool.FindAll(info => !usedRandomBosses.Contains(info));

        if (available.Count == 0)
        {
            RoomInformation previous = usedRandomBosses.Count > 0 ? usedRandomBosses[usedRandomBosses.Count - 1] : null;
            usedRandomBosses.Clear();

            available = new List<RoomInformation>(pool);
            if (previous != null && available.Count > 1)
                available.Remove(previous);
        }

        RoomInformation chosen = available[Random.Range(0, available.Count)];
        usedRandomBosses.Add(chosen);
        Debug.Log($"[DungeonManager] Piso {currentFloor}: boss = {chosen.roomID} (ya salieron: {usedRandomBosses.Count} de {pool.Count})");
        return chosen;
    }

    private void BuildNewDungeon()
    {
        challengeSpawned = false;
        buffRoomSpawned = false;
        ChallengeRunState.Reset();

        if (dungeonProgression != null)
            dungeonProgression.PrepareFloor(currentFloor);

        List<RoomInformation> floorRooms = GetNormalRoomsForFloor(currentFloor);

        dungeonLayout = new DungeonLayout();
        dungeonLayout.Build(startRoomInformation, floorRooms, shopRoomInformation, PickBossRoomInformationForFloor(), challengeRoomInformations, buffRoomInformation);

        currentNode = dungeonLayout.StartNode;
        currentNode.isCurrentRoom = true;

        RoomNode firstRoom = dungeonLayout.CreateNextRoom(currentNode, DoorDirection.Up, RoomType.Normal);
        if (firstRoom != null)
            dungeonProgression?.RegisterGeneratedRoom(RoomType.Normal);

        EnterRoomSafely(currentNode, null);

        // La primera room ya deja colgadas Shop, Boss, Challenge y Buff si hay puerta libre.
        if (firstRoom != null)
            GenerateConnections(firstRoom);

        if (currentNode.spawnedInstance != null)
            currentNode.spawnedInstance.ConfigureDoors(currentNode);
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
            yield return StartCoroutine(screenFader.FadeOut());

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
            yield return StartCoroutine(screenFader.FadeIn());

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

    public void TryMoveToNextRoom(DoorDirection exitDirection)
    {
        if (isTransitioning)
            return;

        StartCoroutine(TransitionToRoomCoroutine(exitDirection));
    }

    private IEnumerator TransitionToRoomCoroutine(DoorDirection exitDirection)
    {
        isTransitioning = true;

        try
        {
            RoomType nextRoomType = dungeonProgression != null
                ? dungeonProgression.GetNextRoomType()
                : RoomType.Normal;

            bool alreadyLinked = currentNode != null && currentNode.HasNeighbor(exitDirection);
            RoomNode nextNode = TryCreateRoom(exitDirection, nextRoomType, out RoomType finalType);

            if (nextNode == null)
                yield break;

            // Solo se cuenta una room la primera vez que se crea. Volver por la misma puerta
            // no tiene que adelantar la Shop/Boss.
            if (!alreadyLinked)
                dungeonProgression?.RegisterGeneratedRoom(finalType);

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

            if (minimapUI != null)
            {
                minimapUI.BuildMap();
                minimapUI.RefreshMap();
            }

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
                ChallengeRoomController challengeController =
                    currentRoomInstance.GetComponent<ChallengeRoomController>();
                if (challengeController != null)
                    challengeController.Prepare();

                ChallengeRoomBase challenge =
                    currentRoomInstance.GetComponentInChildren<ChallengeRoomBase>();

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

    private void GenerateConnections(RoomNode node)
    {
        if (node.information.type == RoomType.Shop || node.information.type == RoomType.Boss || node.information.type == RoomType.Challenge)
            return;

        int maxNewRooms = Random.Range(1, 3);

        List<DoorDirection> directions = new List<DoorDirection>
        {
            DoorDirection.Up,
            DoorDirection.Down,
            DoorDirection.Left,
            DoorDirection.Right
        };

        for (int i = 0; i < directions.Count; i++)
        {
            DoorDirection temp = directions[i];
            int randomIndex = Random.Range(i, directions.Count);
            directions[i] = directions[randomIndex];
            directions[randomIndex] = temp;
        }

        int created = 0;
        bool missingEssential = MissingEssentialRoom();
        dungeonLayout.SetPreferSpareDoors(missingEssential);

        foreach (DoorDirection dir in directions)
        {
            if (created >= maxNewRooms)
                break;

            if (node.HasNeighbor(dir))
                continue;

            if (node.information != null && !node.information.HasDoor(dir))
                continue;

            RoomType type = dungeonProgression != null
                ? dungeonProgression.GetNextRoomType()
                : RoomType.Normal;

            RoomNode newNode = dungeonLayout.CreateNextRoom(node, dir, type, out bool createdNew);

            if (newNode == null)
            {
                newNode = dungeonLayout.CreateNextRoom(node, dir, RoomType.Normal, out createdNew);
                if (newNode != null && createdNew)
                {
                    dungeonProgression?.RegisterGeneratedRoom(RoomType.Normal);
                    created++;
                }

                continue;
            }

            if (createdNew)
            {
                dungeonProgression?.RegisterGeneratedRoom(newNode.information.type);
                created++;
            }
        }

        dungeonLayout.SetPreferSpareDoors(false);
        EnsureEssentialRooms();
    }

    private bool MissingEssentialRoom()
    {
        bool shopMissing = dungeonProgression == null || !dungeonProgression.HasSpawnedShop();
        bool bossMissing = dungeonProgression == null || !dungeonProgression.HasSpawnedBoss();
        bool challengeMissing = HasPendingChallenges() && !challengeSpawned;
        bool buffMissing = buffRoomInformation != null && !buffRoomSpawned;
        return shopMissing || bossMissing || challengeMissing || buffMissing;
    }

    /// <summary>
    /// Shop, Boss, Challenge y Buff Room tienen que existir en cada piso. Se cuelgan de una puerta libre
    /// (la del prefab, nunca una inventada) y se reintenta en cada room nueva hasta que entren.
    /// </summary>
    private void EnsureEssentialRooms()
    {
        if (!buffRoomSpawned)
            TryForceBuffSmart();

        if (dungeonProgression != null && !dungeonProgression.HasSpawnedShop())
            TryForceShopSmart();

        if (!challengeSpawned)
            TryForceChallengeSmart();

        // La boss se cuelga en el extremo, cuando el piso ya tiene recorrido. Si todavía no entró, se reintenta.
        if (dungeonProgression != null && dungeonProgression.HasSpawnedShop() && !dungeonProgression.HasSpawnedBoss()
            && dungeonLayout.GetAllRooms().Count >= 10)
            TryForceBossSmart();
    }

    private bool TryForceShopSmart()
    {
        return TryAttachEssential(RoomType.Shop, farthestFirst: true);
    }

    private bool TryForceBuffSmart()
    {
        if (buffRoomSpawned || buffRoomInformation == null)
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
                dungeonProgression?.RegisterGeneratedRoom(RoomType.Normal);

                if (host.spawnedInstance != null)
                    host.spawnedInstance.ConfigureDoors(host);

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

                dungeonProgression?.RegisterGeneratedRoom(type);

                if (host.spawnedInstance != null)
                    host.spawnedInstance.ConfigureDoors(host);

                return true;
            }
        }

        return false;
    }

    private List<RoomNode> HostsForEssentials(bool farthestFirst)
    {
        List<RoomNode> hosts = new List<RoomNode>();

        foreach (RoomNode node in dungeonLayout.GetAllRooms())
        {
            if (node == null || node.information == null)
                continue;

            if (node.information.type == RoomType.Shop || node.information.type == RoomType.Boss || node.information.type == RoomType.Challenge)
                continue;

            hosts.Add(node);
        }

        if (farthestFirst)
        {
            hosts.Sort((a, b) => b.gridPosition.sqrMagnitude.CompareTo(a.gridPosition.sqrMagnitude));
        }
        else
        {
            for (int i = 0; i < hosts.Count; i++)
            {
                int swap = Random.Range(i, hosts.Count);
                RoomNode temp = hosts[i];
                hosts[i] = hosts[swap];
                hosts[swap] = temp;
            }
        }

        return hosts;
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

        for (int i = 0; i < directions.Count; i++)
        {
            int swap = Random.Range(i, directions.Count);
            DoorDirection temp = directions[i];
            directions[i] = directions[swap];
            directions[swap] = temp;
        }

        return directions;
    }

    private bool TryForceBossSmart()
    {
        if (dungeonProgression == null || !dungeonProgression.HasSpawnedShop() || dungeonProgression.HasSpawnedBoss())
            return false;

        return TryAttachEssential(RoomType.Boss, farthestFirst: true);
    }

    private bool TryForceChallengeSmart()
    {
        if (!HasPendingChallenges() || challengeSpawned)
            return false;

        List<RoomNode> candidates = new List<RoomNode>();

        foreach (RoomNode node in dungeonLayout.GetAllRooms())
        {
            if (node == null || node.information == null)
                continue;

            if (node.uniqueNodeID == "Start")
                continue;

            if (node.information.type != RoomType.Normal)
                continue;

            candidates.Add(node);
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            int randomIndex = Random.Range(i, candidates.Count);
            RoomNode temp = candidates[i];
            candidates[i] = candidates[randomIndex];
            candidates[randomIndex] = temp;
        }

        foreach (RoomNode node in candidates)
        {
            List<DoorDirection> directions = new List<DoorDirection>
            {
                DoorDirection.Up,
                DoorDirection.Down,
                DoorDirection.Left,
                DoorDirection.Right
            };

            for (int i = 0; i < directions.Count; i++)
            {
                int randomIndex = Random.Range(i, directions.Count);
                DoorDirection temp = directions[i];
                directions[i] = directions[randomIndex];
                directions[randomIndex] = temp;
            }

            foreach (DoorDirection dir in directions)
            {
                if (node.HasNeighbor(dir))
                    continue;

                RoomNode challengeNode = dungeonLayout.CreateNextRoom(node, dir, RoomType.Challenge, out bool createdNew);
                if (createdNew && challengeNode != null && challengeNode.information.type == RoomType.Challenge)
                {
                    challengeSpawned = true;

                    if (node.spawnedInstance != null)
                        node.spawnedInstance.ConfigureDoors(node);

                    return true;
                }
            }
        }

        return false;
    }

    private bool HasPendingChallenges()
    {
        foreach (RoomInformation info in challengeRoomInformations)
        {
            if (info == null || info.prefab == null)
                continue;

            if (!ChallengeRunState.WasCompleted(info.challengeId))
                return true;
        }

        return false;
    }

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

            if (currentNode.spawnedInstance != null)
                currentNode.spawnedInstance.ConfigureDoors(currentNode);

            if (minimapUI != null)
            {
                minimapUI.BuildMap();
                minimapUI.RefreshMap();
            }

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
