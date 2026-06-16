using System.Collections.Generic;
using UnityEngine;

public class BreakableTrash : MonoBehaviour
{
    [System.Serializable]
    private struct DropEntry
    {
        public GameObject prefab;
        public LootItem lootItem;
    }

    [Header("Configuración")]
    [SerializeField] private TrashType trashType = TrashType.CommonBag;
    [SerializeField] private int maxItems = 2;

    [Header("Loot (Heart / Key / Scrap)")]
    [SerializeField] private DropEntry heartDrop;
    [SerializeField] private DropEntry keyDrop;
    [SerializeField] private DropEntry scrapDrop;

    [Header("Upgrade (solo GreenContainer)")]
    [Tooltip("Chance de que ademas del loot basico aparezca un buff upgrade")]
    [Range(0f, 100f)]
    [SerializeField] private float upgradeDropChance = 10f;
    [SerializeField] private List<ObjectBuffSO> possibleUpgrades;
    [SerializeField] private GameObject buffPickupPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float minSpawnRadius = 0.6f;
    [SerializeField] private float maxSpawnRadius = 1.3f;

    [Header("Visuales")]
    [SerializeField] private SpriteRenderer closedSprite;
    [SerializeField] private GameObject openVisual;

    private Collider2D hitCollider;
    private int currentHits = 0;
    private int maxHits;
    private bool isDestroyed = false;

    private Vector3 openVisualInitialLocalPosition;

    private void Awake()
    {
        if (closedSprite == null)
            closedSprite = GetComponent<SpriteRenderer>();

        hitCollider = GetComponent<Collider2D>();

        if (openVisual != null)
        {
            openVisualInitialLocalPosition = openVisual.transform.localPosition;
            openVisual.SetActive(false);
        }
    }

    private void Start()
    {
        maxHits = (trashType == TrashType.GreenContainer) ? 3 : 2;
    }

    public void TakeHit(float damage = 1f)
    {
        if (isDestroyed) return;

        currentHits++;

        if (currentHits >= maxHits)
            DestroyTrash();
    }

    private void DestroyTrash()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        Transform roomParent = FindRoomParent();

        ShowOpenVisual();
        SpawnLoot(roomParent);
    }

    private void ShowOpenVisual()
    {
        if (closedSprite != null)
            closedSprite.enabled = false;

        if (openVisual != null)
        {
            openVisual.transform.localPosition = openVisualInitialLocalPosition;
            openVisual.SetActive(true);
        }

        if (hitCollider != null)
            hitCollider.enabled = false;
    }

    private void SpawnLoot(Transform roomParent)
    {
        // armar lista con los drops disponibles
        var options = new List<DropEntry>();
        if (heartDrop.prefab != null) options.Add(heartDrop);
        if (keyDrop.prefab != null) options.Add(keyDrop);
        if (scrapDrop.prefab != null) options.Add(scrapDrop);

        if (options.Count > 0)
        {
            // elegir UN tipo y spawnear 1-maxItems del mismo, sin mezclar
            DropEntry chosen = options[Random.Range(0, options.Count)];
            int amount = Random.Range(1, maxItems + 1);

            for (int i = 0; i < amount; i++)
                SpawnPickup(chosen.prefab, chosen.lootItem, roomParent);
        }

        // solo GreenContainer tiene chance de dropear un buff upgrade
        if (trashType == TrashType.GreenContainer)
            TrySpawnUpgrade(roomParent);
    }

    private void TrySpawnUpgrade(Transform roomParent)
    {
        if (buffPickupPrefab == null) return;
        if (possibleUpgrades == null || possibleUpgrades.Count == 0) return;
        if (Random.Range(0f, 100f) > upgradeDropChance) return;

        ObjectBuffSO chosen = BuffPool.PickRandom(possibleUpgrades);
        if (chosen == null) return;

        Vector3 pos = GetRandomSpawnPos();
        GameObject pickup = Instantiate(buffPickupPrefab, pos, Quaternion.identity);

        UpgradePickup upgradePickup = pickup.GetComponent<UpgradePickup>();
        if (upgradePickup != null)
            upgradePickup.SetUpgrade(chosen);

        if (chosen.icon != null)
        {
            SpriteRenderer sr = pickup.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.sprite = chosen.icon;
        }

        if (roomParent != null)
            pickup.transform.SetParent(roomParent, true);
    }

    private void SpawnPickup(GameObject prefab, LootItem lootItem, Transform roomParent)
    {
        if (prefab == null) return;

        GameObject spawned = Instantiate(prefab, GetRandomSpawnPos(), Quaternion.identity);

        LootPickup pickup = spawned.GetComponent<LootPickup>();
        if (pickup != null && lootItem != null)
            pickup.SetLootItem(lootItem);

        if (roomParent != null)
            spawned.transform.SetParent(roomParent, true);
    }

    private Vector3 GetRandomSpawnPos()
    {
        Vector2 dir = Random.insideUnitCircle.normalized;
        float dist = Random.Range(minSpawnRadius, maxSpawnRadius);
        return transform.position + new Vector3(dir.x, dir.y, 0f) * dist;
    }

    private Transform FindRoomParent()
    {
        RoomInstance room = GetComponentInParent<RoomInstance>();
        if (room != null) return room.transform;

        Transform current = transform.parent;
        while (current != null)
        {
            if (current.name.Contains("Room") || current.name.Contains("room") ||
                current.GetComponent("RoomInstance") != null)
                return current;
            current = current.parent;
        }

        return null;
    }

    public int GetCurrentHits() => currentHits;
}
