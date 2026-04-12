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

    [Header("Dungeon Configuration")]
    [SerializeField] private RoomInformation startRoomInformation;
    [SerializeField] private List<RoomInformation> normalRoomInformations = new List<RoomInformation>();
    [SerializeField] private RoomInformation shopRoomInformation;
    [SerializeField] private RoomInformation bossRoomInformation;

    [Header("Transition")]
    [SerializeField] private float transitionDelay = 0.5f;

    private DungeonLayout dungeonLayout;
    private RoomNode currentNode;
    private RoomInstance currentRoomInstance;
    private bool isTransitioning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        InitializeDungeon();
    }

    private void InitializeDungeon()
    {
        dungeonLayout = new DungeonLayout();
        dungeonLayout.Build(startRoomInformation, normalRoomInformations, shopRoomInformation, bossRoomInformation);

        currentNode = dungeonLayout.StartNode;

        dungeonLayout.CreateNextRoom(currentNode, DoorDirection.Up, RoomType.Normal);

        EnterRoom(currentNode, null);

        if (currentNode.spawnedInstance != null)
        {
            currentNode.spawnedInstance.ConfigureDoors(currentNode);
        }
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

        currentNode = nextNode;
        EnterRoom(currentNode, entryDirection);

        if (previousNode.spawnedInstance != null)
        {
            previousNode.spawnedInstance.ConfigureDoors(previousNode);
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

        if (!node.hasBeenVisited)
        {
            currentRoomInstance.SpawnEnemies();
            node.hasBeenVisited = true;
        }
        else
        {
            currentRoomInstance.UnlockDoorsInstant();
        }

        if (node.uniqueNodeID != "Start")
        {
            GenerateConnections(node);
        }
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
        }
    }

    private void GenerateConnections(RoomNode node)
    {
        int maxNewRooms = Random.Range(2, 4);

        List<DoorDirection> directions = new List<DoorDirection>()
    {
        DoorDirection.Up,
        DoorDirection.Down,
        DoorDirection.Left,
        DoorDirection.Right
    };

        // shuffle
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

            dungeonProgression.RegisterGeneratedRoom(type);
            created++;
        }

        if (!dungeonProgression.HasSpawnedBoss())
        {
            TryForceBossSmart();
        }
    }

    private bool TryForceBossSmart()
    {
        RoomNode bestNode = null;
        float bestDistance = -1f;

        foreach (RoomNode node in dungeonLayout.GetAllRooms())
        {
            if (node.uniqueNodeID == "Start")
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

            RoomNode newNode = dungeonLayout.CreateNextRoom(bestNode, dir, RoomType.Boss);

            if (newNode != null)
            {
                dungeonProgression.RegisterGeneratedRoom(RoomType.Boss);
                Debug.Log("BOSS GENERADO EN ROOM LEJANA");
                return true;
            }
        }

        return false;
    }

}
