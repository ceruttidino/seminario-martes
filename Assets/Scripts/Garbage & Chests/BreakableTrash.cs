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
    [Tooltip("Distancia mínima a la que caen los objetos del contenedor")]
    [SerializeField] private float minSpawnRadius = 0.8f;
    [Tooltip("Distancia máxima a la que caen los objetos del contenedor")]
    [SerializeField] private float maxSpawnRadius = 1.3f;
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

        // partículas de destrucción o tacho abierto (mantiene la rotación del padre)
        if (destroyParticlePrefab != null)
        {
            GameObject visual = Instantiate(destroyParticlePrefab, transform.position, transform.rotation);

            Transform parent = FindRoomParent();
            if (parent != null)
                visual.transform.SetParent(parent, true);
        }

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

        // Evitamos que caigan muy lejos o fuera de las paredes
        float distance = Random.Range(minSpawnRadius, maxSpawnRadius);

        // Mantenemos la posición Z original para que los sprites no queden escondidos detrás del fondo
        Vector3 spawnPos = transform.position + new Vector3(randomDir.x, randomDir.y, 0f) * distance;

        // instanciar el objeto
        GameObject spawned = Instantiate(lootItem.prefab, spawnPos, Quaternion.identity);

        LootPickup pickup = spawned.GetComponent<LootPickup>();
        if (pickup != null)
        {
            pickup.SetLootItem(lootItem);
        }

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
        RoomInstance room = GetComponentInParent<RoomInstance>();
        if (room != null)
        {
            return room.transform;
        }

        Transform current = transform.parent;

        while (current != null)
        {
            // busca por nombre o por componente
            if (current.name.Contains("Room") || current.name.Contains("room") || current.GetComponent("RoomInstance") != null)
            {
                return current;
            }
            current = current.parent;
        }

        return null; // si no encuentra, no parenta
    }

    public int GetCurrentHits() => currentHits;
}