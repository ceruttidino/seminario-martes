using UnityEngine;
using System.Collections;

public class BreakableTrash : MonoBehaviour
{
    [Header("Configuración del Cofre")]
    [SerializeField] private TrashType trashType = TrashType.CommonBag;
    [SerializeField] private int maxHits = 3;
    private int currentHits = 0;

    [Header("Loot Settings")]
    [SerializeField] private LootTableSO lootTable;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius = 1.3f;           // distancia en la que spawnean los objetos
    [SerializeField] private float spawnDelayBetweenItems = 0.07f; // delay entre cada objeto

    [Header("Visuales (Opcional)")]
    [SerializeField] private GameObject destroyParticlePrefab;

    private bool isDestroyed = false;

    private void Start()
    {
        if (lootTable == null)
            Debug.LogWarning($"[{gameObject.name}] No tiene LootTable asignado.");

        // ajuste automático de golpes
        maxHits = (trashType == TrashType.GreenContainer) ? 3 : 2;
    }

    public void TakeHit(float damage = 1f)
    {
        if (isDestroyed) return;

        currentHits++;
        Debug.Log($"[{gameObject.name}] Golpe {currentHits}/{maxHits}");

        if (currentHits >= maxHits)
        {
            DestroyTrash();
        }
    }

    private void DestroyTrash()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        // partículas de destrucción
        if (destroyParticlePrefab != null)
            Instantiate(destroyParticlePrefab, transform.position, Quaternion.identity);

        if (lootTable != null)
        {
            LootItem[] loots = lootTable.GetRandomLoot(trashType);
            StartCoroutine(SpawnLootWithDelay(loots));
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] No tiene LootTable asignado.");
        }

        Destroy(gameObject, 0.4f);
    }

    private IEnumerator SpawnLootWithDelay(LootItem[] loots)
    {
        foreach (LootItem loot in loots)
        {
            if (loot != null && loot.prefab != null)
            {
                SpawnSingleLoot(loot);
                yield return new WaitForSeconds(spawnDelayBetweenItems);
            }
        }
    }

    private void SpawnSingleLoot(LootItem lootItem)
    {
        if (lootItem == null || lootItem.prefab == null) return;

        // psición en círculo alrededor del cofre
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        float distance = Random.Range(0.9f, spawnRadius);
        Vector2 spawnPos = (Vector2)transform.position + randomDir * distance;

        // instanciar el objeto
        GameObject spawned = Instantiate(lootItem.prefab, spawnPos, Quaternion.identity);

        if (spawned.TryGetComponent<SpriteRenderer>(out SpriteRenderer sr))
        {
            sr.transform.localScale = Vector3.one;     // todos los objetos salen del mismo tamaño
        }

        Transform roomParent = FindRoomParent();
        if (roomParent != null)
        {
            spawned.transform.SetParent(roomParent, true);
        }
        else
        {
            Debug.LogWarning($"No se encontró Room Parent para {lootItem.itemName}. Puede quedarse al cambiar de room.");
        }

        Debug.Log($"Spawneado: {lootItem.itemName}");
    }

    // busca automáticamente el padre de la habitación actual
    private Transform FindRoomParent()
    {
        Transform current = transform.parent;

        while (current != null)
        {
            // busca por nombre o por componente
            if (current.name.Contains("Room") || current.name.Contains("room") || current.GetComponent("Room") != null)
            {
                return current;
            }
            current = current.parent;
        }

        return null; // si no encuentra, no parenta
    }

    public int GetCurrentHits() => currentHits;
}