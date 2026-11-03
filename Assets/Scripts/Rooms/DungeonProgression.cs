using UnityEngine;

/// <summary>
/// OBSOLETO: antes decidia sobre la marcha cuando aparecian la Shop y la Boss. Ahora el piso ENTERO se genera al empezar
/// (DungeonLayout) y esto ya no interviene en el layout. Se conserva porque DungeonManager lo tiene asignado en la escena.
/// </summary>
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
            if (shopSpawned && currentNormalRoomsCount >= normalRoomsBeforeBoss)
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

    public bool HasSpawnedShop()
    {
        return shopSpawned;
    }

    public bool HasSpawnedBoss()
    {
        return bossSpawned;
    }

    public void ResetProgression()
    {
        currentNormalRoomsCount = 0;
        shopSpawned = false;
        bossSpawned = false;
    }

}
