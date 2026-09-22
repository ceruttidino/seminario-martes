using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomInformation", menuName = "Dungeon/Room Information")]
public class RoomInformation : ScriptableObject
{
    [Header("Info")]
    public string roomID;
    public RoomType type = RoomType.Normal;
    public RoomInstance prefab;

    [Header("Progression")]
    [Tooltip("A qué nivel/piso pertenece esta room normal. Se ignora para Shop/Boss, que son fijas.")]
    public int level = 1;

    [Header("Doors")]
    public List<DoorDirection> availableDoors = new List<DoorDirection>();

    [Header("Challenge")]
    [Tooltip("Id del challenge. Usar las constantes de ChallengeRunState (Supercontainer, StealTheItem).")]
    public string challengeId;
    [Tooltip("Logo que lleva la puerta que da a esta challenge room (ej: la pieza de scrap).")]
    public Sprite challengeDoorSprite;

    public bool HasDoor(DoorDirection direction)
    {
        return availableDoors.Contains(direction);
    }
}
