using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class MinimapUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform mapContainer;
    [SerializeField] private MinimapRoomIcon roomIconPrefab;

    [Header("Layout")]
    [SerializeField] private Vector2 roomSpacing = new Vector2(35f, 35f);

    private DungeonManager dungeonManager;
    private Dictionary<RoomNode, MinimapRoomIcon> roomIcons = new Dictionary<RoomNode, MinimapRoomIcon>();

    public void Initialize(DungeonManager manager)
    {
        dungeonManager = manager;
        BuildMap();
        RefreshMap();
    }

    public void BuildMap()
    {
        ClearMap();

        List<RoomNode> allRooms = dungeonManager.GetAllRooms();

        Vector2 offset = CalculateCenterOffset(allRooms);

        foreach (RoomNode node in allRooms) 
        {
            MinimapRoomIcon icon = Instantiate(roomIconPrefab, mapContainer);

            RectTransform rect = icon.GetComponent<RectTransform>();
            Vector2 position = new Vector2(node.gridPosition.x * roomSpacing.x, node.gridPosition.y * roomSpacing.y);

            rect.anchoredPosition = position - offset;

            roomIcons.Add(node, icon);
        }
    }

    public void RefreshMap()
    {
        foreach(var pair in roomIcons)
        {
            RoomNode node = pair.Key;
            MinimapRoomIcon icon = pair.Value;

            bool showAsAdjacent = IsAdjacentToVisited(node);
            icon.Setup(node, showAsAdjacent);
        }
    }

    private bool IsAdjacentToVisited(RoomNode node)
    {
        foreach (var pair in node.neighboors)
        {
            if (pair.Value != null && pair.Value.hasBeenVisited)
            {
                return true;
            }
        }

        return false;
    }

    private Vector2 CalculateCenterOffset(List<RoomNode> rooms)
    {
        if (rooms == null || rooms.Count == 0)
        {
            return Vector2.zero;
        }

        Vector2 sum = Vector2.zero;

        foreach (RoomNode node in rooms)
        {
            sum += new Vector2(node.gridPosition.x, node.gridPosition.y);
        }

        Vector2 center = sum / rooms.Count;
        return new Vector2(center.x * roomSpacing.x, center.y * roomSpacing.y);
    }

    private void ClearMap()
    {
        foreach (Transform child in mapContainer)
        {
            Destroy(child.gameObject);
        }

        roomIcons.Clear();
    }
}
