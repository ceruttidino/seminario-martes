using System.Collections.Generic;
using System.Text;
using UnityEngine;

// CAMBIADO (archivo completo)
// ÚNICA fuente de verdad de un nivel: qué rooms salen en ese nivel y qué tan largo es.
// Un asset por nivel (Create > Dungeon > Floor Config), en orden en la lista "Floors" del DungeonManager.
// Ya no hay valores "generales" ni fallbacks: lo que no está acá, no sale en este nivel.
// Una misma room puede estar en varios niveles: arrastrala a la lista de cada uno.
[CreateAssetMenu(fileName = "Floor_", menuName = "Dungeon/Floor Config")]
public class DungeonFloorConfig : ScriptableObject
{
    [Tooltip("Para identificarlo en la consola. Ej: 'Nivel 1 - Cloacas'")]
    public string floorName = "Nivel 1";

    [Header("Obligatorias")]
    [Tooltip("Room donde arranca el nivel (tipo Start)")]
    public RoomInformation startRoom;
    [Tooltip("Rooms normales que pueden salir en este nivel (tipo Normal)")]
    public List<RoomInformation> normalRooms = new List<RoomInformation>();
    [Tooltip("Bosses posibles (tipo Boss). Si hay varios sale uno al azar, sin repetir uno que ya salió en un nivel anterior (si hay alternativa).")]
    public List<RoomInformation> bossRooms = new List<RoomInformation>();

    [Header("Opcionales (vacío = no aparece en este nivel)")]
    [Tooltip("Shop del nivel (tipo Shop)")]
    public RoomInformation shopRoom;
    [Tooltip("Challenge rooms posibles (tipo Challenge). Sale una por nivel.")]
    public List<RoomInformation> challengeRooms = new List<RoomInformation>();
    [Tooltip("Room de buff / Connection Room (tipo Normal). Sale una por nivel.")]
    public RoomInformation buffRoom;

    [Header("Largo del nivel")]
    [Tooltip("Rooms normales que se generan antes de que pueda aparecer la Shop")]
    [Min(1)] public int roomsBeforeShop = 7;
    [Tooltip("Rooms normales que se generan después de la Shop antes de que pueda aparecer el Boss")]
    [Min(1)] public int roomsBeforeBoss = 5;
    [Tooltip("Rooms normales que todavía se pueden generar DESPUÉS de que aparece el Boss (ramas para explorar). 0 = el nivel deja de crecer apenas sale el Boss.")]
    [Min(0)] public int extraRoomsAfterBoss = 2; // NUEVO: tope del nivel (antes crecía para siempre)

    /// <summary>Revisa que el nivel esté bien armado. Devuelve false y la lista de problemas si algo falta.</summary>
    public bool Validate(out string problems)
    {
        StringBuilder sb = new StringBuilder();

        CheckSingle(startRoom, RoomType.Start, "Start Room", required: true, sb);
        CheckList(normalRooms, RoomType.Normal, "Normal Rooms", required: true, sb);
        CheckList(bossRooms, RoomType.Boss, "Boss Rooms", required: true, sb);
        CheckSingle(shopRoom, RoomType.Shop, "Shop Room", required: false, sb);
        CheckList(challengeRooms, RoomType.Challenge, "Challenge Rooms", required: false, sb);
        CheckSingle(buffRoom, RoomType.Normal, "Buff Room", required: false, sb);

        problems = sb.ToString();
        return problems.Length == 0;
    }

    private static void CheckSingle(RoomInformation room, RoomType expected, string label, bool required, StringBuilder sb)
    {
        if (room == null)
        {
            if (required) sb.AppendLine($"- Falta asignar '{label}'.");
            return;
        }

        CheckRoom(room, expected, label, sb);
    }

    private static void CheckList(List<RoomInformation> list, RoomType expected, string label, bool required, StringBuilder sb)
    {
        int valid = 0;

        if (list != null)
        {
            foreach (RoomInformation room in list)
            {
                if (room == null) continue;
                CheckRoom(room, expected, label, sb);
                if (room.prefab != null) valid++;
            }
        }

        if (required && valid == 0)
            sb.AppendLine($"- '{label}' está vacía.");
    }

    private static void CheckRoom(RoomInformation room, RoomType expected, string label, StringBuilder sb)
    {
        if (room.prefab == null)
            sb.AppendLine($"- '{room.name}' (en {label}) no tiene prefab.");

        if (room.type != expected)
            sb.AppendLine($"- '{room.name}' está en {label} pero su tipo es {room.type} (debería ser {expected}).");

        if (room.prefab != null && room.Doors.Count == 0)
            sb.AppendLine($"- '{room.name}' no tiene puertas.");
    }

    private void OnValidate()
    {
        if (!Validate(out string problems))
            Debug.LogWarning($"[FloorConfig] '{name}' tiene cosas para revisar:\n{problems}", this);
    }
}