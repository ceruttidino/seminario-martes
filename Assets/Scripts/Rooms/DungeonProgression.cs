using UnityEngine;

public class DungeonProgression : MonoBehaviour
{
    [Header("Limits")]
    [SerializeField] private int normalRoomsBeforeShop = 2;
    [SerializeField] private int normalRoomsBeforeBoss = 1;

    private int currentNormalRoomsCount = 0;
    private bool shopSpawned = false;
    private bool bossSpawned = false;

    public RoomType GetNextRoomType()
    {

        if (!shopSpawned)
        {
            if (currentNormalRoomsCount >= normalRoomsBeforeShop)
            {
                return RoomType.Shop;
            }
            return RoomType.Normal;
        }

        if (!bossSpawned)
        {
            if (!shopSpawned)
            {
                return RoomType.Normal;
            }

            if (currentNormalRoomsCount >= normalRoomsBeforeBoss)
            {
                return RoomType.Boss;
            }
            return RoomType.Normal;
        }

        return RoomType.Normal;
    }

    public void RegisterGeneratedRoom(RoomType roomType)
    {
        switch (roomType)
        {
            case RoomType.Normal:
                currentNormalRoomsCount++;
                break;

            case RoomType.Shop:
                shopSpawned = true;
                currentNormalRoomsCount = 0;
                break;

            case RoomType.Boss:
                bossSpawned = true;
                break;
        }

    }

}
