using TMPro;
using UnityEngine;

// Shows the current floor on screen ("Floor 1", "Floor 2", ...).
// Reads DungeonManager.Instance.CurrentFloor and only rewrites the text when the floor changes.
public class FloorLabelUI : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    [Tooltip("{0} = current floor, {1} = total floors, {2} = floor name from the FloorConfig.\nExamples: \"Floor {0}\", \"Floor {0}/{1}\", \"{2} - Floor {0}\"")]
    [SerializeField] private string format = "Floor {0}";

    private int lastFloor = -1;

    private void Awake()
    {
        if (label == null)
            label = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        DungeonManager dungeon = DungeonManager.Instance;
        if (dungeon == null || label == null)
            return;

        int floor = dungeon.CurrentFloor;
        if (floor == lastFloor)
            return;

        lastFloor = floor;

        string floorName = dungeon.CurrentFloorConfig != null ? dungeon.CurrentFloorConfig.floorName : "";
        label.text = string.Format(format, floor, dungeon.MaxFloor, floorName);
    }
}