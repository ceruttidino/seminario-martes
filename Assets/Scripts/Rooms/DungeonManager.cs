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
    [Tooltip("Ya no define el layout: el piso entero se genera al empezar (DungeonLayout). Solo se usa para reiniciar el contador al cambiar de piso.")]
    [SerializeField] private DungeonProgression dungeonProgression;

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

    [Header("Level Size")]
    [Tooltip("Cantidad TOTAL de rooms del piso 1 (incluye Start, Shop, Boss y Challenge). En cada partida se sortea un valor entre X (minimo) e Y (maximo).")]
    [SerializeField] private Vector2Int floor1Rooms = new Vector2Int(8, 10);
    [Tooltip("Idem para el piso 2: un poco mas largo que el 1.")]
    [SerializeField] private Vector2Int floor2Rooms = new Vector2Int(10, 13);
    [Tooltip("Idem para el piso 3 (y siguientes): el mas largo.")]
    [SerializeField] private Vector2Int floor3Rooms = new Vector2Int(12, 15);
    [Tooltip("0 = el piso sale distinto en cada partida. Cualquier otro numero genera SIEMPRE el mismo piso (solo para probar / reproducir un bug). La semilla usada se imprime en la consola al empezar cada piso.")]
    [SerializeField] private int debugSeed = 0;

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

    /// <summary>
    /// Cantidad total de rooms del piso: un valor al azar entre el minimo y el maximo configurados. Cada piso es un
    /// poco mas largo que el anterior.
    /// </summary>
    private int RollRoomCount(int floor)
    {
        Vector2Int range = floor <= 1 ? floor1Rooms : (floor == 2 ? floor2Rooms : floor3Rooms);

        int min = Mathf.Max(5, Mathf.Min(range.x, range.y));
        int max = Mathf.Max(min, Mathf.Max(range.x, range.y));

        return Random.Range(min, max + 1);
    }

    /// <summary>
    /// Genera el piso ENTERO de una vez (estilo Isaac): todas las rooms y a donde lleva cada puerta quedan definidas desde
    /// el principio. Nada esta descubierto: el minimapa solo muestra lo que el jugador va visitando.
    /// </summary>
    private void BuildNewDungeon()
    {
        // Los challenges se juegan UNA vez por nivel (no por run): al empezar cada piso vuelven a estar disponibles.
        ChallengeRunState.Reset();

        List<RoomInformation> floorRooms = GetNormalRoomsForFloor(currentFloor);
        RoomInformation bossRoom = PickBossRoomInformationForFloor();

        int totalRooms = RollRoomCount(currentFloor);
        int seed = debugSeed != 0 ? debugSeed + currentFloor : Random.Range(int.MinValue, int.MaxValue);

        dungeonLayout = new DungeonLayout();
        bool generated = dungeonLayout.Generate(startRoomInformation, floorRooms, shopRoomInformation, bossRoom,
            challengeRoomInformations, totalRooms, seed);

        // Si por configuracion no entro, se prueba con un piso mas chico antes de rendirse.
        for (int shrink = 1; !generated && shrink <= 3; shrink++)
        {
            Debug.LogWarning($"[DungeonManager] Piso {currentFloor}: no se pudo armar con {totalRooms} rooms ({dungeonLayout.LastError}). Reintentando mas chico.");
            totalRooms = Mathf.Max(5, totalRooms - 2);
            generated = dungeonLayout.Generate(startRoomInformation, floorRooms, shopRoomInformation, bossRoom,
                challengeRoomInformations, totalRooms, seed + shrink);
        }

        if (!generated)
        {
            Debug.LogError($"[DungeonManager] Piso {currentFloor}: no se pudo generar el piso ({dungeonLayout.LastError}). Se usa un piso minimo (Start + Boss). Revisar la configuracion de RoomInformation.");

            if (!dungeonLayout.GenerateFallback(startRoomInformation, bossRoom))
                return;
        }

        LogLayout(totalRooms, seed);

        currentNode = dungeonLayout.StartNode;
        currentNode.isCurrentRoom = true;

        EnterRoomSafely(currentNode, null);

        if (currentNode.spawnedInstance != null)
        {
            currentNode.spawnedInstance.ConfigureDoors(currentNode);
        }
    }

    private void LogLayout(int requestedRooms, int seed)
    {
        List<RoomNode> all = dungeonLayout.GetAllRooms();

        int normal = 0, shop = 0, boss = 0, challenge = 0;
        foreach (RoomNode node in all)
        {
            switch (node.information.type)
            {
                case RoomType.Normal: normal++; break;
                case RoomType.Shop: shop++; break;
                case RoomType.Boss: boss++; break;
                case RoomType.Challenge: challenge++; break;
            }
        }

        Debug.Log($"[DungeonManager] Piso {currentFloor}: {all.Count} rooms (pedidas {requestedRooms}) = Start + {normal} normales + {shop} Shop + {boss} Boss + {challenge} Challenge | semilla {seed} | intentos {dungeonLayout.Attempts}\n" +
                  "S = Start, o = normal, $ = Shop, B = Boss, C = Challenge\n" + dungeonLayout.ToAscii());
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

        // El piso ya esta generado: la puerta por la que sale el jugador siempre lleva a una room que ya existe, y esa room
        // tiene la puerta de vuelta en el lado opuesto. No se crea ni se cambia nada al pasar.
        RoomNode nextNode = currentNode != null ? currentNode.GetNeighbor(exitDirection) : null;

        if (nextNode == null)
        {
            isTransitioning = false;
            yield break;
        }

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

        EnterRoomSafely(currentNode, entryDirection);

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

    /// <summary>
    /// Entra a la room sin que un error de UNA room (un dato roto en un prefab) deje la transicion colgada: el error se
    /// muestra en la consola, pero la transicion termina igual (la pantalla vuelve a verse y el juego no se traba).
    /// </summary>
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

            // Visitada = descubierta: recien ahi el minimapa la muestra (y deja ver las que tiene pegadas).
            node.hasBeenVisited = true;
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

        isTransitioning = false;
    }

    public List<RoomNode> GetAllRooms()
    {
        if (dungeonLayout == null)
            return new List<RoomNode>();

        return dungeonLayout.GetAllRooms();
    }

}
