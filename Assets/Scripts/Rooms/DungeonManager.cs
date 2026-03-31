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

        EnterRoom(currentNode, null);
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

        currentNode = nextNode;
        EnterRoom(currentNode, entryDirection);

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
                    return node;
            }
        }

        if (node == null)
        {
            if(requestedType == RoomType.Shop || requestedType == RoomType.Boss)
            {
                return null;
            }

            node = dungeonLayout.CreateNextRoom(currentNode, exitDirection, RoomType.Normal);
            finalType = RoomType.Normal;
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
            node.spawnedInstance.ConfigureDoors(node);
        }
        else
        {
            node.spawnedInstance.gameObject.SetActive(true);
        }

        currentRoomInstance = node.spawnedInstance;
        node.hasBeenVisited = true;

        MovePlayerToCorrectSpawn(entryDirection);
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
}
