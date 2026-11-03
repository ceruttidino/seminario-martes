using UnityEngine;

/// <summary>
/// Decide sobre la marcha cuándo aparecen Shop y Boss. Cada piso es más largo que el anterior;
/// el primero ya sale más extenso que el valor viejo de 2-3 rooms.
/// </summary>
public class DungeonProgression : MonoBehaviour
{
    [Header("Floor 1 (base)")]
    [SerializeField] private int normalRoomsBeforeShop = 7;
    [SerializeField] private int normalRoomsBeforeBoss = 5;

    [Header("Scaling")]
    [Tooltip("Rooms extra (antes de shop y después de shop) que se suman en cada piso siguiente.")]
    [SerializeField] private int extraRoomsPerFloor = 2;

    private int currentNormalRoomsCount;
    private bool shopSpawned;
    private bool bossSpawned;
    private int roomsBeforeShop;
    private int roomsBeforeBoss;

    public void PrepareFloor(int floor)
    {
        ResetProgression();

        int extra = Mathf.Max(0, floor - 1) * Mathf.Max(0, extraRoomsPerFloor);
        roomsBeforeShop = Mathf.Max(4, normalRoomsBeforeShop + extra);
        roomsBeforeBoss = Mathf.Max(3, normalRoomsBeforeBoss + extra);

        // El primer piso era el más corto: se le da un piso extra de margen respecto del valor viejo.
        if (floor <= 1)
        {
            roomsBeforeShop = Mathf.Max(roomsBeforeShop, 7);
            roomsBeforeBoss = Mathf.Max(roomsBeforeBoss, 5);
        }
    }

    // NUEVO: un nivel con FloorConfig puede pisar el largo calculado automáticamente
    public void OverrideRoomCounts(int beforeShop, int beforeBoss)
    {
        roomsBeforeShop = Mathf.Max(1, beforeShop);
        roomsBeforeBoss = Mathf.Max(1, beforeBoss);
    }

    public RoomType GetNextRoomType()
    {
        if (!shopSpawned)
        {
            if (currentNormalRoomsCount >= roomsBeforeShop)
                return RoomType.Shop;

            return RoomType.Normal;
        }

        if (!bossSpawned)
        {
            if (shopSpawned && currentNormalRoomsCount >= roomsBeforeBoss)
                return RoomType.Boss;

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
        roomsBeforeShop = Mathf.Max(4, normalRoomsBeforeShop);
        roomsBeforeBoss = Mathf.Max(3, normalRoomsBeforeBoss);
    }
}