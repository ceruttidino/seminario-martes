using System.Collections.Generic;
using UnityEngine;

public class MinimapUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform mapContainer;
    [SerializeField] private MinimapRoomIcon roomIconPrefab;

    [Header("Layout")]
    [SerializeField] private Vector2 roomSpacing = new Vector2(35f, 35f);

    private DungeonManager dungeonManager;
    private readonly Dictionary<RoomNode, MinimapRoomIcon> roomIcons = new Dictionary<RoomNode, MinimapRoomIcon>();

    public void Initialize(DungeonManager manager)
    {
        dungeonManager = manager;
        BuildMap();
        RefreshMap();
    }

    public void BuildMap()
    {
        List<RoomNode> allRooms = dungeonManager.GetAllRooms();
        Vector2 offset = CalculateCenterOffset(allRooms);

        // reutiliza iconos existentes en vez de destruir y recrear
        var pool = new Queue<MinimapRoomIcon>(roomIcons.Values);
        roomIcons.Clear();

        foreach (RoomNode node in allRooms)
        {
            MinimapRoomIcon icon = pool.Count > 0 ? pool.Dequeue() : Instantiate(roomIconPrefab, mapContainer);
            icon.gameObject.SetActive(true);

            RectTransform rect = icon.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(
                node.gridPosition.x * roomSpacing.x,
                node.gridPosition.y * roomSpacing.y
            ) - offset;

            roomIcons.Add(node, icon);
        }

        // desactiva los que sobraron
        foreach (MinimapRoomIcon leftover in pool)
            leftover.gameObject.SetActive(false);
    }

    public void RefreshMap()
    {
        foreach (var pair in roomIcons)
        {
            bool showAsAdjacent = IsAdjacentToVisited(pair.Key);
            pair.Value.Setup(pair.Key, showAsAdjacent);
        }
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

    private Vector2 CalculateCenterOffset(List<RoomNode> rooms)
    {
        if (rooms == null || rooms.Count == 0)
            return Vector2.zero;

        Vector2 sum = Vector2.zero;
        foreach (RoomNode node in rooms)
            sum += new Vector2(node.gridPosition.x, node.gridPosition.y);

        Vector2 center = sum / rooms.Count;
        return new Vector2(center.x * roomSpacing.x, center.y * roomSpacing.y);
    }

    private void ClearMap()
    {
        foreach (var icon in roomIcons.Values)
            icon.gameObject.SetActive(false);

        roomIcons.Clear();
    }
}
