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

    public void Setup(RoomNode node, bool showAsAdjacent)
    {
        if (!node.hasBeenVisited && !showAsAdjacent)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        playerIndicator.SetActive(node.isCurrentRoom);

        if (node.information != null &&
            (node.information.type == RoomType.Shop || node.information.type == RoomType.Boss))
        {
            roomImage.color = node.information.type == RoomType.Shop
                ? shopRoomColor
                : bossRoomColor;
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
}
