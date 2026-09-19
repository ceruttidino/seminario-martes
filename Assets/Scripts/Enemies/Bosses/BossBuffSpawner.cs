using System.Collections.Generic;
using UnityEngine;

public class BossBuffSpawner : MonoBehaviour
{
    [SerializeField] private List<ObjectBuffSO> possibleBuffs;
    [SerializeField] private GameObject buffPickupPrefab;
    [SerializeField] private Transform spawnPoint; // ancla en el centro del room

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

        Vector3 pos = GetSpawnPosition();
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

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoint != null)
            return spawnPoint.position;

        RoomInstance room = GetComponentInParent<RoomInstance>();
        if (room != null)
            return room.transform.position;

        return transform.position;
    }
}