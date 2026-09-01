using System.Collections.Generic;
using UnityEngine;

// Adjuntar al boss. Escucha OnDeath y spawnea un buff random no repetido durante la run.
public class BossBuffSpawner : MonoBehaviour
{
    [SerializeField] private List<ObjectBuffSO> possibleBuffs;
    [SerializeField] private GameObject buffPickupPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Vector2 offsetFromVictoryDoor = new Vector2(-2.5f, 0f);

    private void Awake()
    {
        EnemyHealth health = GetComponent<EnemyHealth>();
        if (health != null)
            health.OnDeath += SpawnRandomBuff;
    }

    private void SpawnRandomBuff()
    {
        if (buffPickupPrefab == null || possibleBuffs == null || possibleBuffs.Count == 0)
            return;

        ObjectBuffSO chosen = BuffPool.PickRandom(possibleBuffs);
        if (chosen == null) return;

        Vector3 pos = GetLootPosition();
        Transform roomParent = GetComponentInParent<RoomInstance>()?.transform ?? transform;
        GameObject pickup = Instantiate(buffPickupPrefab, pos, Quaternion.identity, roomParent);

        UpgradePickup upgradePickup = pickup.GetComponent<UpgradePickup>();
        if (upgradePickup != null)
            upgradePickup.SetUpgrade(chosen);

        if (chosen.icon != null)
        {
            SpriteRenderer sr = pickup.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
                sr.sprite = chosen.icon;
        }
    }

    private Vector3 GetLootPosition()
    {
        Vector3 avoid = GetVictoryDoorPosition();
        Vector3[] candidates =
        {
            avoid + (Vector3)offsetFromVictoryDoor,
            avoid + new Vector3(2.5f, 0f, 0f),
            avoid + new Vector3(0f, 2f, 0f),
            avoid + new Vector3(0f, -2f, 0f)
        };

        foreach (Vector3 candidate in candidates)
        {
            if (Vector2.Distance(candidate, avoid) >= 1.5f)
                return candidate;
        }

        return avoid + (Vector3)offsetFromVictoryDoor;
    }

    private Vector3 GetVictoryDoorPosition()
    {
        RoomInstance room = GetComponentInParent<RoomInstance>();
        if (room != null)
        {
            VictoryDoor door = room.GetComponentInChildren<VictoryDoor>(true);
            if (door != null)
                return door.transform.position;

            return room.transform.position;
        }

        if (spawnPoint != null)
            return spawnPoint.position + (Vector3)offsetFromVictoryDoor;

        return transform.position + (Vector3)offsetFromVictoryDoor;
    }
}
