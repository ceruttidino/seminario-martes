using System.Collections.Generic;
using UnityEngine;

// Adjuntar al boss. Escucha OnDeath y spawnea un buff random no repetido durante la run.
public class BossBuffSpawner : MonoBehaviour
{
    [SerializeField] private List<ObjectBuffSO> possibleBuffs;
    [SerializeField] private GameObject buffPickupPrefab;
    [SerializeField] private Transform spawnPoint;

    // Buffs ya usados durante la sesion actual; static para que persista entre salas
    private static readonly HashSet<ObjectBuffSO> usedBuffs = new HashSet<ObjectBuffSO>();

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

        List<ObjectBuffSO> available = possibleBuffs.FindAll(b => b != null && !usedBuffs.Contains(b));

        if (available.Count == 0)
            return;

        ObjectBuffSO chosen = available[Random.Range(0, available.Count)];
        usedBuffs.Add(chosen);

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        GameObject pickup = Instantiate(buffPickupPrefab, pos, Quaternion.identity);

        UpgradePickup upgradePickup = pickup.GetComponent<UpgradePickup>();
        if (upgradePickup != null)
            upgradePickup.SetUpgrade(chosen);

        // Aplica el sprite del buff al SpriteRenderer del pickup
        if (chosen.icon != null)
        {
            SpriteRenderer sr = pickup.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
                sr.sprite = chosen.icon;
        }
    }

    // Util para tests y debugging: limpiar el historial entre runs
    public static void ResetUsedBuffs()
    {
        usedBuffs.Clear();
    }
}
