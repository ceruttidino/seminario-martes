using UnityEngine;
using UnityEngine.UI;

public class MinimapRoomIcon : MonoBehaviour
{
    [SerializeField] private Image roomImage;
    [SerializeField] private GameObject playerIndicator;

    [Header("Colors")]
    [SerializeField] private Color currentRoomColor = Color.white;
    [SerializeField] private Color normalVisitedColor = new Color(0.6f, 0.65f, 0.65f);
    [SerializeField] private Color adjacentRoomColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
    [SerializeField] private Color startRoomColor = Color.green;
    [SerializeField] private Color shopRoomColor = Color.yellow;
    [SerializeField] private Color bossRoomColor = Color.red;
    [SerializeField] private Color buffRoomColor = Color.blue;

    public void Setup(RoomNode node, bool showAsAdjacent)
    {
        if (!node.hasBeenVisited && !showAsAdjacent)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        playerIndicator.SetActive(node.isCurrentRoom);

        if (IsShopRoom(node))
        {
            roomImage.color = shopRoomColor;
            return;
        }

        if (IsBossRoom(node))
        {
            roomImage.color = bossRoomColor;
            return;
        }

        if (IsBuffConnectionRoom(node))
        {
            roomImage.color = buffRoomColor;
            return;
        }

        if (!node.hasBeenVisited && showAsAdjacent)
        {
            roomImage.color = adjacentRoomColor;
            return;
        }

        if (node.isCurrentRoom)
        {
            roomImage.color = currentRoomColor;
            return;
        }

        switch (node.information.type)
        {
            case RoomType.Start:
                roomImage.color = startRoomColor;
                break;

            default:
                roomImage.color = normalVisitedColor;
                break;
        }
    }

    private static bool IsShopRoom(RoomNode node)
    {
        return node.information != null && node.information.type == RoomType.Shop;
    }

    private static bool IsBossRoom(RoomNode node)
    {
        return node.information != null && node.information.type == RoomType.Boss;
    }

    private static bool IsBuffConnectionRoom(RoomNode node)
    {
        if (node.information == null)
            return false;

        string id = node.information.roomID;
        if (string.IsNullOrEmpty(id))
            id = node.information.name;

        return id == "Connection_Room_4" || id == "ConnectionRoom4";
    }
}
