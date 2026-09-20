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
            hitShake.Play(0.16f, 0.12f);

        if (currentHits >= maxHits)
            StartCoroutine(BreakAfterShake());
    }

    private IEnumerator BreakAfterShake()
    {
        isDestroyed = true;
        yield return new WaitForSeconds(0.14f);
        DestroyTrash();
    }

    private void DestroyTrash()
    {
        Transform roomParent = FindRoomParent();

        ShowOpenVisual();
        BurstTrash();
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

    private void BurstTrash()
    {
        Sprite[] sprites = CollectDebrisSprites();
        if (sprites.Length == 0) return;

        if (trashType == TrashType.CommonBag)
            TrashBurst.ExplodeAllAround(transform.position, sprites, Random.Range(14, 19));
        else
            TrashBurst.ExplodeToward(transform.position, -GetLidDirection(), sprites, Random.Range(12, 17), 72f);
    }

    private Sprite[] CollectDebrisSprites()
    {
        var sprites = new System.Collections.Generic.List<Sprite>();
        AddSprite(sprites, closedSprite != null ? closedSprite.sprite : null);

        if (openVisual != null)
        {
            SpriteRenderer openRenderer = openVisual.GetComponent<SpriteRenderer>();
            if (openRenderer != null)
                AddSprite(sprites, openRenderer.sprite);
        }

        AddPrefabSprite(sprites, heartDrop.prefab);
        AddPrefabSprite(sprites, keyDrop.prefab);
        AddPrefabSprite(sprites, scrapDrop.prefab);
        return sprites.ToArray();
    }

    private static void AddPrefabSprite(System.Collections.Generic.List<Sprite> sprites, GameObject prefab)
    {
        if (prefab == null) return;
        SpriteRenderer renderer = prefab.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null)
            AddSprite(sprites, renderer.sprite);
    }

    private static void AddSprite(System.Collections.Generic.List<Sprite> sprites, Sprite sprite)
    {
        if (sprite != null && !sprites.Contains(sprite))
            sprites.Add(sprite);
    }

    private Vector2 GetLidDirection()
    {
        Vector2 dir = transform.up;
        if (dir.sqrMagnitude < 0.01f)
            dir = Vector2.up;
        return dir.normalized;
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
            int amount = Random.Range(1, maxItems + 1);
            for (int i = 0; i < amount; i++)
                lootToSpawn.Add(options[Random.Range(0, options.Count)]);
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

    private Vector3[] GetSpawnPositions(int count)
    {
        Vector3[] positions = new Vector3[count];
        bool allAround = trashType == TrashType.CommonBag;
        Vector2 lidDir = GetLidDirection();
        Vector2 ejectDir = -lidDir;

        for (int i = 0; i < count; i++)
        {
            Vector2 dir;
            if (allAround)
            {
                float angle = (360f / Mathf.Max(1, count)) * i + Random.Range(-20f, 20f);
                dir = Quaternion.Euler(0f, 0f, angle) * Vector2.up;
            }
            else
            {
                float t = count == 1 ? 0f : (i / (float)(count - 1)) * 2f - 1f;
                float cone = 38f;
                float angle = Vector2.SignedAngle(Vector2.up, ejectDir) + t * cone + Random.Range(-8f, 8f);
                dir = Quaternion.Euler(0f, 0f, angle) * Vector2.up;
            }

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

        for (int i = 0; i < 8; i++)
        {
            Vector2 dir = Quaternion.Euler(0f, 0f, i * 45f) * Vector2.up;
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
