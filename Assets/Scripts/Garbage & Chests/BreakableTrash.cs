using System.Collections;
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
    private HitShake hitShake;
    private int currentHits = 0;
    private int maxHits;
    private bool isDestroyed = false;

    private Vector3 openVisualInitialLocalPosition;

    private Bounds trashBounds;

    private void Awake()
    {
        if (closedSprite == null)
            closedSprite = GetComponent<SpriteRenderer>();

        hitCollider = GetComponent<Collider2D>();
        if (hitCollider != null)
            trashBounds = hitCollider.bounds;

        if (openVisual != null)
        {
            openVisualInitialLocalPosition = openVisual.transform.localPosition;
            openVisual.SetActive(false);
        }

        hitShake = GetComponent<HitShake>();
        if (hitShake == null)
            hitShake = gameObject.AddComponent<HitShake>();
    }

    private void Start()
    {
        maxHits = (trashType == TrashType.GreenContainer) ? 3 : 2;
    }

    public void TakeHit(float damage = 1f)
    {
        if (isDestroyed) return;

        currentHits++;

        if (hitShake != null)
            hitShake.Play();

        if (currentHits >= maxHits)
            StartCoroutine(BreakAfterShake());
    }

    private IEnumerator BreakAfterShake()
    {
        isDestroyed = true;
        yield return new WaitForSeconds(0.08f);
        DestroyTrash();
    }

    private void DestroyTrash()
    {
        Transform roomParent = FindRoomParent();

        ShowOpenVisual();
        SpawnLoot(roomParent);

        RoomInstance room = GetComponentInParent<RoomInstance>();
        room?.InvalidatePathGrid();
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
    }

    private void SpawnLoot(Transform roomParent)
    {
        var options = new List<DropEntry>();
        if (heartDrop.prefab != null) options.Add(heartDrop);
        if (keyDrop.prefab != null) options.Add(keyDrop);
        if (scrapDrop.prefab != null) options.Add(scrapDrop);

        var lootToSpawn = new List<DropEntry>();
        if (options.Count > 0)
        {
            DropEntry chosen = options[Random.Range(0, options.Count)];
            int amount = Random.Range(1, maxItems + 1);
            for (int i = 0; i < amount; i++)
                lootToSpawn.Add(chosen);
        }

        ObjectBuffSO chosenUpgrade = null;
        if (trashType == TrashType.GreenContainer)
            chosenUpgrade = TryRollUpgrade();

        int totalCount = lootToSpawn.Count + (chosenUpgrade != null ? 1 : 0);
        if (totalCount == 0) return;

        Vector3[] positions = GetSpawnPositions(totalCount);
        int index = 0;

        foreach (DropEntry entry in lootToSpawn)
            SpawnPickup(entry.prefab, entry.lootItem, roomParent, positions[index++]);

        if (chosenUpgrade != null)
            SpawnUpgrade(chosenUpgrade, roomParent, positions[index++]);
    }

    private ObjectBuffSO TryRollUpgrade()
    {
        if (buffPickupPrefab == null) return null;
        if (possibleUpgrades == null || possibleUpgrades.Count == 0) return null;
        if (Random.Range(0f, 100f) > upgradeDropChance) return null;
        return BuffPool.PickRandom(possibleUpgrades);
    }

    private void SpawnUpgrade(ObjectBuffSO chosen, Transform roomParent, Vector3 targetPos)
    {
        GameObject pickup = Instantiate(buffPickupPrefab, transform.position, Quaternion.identity);

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

        LaunchPickup(pickup, targetPos);
    }

    private void SpawnPickup(GameObject prefab, LootItem lootItem, Transform roomParent, Vector3 targetPos)
    {
        if (prefab == null) return;

        GameObject spawned = Instantiate(prefab, transform.position, Quaternion.identity);

        LootPickup pickup = spawned.GetComponent<LootPickup>();
        if (pickup != null && lootItem != null)
            pickup.SetLootItem(lootItem);

        if (roomParent != null)
            spawned.transform.SetParent(roomParent, true);

        LaunchPickup(spawned, targetPos);
    }

    private void LaunchPickup(GameObject spawned, Vector3 targetPos)
    {
        LootPopMover mover = spawned.GetComponent<LootPopMover>();
        if (mover == null)
            mover = spawned.AddComponent<LootPopMover>();

        mover.Launch(targetPos, hitCollider);
    }

    private static readonly Vector2[] PreferredLootDirs =
    {
        Vector2.down,
        new Vector2(-1f, -0.35f).normalized,
        new Vector2(1f, -0.35f).normalized,
        Vector2.left,
        Vector2.right,
        new Vector2(-0.75f, -0.75f).normalized,
        new Vector2(0.75f, -0.75f).normalized
    };

    private Vector3[] GetSpawnPositions(int count)
    {
        Vector3[] positions = new Vector3[count];
        int startDir = Random.Range(0, PreferredLootDirs.Length);

        for (int i = 0; i < count; i++)
        {
            Vector2 dir = PreferredLootDirs[(startDir + i) % PreferredLootDirs.Length];
            positions[i] = FindReachablePosition(dir);
        }

        return positions;
    }

    private Vector3 FindReachablePosition(Vector2 preferredDir)
    {
        float minRadius = GetSafeMinRadius();
        float maxRadius = Mathf.Max(minRadius + 0.15f, maxSpawnRadius);

        for (float dist = minRadius; dist <= maxRadius + 1.6f; dist += 0.2f)
        {
            Vector3 candidate = transform.position + (Vector3)(preferredDir * dist);
            if (!IsPositionBlocked(candidate))
                return candidate;
        }

        for (int i = 0; i < PreferredLootDirs.Length; i++)
        {
            Vector2 dir = PreferredLootDirs[i];
            for (float dist = minRadius; dist <= maxRadius + 2.2f; dist += 0.2f)
            {
                Vector3 candidate = transform.position + (Vector3)(dir * dist);
                if (!IsPositionBlocked(candidate))
                    return candidate;
            }
        }

        return transform.position + (Vector3)(Vector2.down * (minRadius + 0.4f));
    }

    private bool IsPositionBlocked(Vector3 worldPos)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, 0.18f);
        foreach (Collider2D hit in hits)
        {
            if (hit == null || hit.isTrigger) continue;
            if (hit.GetComponent<LootPickup>() != null) continue;
            if (hit.GetComponent<UpgradePickup>() != null) continue;
            return true;
        }

        return false;
    }

    private float GetSafeMinRadius()
    {
        float halfDiagonal = new Vector2(trashBounds.extents.x, trashBounds.extents.y).magnitude;
        return Mathf.Max(minSpawnRadius, halfDiagonal + 0.15f);
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
