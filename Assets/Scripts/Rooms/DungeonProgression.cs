using UnityEngine;

// CAMBIADO (archivo completo): ya NO es un componente (sacalo del GameObject del DungeonManager).
// Solo lleva la cuenta de rooms generadas en el nivel actual y decide cuándo toca Shop y Boss.
// El largo de cada nivel viene del DungeonFloorConfig (roomsBeforeShop / roomsBeforeBoss).
public class DungeonProgression
{
    private int currentNormalRoomsCount;
    private bool shopSpawned;
    private bool bossSpawned;
    private int roomsBeforeShop = 7;
    private int roomsBeforeBoss = 5;
    private int extraRoomsAfterBoss;     // NUEVO
    private int normalRoomsAfterBoss;    // NUEVO

    /// <summary>
    /// NUEVO: false cuando el nivel ya llegó a su tope (salió el boss y se usaron las rooms extra).
    /// Antes no existía y el nivel crecía para siempre mientras se exploraba.
    /// </summary>
    public bool CanCreateNormalRoom => !bossSpawned || normalRoomsAfterBoss < extraRoomsAfterBoss;

    /// <summary>Se llama al empezar cada nivel con los valores de su FloorConfig.</summary>
    public void Configure(int beforeShop, int beforeBoss, bool hasShop, int extraAfterBoss = 0) // CAMBIADO: + extraAfterBoss
    {
        currentNormalRoomsCount = 0;
        normalRoomsAfterBoss = 0;                         // NUEVO
        extraRoomsAfterBoss = Mathf.Max(0, extraAfterBoss); // NUEVO
        bossSpawned = false;
        roomsBeforeShop = Mathf.Max(1, beforeShop);
        roomsBeforeBoss = Mathf.Max(1, beforeBoss);

        // Nivel sin Shop: se saltea esa etapa y el boss sale después del largo total del nivel
        shopSpawned = !hasShop;
        if (!hasShop)
            roomsBeforeBoss = roomsBeforeShop + roomsBeforeBoss;
    }

    public RoomType GetNextRoomType()
    {
        if (!shopSpawned)
            return currentNormalRoomsCount >= roomsBeforeShop ? RoomType.Shop : RoomType.Normal;

        if (!bossSpawned)
            return currentNormalRoomsCount >= roomsBeforeBoss ? RoomType.Boss : RoomType.Normal;

        return RoomType.Normal;
    }

    /// <summary>true cuando ya se generaron suficientes rooms y el boss todavía no apareció.</summary>
    public bool IsBossDue => shopSpawned && !bossSpawned && currentNormalRoomsCount >= roomsBeforeBoss;

    public void RegisterGeneratedRoom(RoomType roomType)
    {
        switch (roomType)
        {
            case RoomType.Normal:
                if (bossSpawned) normalRoomsAfterBoss++; // CAMBIADO: después del boss cuenta contra el tope
                else currentNormalRoomsCount++;
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

    public bool HasSpawnedShop() => shopSpawned;
    public bool HasSpawnedBoss() => bossSpawned;
}