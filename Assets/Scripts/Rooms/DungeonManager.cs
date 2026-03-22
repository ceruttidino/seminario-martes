using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonManager : MonoBehaviour
{
    public static DungeonManager Instance;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform roomParent;

    [Header("Dungeon Configuration")]
    [SerializeField] private RoomInformation startRoomInformation;
    [SerializeField] private List<RoomInformation> normalRoomInformations = new List<RoomInformation>();

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
        dungeonLayout.Build(startRoomInformation, normalRoomInformations);

        currentNode = dungeonLayout.StartNode;

        if (currentNode != null && currentNode.information != null)
        {
            Debug.Log($"[DungeonManager] Start node created: {currentNode.uniqueNodeID} | Room: {currentNode.information.roomID}");
        }

        EnterRoom(currentNode, null);
    }

    public void TryMoveToNextRoom(DoorDirection exitDirection)
    {
        Debug.Log($"[DungeonManager] TryMoveToNextRoom called. Exit direction: {exitDirection}");

        if (isTransitioning)
        {
            Debug.Log("[DungeonManager] Transition blocked because isTransitioning = true");
            return;
        }

        StartCoroutine(TransitionToRoomCoroutine(exitDirection));
    }

    private IEnumerator TransitionToRoomCoroutine(DoorDirection exitDirection)
    {
        isTransitioning = true;

        Debug.Log($"[DungeonManager] Starting transition from node: {currentNode?.uniqueNodeID} using door: {exitDirection}");

        RoomNode nextNode = dungeonLayout.CreateNextRoom(currentNode, exitDirection);

        if(nextNode == null)
        {
            Debug.LogWarning("[DungeonManager] Transition cancelled. nextNode is null.");
            isTransitioning = false;
            yield break;
        }

        DoorDirection entryDirection = DungeonLayout.GetOppositeDirection(exitDirection);

        Debug.Log($"[DungeonManager] Next node: {nextNode.uniqueNodeID} | Entry direction will be: {entryDirection}");

        if (currentRoomInstance != null) 
        {
            Debug.Log($"[DungeonManager] Hiding current room instance: {currentRoomInstance.name}");
            currentRoomInstance.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(transitionDelay);

        currentNode = nextNode;
        EnterRoom(currentNode, entryDirection);

        Debug.Log($"[DungeonManager] Transition complete. Current node is now: {currentNode.uniqueNodeID}");

        isTransitioning = false;
    }

    private void EnterRoom(RoomNode node, DoorDirection? entryDirection)
    {
        if (node == null || node.information == null || node.information.prefab == null)
        {
            Debug.LogError("[DungeonManager] EnterRoom failed: node/information/prefab invalid.");
            return;
        }

        Debug.Log($"[DungeonManager] Entering node: {node.uniqueNodeID} | Room: {node.information.roomID}");

        if (node.spawnedInstance == null)
        {
            Debug.Log($"[DungeonManager] Instantiating NEW room instance for node: {node.uniqueNodeID}");

            node.spawnedInstance = Instantiate(node.information.prefab, Vector3.zero, Quaternion.identity, roomParent);
            node.spawnedInstance.ConfigureDoors(node);
        }
        else
        {
            Debug.Log($"[DungeonManager] Reusing EXISTING room instance for node: {node.uniqueNodeID}");
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
            Debug.LogWarning("[DungeonManager] MovePlayerToCorrectSpawn aborted: player or currentRoomInstance is null.");
            return;
        }

        Transform spawnPoint;

        if (entryDirection.HasValue)
        {
            spawnPoint = currentRoomInstance.GetSpawnPointFromEntry(entryDirection.Value);
            Debug.Log($"[DungeonManager] Looking for entry spawn from direction: {entryDirection.Value}");
        }
        else
        {
            spawnPoint = currentRoomInstance.DefaultSpawnPoint;
            Debug.Log("[DungeonManager] Using default spawn point.");
        }
        if (spawnPoint != null) 
        {
            Debug.Log($"[DungeonManager] Moving player to spawn point: {spawnPoint.name} | Position: {spawnPoint.position}");
            player.position = spawnPoint.position;
        }
    }
}
