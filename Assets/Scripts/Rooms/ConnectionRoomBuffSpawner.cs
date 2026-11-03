using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Put this ONLY on the room prefabs that should offer a buff (e.g. connection rooms).
/// On Start it replaces the placeholder pickup with a real pickup holding a random buff.
/// </summary>
[DisallowMultipleComponent]
public class ConnectionRoomBuffSpawner : MonoBehaviour
{
    [SerializeField] private List<ObjectBuffSO> possibleBuffs;
    [SerializeField] private GameObject buffPickupPrefab;

    [Tooltip("Placeholder CHILD inside this room prefab. Marks the spawn point and gets removed. " +
             "Must be an object in the room hierarchy, NOT the prefab asset from the Project window.")]
    [SerializeField] private GameObject existingPickup;

    [SerializeField] private bool debugLogs = true;

    private bool spawned;

    // Start (not Awake): lets the dungeon generator finish placing/configuring the room first.
    private void Start()
    {
        SpawnRoomBuff();
    }

    private void SpawnRoomBuff()
    {
        if (spawned) return;

        if (buffPickupPrefab == null) { Warn("buffPickupPrefab is not assigned."); return; }
        if (possibleBuffs == null || possibleBuffs.Count == 0) { Warn("possibleBuffs is empty."); return; }

        // Guard: the pickup template must be a prefab ASSET (Project window), not an object inside the room.
        if (buffPickupPrefab.scene.IsValid())
        {
            Warn("buffPickupPrefab points to an object inside the room. Assign a prefab from the Project window.");
            return;
        }

        // Guard: if someone dragged the prefab ASSET instead of the child, Destroy() would throw.
        bool placeholderIsSceneObject = existingPickup != null && existingPickup.scene.IsValid();
        if (existingPickup != null && !placeholderIsSceneObject)
            Warn("existingPickup points to a prefab asset, not a child of the room. Using the spawner position.");

        Vector3 spawnPos = placeholderIsSceneObject ? existingPickup.transform.position : transform.position;

        if (placeholderIsSceneObject)
        {
            existingPickup.SetActive(false); // Destroy is deferred to end of frame; kill its collider/visuals now
            Destroy(existingPickup);
        }

        ObjectBuffSO chosen = BuffPool.PickRandom(possibleBuffs);
        if (chosen == null) { Warn("BuffPool.PickRandom returned null."); return; }

        RoomInstance room = GetComponentInParent<RoomInstance>();
        Transform roomParent = room != null ? room.transform : transform;

        GameObject pickup = Instantiate(buffPickupPrefab, spawnPos, Quaternion.identity, roomParent);
        spawned = true;

        UpgradePickup upgradePickup = pickup.GetComponent<UpgradePickup>();
        if (upgradePickup != null)
            upgradePickup.SetUpgrade(chosen);
        else
            Warn("buffPickupPrefab has no UpgradePickup on its root.");

        if (chosen.icon != null)
        {
            SpriteRenderer sr = pickup.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.sprite = chosen.icon;
        }

        if (debugLogs)
            Debug.Log($"[BuffSpawner] {name}: spawned '{chosen.name}' at {spawnPos} (parent: {roomParent.name})", this);
    }

    private void Warn(string msg)
    {
        if (debugLogs) Debug.LogWarning($"[BuffSpawner] {name}: {msg}", this);
    }
}