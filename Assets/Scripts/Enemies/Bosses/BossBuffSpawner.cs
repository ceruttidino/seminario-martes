using System.Collections.Generic;
using UnityEngine;

// Adjuntar al boss. Escucha OnDeath y spawnea un buff random no repetido durante la run.
public class BossBuffSpawner : MonoBehaviour
{
    [SerializeField] private List<ObjectBuffSO> possibleBuffs;
    [SerializeField] private GameObject buffPickupPrefab;
    [SerializeField] private Transform spawnPoint;

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

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        GameObject pickup = Instantiate(buffPickupPrefab, pos, Quaternion.identity);

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
}
