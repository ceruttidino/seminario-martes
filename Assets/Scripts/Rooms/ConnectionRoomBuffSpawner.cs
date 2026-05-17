using System.Collections.Generic;
using UnityEngine;

// Adjuntar al Connection_Room. Reemplaza el pickup fijo por un buff random no repetido.
public class ConnectionRoomBuffSpawner : MonoBehaviour
{
    [SerializeField] private List<ObjectBuffSO> possibleBuffs;
    [SerializeField] private GameObject buffPickupPrefab;

    // Arrastrar el UpgradePickup_Health que ya esta en el prefab del ConnectionRoom
    [SerializeField] private GameObject existingPickup;

    private void Awake()
    {
        SpawnRoomBuff();
    }

    private void SpawnRoomBuff()
    {
        if (buffPickupPrefab == null || possibleBuffs == null || possibleBuffs.Count == 0) return;

        Vector3 spawnPos = existingPickup != null ? existingPickup.transform.position : transform.position;

        if (existingPickup != null)
            Destroy(existingPickup);

        ObjectBuffSO chosen = BuffPool.PickRandom(possibleBuffs);
        if (chosen == null) return;

        GameObject pickup = Instantiate(buffPickupPrefab, spawnPos, Quaternion.identity);

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
