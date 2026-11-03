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

    [Header("Vista")]
    [Tooltip("Escala del contenido del mapa (1 = tamano normal). MinimapExpander la baja en el minimapa chico (siempre visible) para que entren mas salas y la sube al expandirlo.")]
    [SerializeField] private float viewScale = 1f;

    private Vector2 originalIconSize;
    private Vector2 iconSize;
    private Vector2 roomSpacing;
    private RoomNode currentNode;

    private DungeonManager dungeonManager;
    private readonly Dictionary<RoomNode, MinimapRoomIcon> roomIcons = new Dictionary<RoomNode, MinimapRoomIcon>();

    public MinimapRoomIcon RoomIconPrefab => roomIconPrefab;

    public float ViewScale => viewScale;

    /// <summary>Cambia el zoom del contenido y vuelve a centrar la sala actual.</summary>
    public void SetViewScale(float scale)
    {
        viewScale = Mathf.Max(0.05f, scale);

        if (mapContainer != null)
            mapContainer.localScale = new Vector3(viewScale, viewScale, 1f);

        Recenter();
    }

    /// <summary>Vuelve a centrar el mapa en la sala actual (con el zoom actual).</summary>
    public void Recenter()
    {
        if (currentNode != null && mapContainer != null && roomSpacing != Vector2.zero)
            CenterOnRoom(currentNode);
    }

    public void Configure(RectTransform container, MinimapRoomIcon iconPrefab)
    {
        mapContainer = container;
        if (iconPrefab != null)
            roomIconPrefab = iconPrefab;
    }

    public void Initialize(DungeonManager manager)
    {
        dungeonManager = manager;
        if (roomIconPrefab == null || mapContainer == null) return;

        originalIconSize = roomIconPrefab.GetComponent<RectTransform>().sizeDelta;
        iconSize = originalIconSize * iconScale;
        roomSpacing = iconSize + roomGap;
        mapContainer.localScale = new Vector3(viewScale, viewScale, 1f);

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
        currentNode = room;

        Vector2 roomPos = new Vector2(
            room.gridPosition.x * roomSpacing.x,
            room.gridPosition.y * roomSpacing.y
        );

        // El contenedor se escala desde su centro: la posicion de la sala escala igual.
        mapContainer.anchoredPosition = -roomPos * viewScale;
    }

    private bool IsAdjacentToVisited(RoomNode node)
    {
        foreach (var pair in node.neighboors)
        {
            RoomNode other = pair.Value;
            if (other == null || !other.hasBeenVisited)
                continue;

            // El spawn no revela las salas de al lado: al empezar un piso solo se ve la sala actual.
            if (other.uniqueNodeID == "Start")
                continue;
            if (other.information != null && other.information.type == RoomType.Start)
                continue;

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
