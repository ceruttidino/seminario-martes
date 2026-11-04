using UnityEngine;
using UnityEngine.UI;

public class MinimapRoomIcon : MonoBehaviour
{
    [SerializeField] private Image roomImage;
    [SerializeField] private GameObject playerIndicator;

    [Header("Iconos")]
    [SerializeField] private Sprite visitedNormalSprite;
    [SerializeField] private Sprite hiddenNormalSprite;
    [SerializeField] private Sprite buffSprite;
    [SerializeField] private Sprite shopSprite;
    [SerializeField] private Sprite spawnSprite;
    [SerializeField] private Sprite bossSprite;
    [SerializeField] private Sprite challengeSprite;

    public void Setup(RoomNode node, bool showAsAdjacent)
    {
        if (!node.hasBeenVisited && !showAsAdjacent)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (playerIndicator != null)
            playerIndicator.SetActive(node.isCurrentRoom);

        if (roomImage == null)
            return;

        roomImage.sprite = SpriteFor(node, showAsAdjacent);
        roomImage.color = Color.white;
        roomImage.preserveAspect = true;
    }

    private Sprite SpriteFor(RoomNode node, bool showAsAdjacent)
    {
        if (IsShopRoom(node))
            return shopSprite;

        if (IsBossRoom(node))
            return bossSprite;

        if (node.information != null && node.information.IsBuffRoom)
            return buffSprite;

        if (IsChallengeRoom(node))
            return challengeSprite;

        if (IsStartRoom(node))
            return spawnSprite;

        if (!node.hasBeenVisited && showAsAdjacent)
            return hiddenNormalSprite;

        return visitedNormalSprite;
    }

    private static bool IsShopRoom(RoomNode node)
    {
        return node.information != null && node.information.type == RoomType.Shop;
    }

    private static bool IsBossRoom(RoomNode node)
    {
        return node.information != null && node.information.type == RoomType.Boss;
    }

    private static bool IsChallengeRoom(RoomNode node)
    {
        return node.information != null && node.information.type == RoomType.Challenge;
    }

    private static bool IsStartRoom(RoomNode node)
    {
        if (node.uniqueNodeID == "Start")
            return true;

        return node.information != null && node.information.type == RoomType.Start;
    }
}
