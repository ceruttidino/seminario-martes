using System.Collections.Generic;
using UnityEngine;

// CAMBIADO (archivo completo)
// Describe UNA room: qué es (tipo), qué prefab usa y qué puertas tiene.
// Ya NO dice en qué nivel aparece: eso se decide SOLO en los DungeonFloorConfig.
//
// Las puertas se leen automáticamente de las RoomDoor del prefab (lista "Room Doors" del RoomInstance),
// así nunca puede pasar que el asset diga que hay una puerta que el prefab no tiene.
[CreateAssetMenu(fileName = "Room_", menuName = "Dungeon/Room Information")]
public class RoomInformation : ScriptableObject
{
    public enum DoorSource
    {
        AutoFromPrefab, // las puertas salen del prefab (recomendado)
        Manual          // solo para usar MENOS puertas de las que tiene el prefab
    }

    [Header("Room")]
    [Tooltip("Identificador para la consola. Vacío = nombre del asset.")]
    public string roomID;
    public RoomType type = RoomType.Normal;
    public RoomInstance prefab;

    [Header("Puertas")]
    [Tooltip("Auto: usa todas las RoomDoor del prefab. Manual: usa la lista de abajo (por ejemplo, si el prefab tiene 4 puertas pero esta room solo debería usar 2).")]
    public DoorSource doorSource = DoorSource.AutoFromPrefab;
    [Tooltip("Solo se usa en modo Manual. Cada puerta de acá tiene que existir en el prefab.")]
    [SerializeField] private List<DoorDirection> manualDoors = new List<DoorDirection>();
    [Tooltip("Solo lectura: las puertas que va a usar el generador")]
    [SerializeField] private string detectedDoors;

    [Header("Challenge (solo si el tipo es Challenge)")]
    [Tooltip("Id del challenge. Usar las constantes de ChallengeRunState (Supercontainer, StealTheItem).")]
    public string challengeId;
    [Tooltip("Logo que lleva la puerta que da a esta challenge room (ej: la pieza de scrap).")]
    public Sprite challengeDoorSprite;

    [System.NonSerialized] private List<DoorDirection> cachedDoors;

    /// <summary>Nombre para logs: el roomID, o el nombre del asset si está vacío.</summary>
    public string ID => string.IsNullOrEmpty(roomID) ? name : roomID;

    /// <summary>Puertas que puede usar el generador.</summary>
    public IReadOnlyList<DoorDirection> Doors => GetDoors();

    public bool HasDoor(DoorDirection direction)
    {
        return GetDoors().Contains(direction);
    }

    // NUEVO: para armar rooms por código (lo usa DungeonGenerationVerify). Pasa la room a modo Manual.
    public void SetManualDoors(params DoorDirection[] doors)
    {
        doorSource = DoorSource.Manual;
        manualDoors = new List<DoorDirection>(doors);
        cachedDoors = null;
    }

    private List<DoorDirection> GetDoors()
    {
        if (cachedDoors == null)
            cachedDoors = BuildDoorList();
        return cachedDoors;
    }

    private List<DoorDirection> BuildDoorList()
    {
        List<DoorDirection> result = new List<DoorDirection>();

        if (doorSource == DoorSource.Manual)
        {
            foreach (DoorDirection door in manualDoors)
                if (!result.Contains(door)) result.Add(door);
            return result;
        }

        if (prefab != null)
        {
            foreach (DoorDirection door in prefab.PrefabDoorDirections)
                if (!result.Contains(door)) result.Add(door);
        }

        return result;
    }

    private void OnEnable()
    {
        cachedDoors = null;
    }

    private void OnValidate()
    {
        cachedDoors = null;
        detectedDoors = GetDoors().Count > 0 ? string.Join(", ", GetDoors()) : "(ninguna)";

        if (prefab == null)
            return;

        if (GetDoors().Count == 0)
            Debug.LogWarning($"[RoomInformation] '{name}' no tiene puertas: el generador nunca la va a poder conectar. Revisá la lista 'Room Doors' del prefab.", this);

        if (doorSource == DoorSource.Manual)
        {
            List<DoorDirection> prefabDoors = new List<DoorDirection>(prefab.PrefabDoorDirections);
            foreach (DoorDirection door in manualDoors)
            {
                if (!prefabDoors.Contains(door))
                    Debug.LogWarning($"[RoomInformation] '{name}' usa la puerta {door} pero el prefab '{prefab.name}' no tiene una RoomDoor en esa dirección.", this);
            }
        }

        if (type == RoomType.Challenge && string.IsNullOrEmpty(challengeId))
            Debug.LogWarning($"[RoomInformation] '{name}' es Challenge pero no tiene Challenge Id.", this);

        if (type == RoomType.Boss && prefab.GetComponentInChildren<BossBase>(true) == null)
            Debug.LogWarning($"[RoomInformation] '{name}' es Boss pero el prefab '{prefab.name}' no tiene ningún BossBase adentro.", this);
    }
}