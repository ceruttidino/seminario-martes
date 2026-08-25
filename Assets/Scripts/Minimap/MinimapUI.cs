using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MinimapUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform mapContainer;
    [SerializeField] private MinimapRoomIcon roomIconPrefab;

    [Header("Layout")]
    [SerializeField] private float iconScale = 1.3f;
    [SerializeField] private Vector2 roomGap = new Vector2(10f, 10f);

    private Vector2 originalIconSize;
    private Vector2 iconSize;
    private Vector2 roomSpacing;

    private DungeonManager dungeonManager;
    private readonly Dictionary<RoomNode, MinimapRoomIcon> roomIcons = new Dictionary<RoomNode, MinimapRoomIcon>();

    public void Initialize(DungeonManager manager)
    {
        dungeonManager = manager;

        originalIconSize = roomIconPrefab.GetComponent<RectTransform>().sizeDelta;
        iconSize = originalIconSize * iconScale;
        roomSpacing = iconSize + roomGap;

        BuildMap();
        RefreshMap();
    }

    public void BuildMap()
    {
        List<RoomNode> allRooms = dungeonManager.GetAllRooms();

        var pool = new Queue<MinimapRoomIcon>(roomIcons.Values);
        roomIcons.Clear();

        foreach (RoomNode node in allRooms)
        {
            MinimapRoomIcon icon = pool.Count > 0 ? pool.Dequeue() : Instantiate(roomIconPrefab, mapContainer);
            icon.gameObject.SetActive(true);

            RectTransform rect = icon.GetComponent<RectTransform>();
            rect.sizeDelta = iconSize;

            rect.anchoredPosition = new Vector2(
                node.gridPosition.x * roomSpacing.x,
                node.gridPosition.y * roomSpacing.y
            );

            roomIcons.Add(node, icon);
        }

        // desactiva los que sobraron
        foreach (MinimapRoomIcon leftover in pool)
            leftover.gameObject.SetActive(false);
    }

    public void RefreshMap()
    {
        RoomNode currentRoom = null;

        foreach (var pair in roomIcons)
        {
            bool showAsAdjacent = IsAdjacentToVisited(pair.Key);
            pair.Value.Setup(pair.Key, showAsAdjacent);

            if (pair.Key.isCurrentRoom)
                currentRoom = pair.Key;
        }

        if (currentRoom != null)
            CenterOnRoom(currentRoom);
    }

    private void CenterOnRoom(RoomNode room)
    {
        Vector2 roomPos = new Vector2(
            room.gridPosition.x * roomSpacing.x,
            room.gridPosition.y * roomSpacing.y
        );

        mapContainer.anchoredPosition = -roomPos;
    }

    private bool IsAdjacentToVisited(RoomNode node)
    {
        foreach (var pair in node.neighboors)
        {
            if (pair.Value != null && pair.Value.hasBeenVisited)
                return true;
        }
        return false;
    }

    private void ClearMap()
    {
        foreach (var icon in roomIcons.Values)
            icon.gameObject.SetActive(false);

        roomIcons.Clear();
    }

}
