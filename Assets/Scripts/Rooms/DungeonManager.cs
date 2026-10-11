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
    }

    private void Start()
    {
        InitializeDungeon();
    }

    private void InitializeDungeon()
    {
        BuildNewDungeon();

        if (minimapUI != null)
        {
            minimapUI.Initialize(this);
        }
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

        // Sin repeticion: solo pueden salir los bosses que todavia no aparecieron en esta partida.
        List<RoomInformation> available = pool.FindAll(info => !usedRandomBosses.Contains(info));

        if (available.Count == 0)
        {
            // Ya salieron todos (solo pasa si hay mas pisos "random" que bosses en el pool): se reinicia el ciclo
            // evitando que el primero del ciclo nuevo sea el ultimo que salio.
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

        // Los challenges se juegan UNA vez por nivel (no por run): al empezar cada piso vuelven a estar disponibles.
        ChallengeRunState.Reset();

        List<RoomInformation> floorRooms = GetNormalRoomsForFloor(currentFloor);

        dungeonLayout = new DungeonLayout();
        dungeonLayout.Build(startRoomInformation, floorRooms, shopRoomInformation, PickBossRoomInformationForFloor(), challengeRoomInformations);

        currentNode = dungeonLayout.StartNode;
        currentNode.isCurrentRoom = true;

        dungeonLayout.CreateNextRoom(currentNode, DoorDirection.Up, RoomType.Normal);

        EnterRoom(currentNode, null);

        if (currentNode.spawnedInstance != null)
        {
            currentNode.spawnedInstance.ConfigureDoors(currentNode);
        }

    }

    // BuffPool no se resetea: los power-ups ya usados no vuelven a salir en el piso siguiente.
    public void StartNextFloor()
    {
        if (isTransitioning) return;
        if (IsFinalFloor) return;

        StartCoroutine(NextFloorRoutine());
    }

    private IEnumerator NextFloorRoutine()
    {
        isTransitioning = true;

        if (screenFader != null)
            yield return StartCoroutine(screenFader.FadeOut());

        DestroyCurrentDungeon();

        // Los pickups de mejoras sin recoger se destruyen con el piso: sus items vuelven a estar disponibles.
        UpgradeRegistry.ClearReservations();

        currentFloor++;
        dungeonProgression.ResetProgression();

        BuildNewDungeon();

        if (minimapUI != null)
        {
            minimapUI.Initialize(this);
        }

        if (screenFader != null)
            yield return StartCoroutine(screenFader.FadeIn());

        isTransitioning = false;
    }

    private void DestroyCurrentDungeon()
    {
        if (dungeonLayout == null) return;

        foreach (RoomNode node in dungeonLayout.GetAllRooms())
        {
            if (node.spawnedInstance != null)
            {
                Destroy(node.spawnedInstance.gameObject);
            }
        }

        currentRoomInstance = null;
        currentNode = null;
    }

    public void TryMoveToNextRoom(DoorDirection exitDirection)
    {

        if (isTransitioning)
        {
            return;
        }

        StartCoroutine(TransitionToRoomCoroutine(exitDirection));
    }

    private IEnumerator TransitionToRoomCoroutine(DoorDirection exitDirection)
    {
        isTransitioning = true;

        RoomType nextRoomType = dungeonProgression.GetNextRoomType();

        RoomNode nextNode = TryCreateRoom(exitDirection, nextRoomType, out RoomType finalType);

        if (nextNode == null)
        {
            isTransitioning = false;
            yield break;
        }

        dungeonProgression.RegisterGeneratedRoom(finalType);

        yield return StartCoroutine(screenFader.FadeOut());

        DoorDirection entryDirection = DungeonLayout.GetOppositeDirection(exitDirection);

        if (currentRoomInstance != null) 
        {
            currentRoomInstance.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(transitionDelay);

        RoomNode previousNode = currentNode;

        if (previousNode != null)
        {
            previousNode.isCurrentRoom = false;
        }

        currentNode = nextNode;

        currentNode.isCurrentRoom = true;

        EnterRoom(currentNode, entryDirection);

        if (previousNode.spawnedInstance != null)
        {
            previousNode.spawnedInstance.ConfigureDoors(previousNode);
        }

        if (minimapUI != null)
        {
            minimapUI.BuildMap();
            minimapUI.RefreshMap();
        }

        yield return StartCoroutine(screenFader.FadeIn());

        isTransitioning = false;
    }

    private RoomNode TryCreateRoom(DoorDirection exitDirection, RoomType requestedType, out RoomType finalType)
    {
        finalType = requestedType;

        RoomNode node = dungeonLayout.CreateNextRoom(currentNode, exitDirection, requestedType);

        if (node == null && requestedType != RoomType.Normal)
        {
            foreach (DoorDirection dir in System.Enum.GetValues(typeof(DoorDirection)))
            {
                if (dir == exitDirection) continue;

                node = dungeonLayout.CreateNextRoom(currentNode, dir, requestedType);

                if (node != null)
                {
                    finalType = requestedType;
                    return node;
                }
            }
        }

        if (node == null)
        {
            foreach (DoorDirection dir in System.Enum.GetValues(typeof(DoorDirection)))
            {
                node = dungeonLayout.CreateNextRoom(currentNode, dir, RoomType.Normal);

                if (node != null)
                {
                    finalType = RoomType.Normal;
                    return node;
                }
            }
        }

        return node;
    }

    private void EnterRoom(RoomNode node, DoorDirection? entryDirection)
    {
        if (node == null || node.information == null || node.information.prefab == null)
        {
            return;
        }

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
            {
                currentRoomInstance.LockDoors();
            }

            if (node.information.type == RoomType.Shop)
            {
                currentRoomInstance.UnlockDoorsInstant();
            }

            if (node.information.type == RoomType.Challenge)
            {
                ChallengeRoomController challengeController =
                    currentRoomInstance.GetComponent<ChallengeRoomController>();
                if (challengeController != null)
                    challengeController.Prepare();

                ChallengeRoomBase challenge =
                    currentRoomInstance.GetComponentInChildren<ChallengeRoomBase>();

                if (challenge != null)
                {
                    challenge.Prepare();
                }
            }

            node.hasBeenVisited = true;

            if (node.uniqueNodeID != "Start")
            {
                GenerateConnections(node);
            }
        }
        else
        {
            currentRoomInstance.UnlockDoorsInstant();
        }

        AudioManager.PlayMusicForRoom(node.information.type, currentRoomInstance);

        if (currentRoomInstance != null && currentRoomInstance.IsInCombat)
            TabMenuUI.CloseCurrent();
    }

    private void MovePlayerToCorrectSpawn(DoorDirection? entryDirection)
    {
        if (player == null || currentRoomInstance == null)
        {
            return;
        }

        Transform spawnPoint;

        if (entryDirection.HasValue)
        {
            spawnPoint = currentRoomInstance.GetSpawnPointFromEntry(entryDirection.Value);
        }
        else
        {
            spawnPoint = currentRoomInstance.DefaultSpawnPoint;
        }
        if (spawnPoint != null) 
        {
            player.position = spawnPoint.position;
            player.GetComponent<PlayerWalkableClamp>()?.ResetTo(spawnPoint.position);

            // autoSyncTransforms está off: sin esto la física sigue en la posición vieja y los enemigos no detectan al jugador.
            Physics2D.SyncTransforms();
        }
    }

    private void GenerateConnections(RoomNode node)
    {
        if (node.information.type == RoomType.Shop || node.information.type == RoomType.Boss || node.information.type == RoomType.Challenge)
        {
            return;
        }

        int maxNewRooms = Random.Range(2, 4);

        List<DoorDirection> directions = new List<DoorDirection>()
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

        foreach (var dir in directions)
        {
            if (created >= maxNewRooms)
                break;

            if (node.HasNeighbor(dir))
                continue;

            RoomType type = dungeonProgression.GetNextRoomType();

            RoomNode newNode = dungeonLayout.CreateNextRoom(node, dir, type);

            if (newNode == null)
            {
                newNode = dungeonLayout.CreateNextRoom(node, dir, RoomType.Normal);

                if (newNode != null)
                {
                    dungeonProgression.RegisterGeneratedRoom(RoomType.Normal);
                    created++;
                }

                continue;
            }

            dungeonProgression.RegisterGeneratedRoom(newNode.information.type);
            created++;
        }

        if (!dungeonProgression.HasSpawnedShop())
        {
            TryForceShopSmart();
        }

        if (dungeonProgression.HasSpawnedShop() && !dungeonProgression.HasSpawnedBoss())
        {
            TryForceBossSmart();
        }

        if (!challengeSpawned)
        {
            TryForceChallengeSmart();
        }
    }
   
    private bool TryForceShopSmart()
    {
        RoomNode bestNode = null;
        float bestDistance = -1f;

        foreach (RoomNode node in dungeonLayout.GetAllRooms())
        {
            if (node.uniqueNodeID == "Start")
                continue;

            if (node.information.type == RoomType.Shop || node.information.type == RoomType.Boss)
                continue;

            float dist = node.gridPosition.magnitude;

            if (dist > bestDistance)
            {
                bestDistance = dist;
                bestNode = node;
            }
        }

        if (bestNode == null)
            return false;

        foreach (DoorDirection dir in System.Enum.GetValues(typeof(DoorDirection)))
        {
            if (bestNode.HasNeighbor(dir))
                continue;

            RoomNode newNode = dungeonLayout.CreateNextRoom(bestNode, dir, RoomType.Shop);

            if (newNode != null && newNode.information.type == RoomType.Shop)
            {
                dungeonProgression.RegisterGeneratedRoom(RoomType.Shop);

                if (bestNode.spawnedInstance != null)
                {
                    bestNode.spawnedInstance.ConfigureDoors(bestNode);
                }

                return true;
            }
        }

        return false;
    }
    private bool TryForceBossSmart()
    {
        if (!dungeonProgression.HasSpawnedShop())
            return false;

        List<RoomNode> candidates = new List<RoomNode>();

        foreach (RoomNode node in dungeonLayout.GetAllRooms())
        {
            if (node.uniqueNodeID == "Start")
                continue;

            if (node.information.type == RoomType.Shop || node.information.type == RoomType.Boss)
                continue;

            candidates.Add(node);
        }

        candidates.Sort((a, b) =>
            b.gridPosition.magnitude.CompareTo(a.gridPosition.magnitude)
        );

        foreach (RoomNode node in candidates)
        {
            foreach (DoorDirection dir in System.Enum.GetValues(typeof(DoorDirection)))
            {
                if (node.HasNeighbor(dir))
                    continue;

                RoomNode newNode = dungeonLayout.CreateNextRoom(node, dir, RoomType.Boss);

                if (newNode != null && newNode.information.type == RoomType.Boss)
                {
                    dungeonProgression.RegisterGeneratedRoom(RoomType.Boss);
                    return true;
                }
            }
        }

        Debug.LogWarning("No se pudo forzar la Boss Room en ningún lugar válido.");
        return false;
    }

    private bool TryForceChallengeSmart()
    {
        if (!HasPendingChallenges())
            return false;

        if (challengeSpawned)
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
            List<DoorDirection> directions = new List<DoorDirection>()
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

                RoomNode challengeNode =
                    dungeonLayout.CreateNextRoom(
                        node,
                        dir,
                        RoomType.Challenge
                    );

                if (challengeNode != null &&
                    challengeNode.information.type == RoomType.Challenge)
                {
                    challengeSpawned = true;

                    if (node.spawnedInstance != null)
                    {
                        node.spawnedInstance.ConfigureDoors(node);
                    }

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
            if (info == null)
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

        RoomNode targetNode = currentNode.GetNeighbor(exitDirection);

        if (targetNode == null)
        {
            isTransitioning = false;
            yield break;
        }

        if (screenFader != null)
            yield return StartCoroutine(screenFader.FadeOut());

        DoorDirection entryDirection = DungeonLayout.GetOppositeDirection(exitDirection);

        if (currentRoomInstance != null)
            currentRoomInstance.gameObject.SetActive(false);

        yield return new WaitForSeconds(transitionDelay);

        RoomNode previousNode = currentNode;
        previousNode.isCurrentRoom = false;

        if (sealRoom)
            previousNode.isSealed = true;

        currentNode = targetNode;
        currentNode.isCurrentRoom = true;

        EnterRoom(currentNode, entryDirection);

        if (currentNode.spawnedInstance != null)
            currentNode.spawnedInstance.ConfigureDoors(currentNode);

        if (minimapUI != null)
        {
            minimapUI.BuildMap();
            minimapUI.RefreshMap();
        }

        if (screenFader != null)
            yield return StartCoroutine(screenFader.FadeIn());

        isTransitioning = false;
    }

    public List<RoomNode> GetAllRooms()
    {
        if (dungeonLayout == null)
            return new List<RoomNode>();

        return dungeonLayout.GetAllRooms();
    }

}
