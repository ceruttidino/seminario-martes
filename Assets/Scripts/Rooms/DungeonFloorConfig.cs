using System.Collections.Generic;
using UnityEngine;

// NUEVO (archivo completo)
// Configuración de UN nivel/piso del dungeon: qué rooms pueden salir en ese nivel.
// Se crea un asset por nivel (Create > Dungeon > Floor Config) y se ponen en orden
// en la lista "Floors" del DungeonManager (elemento 0 = nivel 1, elemento 1 = nivel 2, etc.).
//
// Cualquier campo que dejes vacío usa el valor general del DungeonManager,
// así solo configurás lo que cambia entre niveles.
// Una misma room puede estar en varios niveles: arrastrala a la lista de cada uno.
[CreateAssetMenu(fileName = "FloorConfig", menuName = "Dungeon/Floor Config")]
public class DungeonFloorConfig : ScriptableObject
{
    [Tooltip("Solo para identificarlo (aparece en la consola al generar el piso). Ej: 'Nivel 1 - Cloacas'")]
    public string floorName = "Nivel";

    [Header("Rooms normales")]
    [Tooltip("Rooms que pueden salir en este nivel. Vacío = se usa el filtro viejo por el campo 'level' de cada RoomInformation.")]
    public List<RoomInformation> normalRooms = new List<RoomInformation>();

    [Header("Rooms especiales (vacío = usa la del DungeonManager)")]
    public RoomInformation startRoom;
    public RoomInformation shopRoom;

    [Tooltip("Bosses posibles en este nivel. Si hay varios, sale uno al azar; nunca se repite un boss que ya salió en un nivel anterior (si hay alternativa).")]
    public List<RoomInformation> bossRooms = new List<RoomInformation>();

    [Tooltip("Challenge rooms posibles en este nivel.")]
    public List<RoomInformation> challengeRooms = new List<RoomInformation>();
    [Tooltip("Tildalo si en este nivel NO tiene que aparecer ninguna challenge room")]
    public bool noChallengeRoom = false;

    [Tooltip("Room de buff (Connection Room) de este nivel.")]
    public RoomInformation buffRoom;
    [Tooltip("Tildalo si en este nivel NO tiene que aparecer la buff room")]
    public bool noBuffRoom = false;

    [Header("Largo del nivel (opcional)")]
    [Tooltip("Si está tildado, este nivel ignora el escalado automático de DungeonProgression y usa estos valores")]
    public bool overrideRoomCounts = false;
    [Min(1)] public int roomsBeforeShop = 7;
    [Min(1)] public int roomsBeforeBoss = 5;

    // ---------------- Helpers (los usa DungeonManager) ----------------

    public bool HasNormalRooms => CountValid(normalRooms) > 0;
    public bool HasBossRooms => CountValid(bossRooms) > 0;
    public bool HasChallengeRooms => CountValid(challengeRooms) > 0;

    private static int CountValid(List<RoomInformation> list)
    {
        if (list == null) return 0;

        int count = 0;
        foreach (RoomInformation room in list)
            if (room != null && room.prefab != null) count++;
        return count;
    }

    // Avisa en la consola si pusiste una room en la lista equivocada (ej: una Shop en Normal Rooms)
    private void OnValidate()
    {
        CheckType(normalRooms, RoomType.Normal, "Normal Rooms");
        CheckType(bossRooms, RoomType.Boss, "Boss Rooms");
        CheckType(challengeRooms, RoomType.Challenge, "Challenge Rooms");

        if (shopRoom != null && shopRoom.type != RoomType.Shop)
            Debug.LogWarning($"[FloorConfig] {name}: '{shopRoom.name}' está en Shop Room pero su tipo es {shopRoom.type}.", this);

        if (startRoom != null && startRoom.type != RoomType.Start)
            Debug.LogWarning($"[FloorConfig] {name}: '{startRoom.name}' está en Start Room pero su tipo es {startRoom.type}.", this);
    }

    private void CheckType(List<RoomInformation> list, RoomType expected, string listName)
    {
        if (list == null) return;

        foreach (RoomInformation room in list)
        {
            if (room != null && room.type != expected)
                Debug.LogWarning($"[FloorConfig] {name}: '{room.name}' está en {listName} pero su tipo es {room.type}.", this);
        }
    }
}
