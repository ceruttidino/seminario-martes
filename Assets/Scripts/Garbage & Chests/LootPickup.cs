using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LootPickup : MonoBehaviour
{
    [Header("Loot Settings")]
    [SerializeField] private float pickupDelay = 0.6f;
    [SerializeField] private LootItem lootItem;

    [SerializeField] private AudioSource pickupSfx;

    private bool canBePickedUp;
    private bool collected;
    private float spawnTime;
    private int rolledScrapAmount = 1;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
        EnsurePickupSize(col);
    }

    private void Start()
    {
        spawnTime = Time.time;

        if (GetComponent<PickupEffect>() == null)
            gameObject.AddComponent<PickupEffect>();
    }

    private void Update()
    {
        if (!canBePickedUp && Time.time > spawnTime + pickupDelay)
            canBePickedUp = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryCollect(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryCollect(other);
    }

    public void SetLootItem(LootItem item)
    {
        lootItem = item;
    }

    private void TryCollect(Collider2D other)
    {
        if (collected || !canBePickedUp || lootItem == null)
            return;

        GameObject player = ResolvePlayer(other);
        if (player == null)
            return;

        if (!ApplyLoot(player))
            return;

        collected = true;
        NotifyInventoryPickup();
        AudioManager.PlayLoot(lootItem.lootType, lootItem.upgradeSO, pickupSfx != null ? pickupSfx.clip : null);

        PickupEffect effect = GetComponent<PickupEffect>();
        if (effect != null)
            effect.OnPickup();
        else
            Destroy(gameObject);
    }

    private static GameObject ResolvePlayer(Collider2D other)
    {
        if (other == null)
            return null;

        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player"))
            return other.attachedRigidbody.gameObject;

        if (other.CompareTag("Player"))
            return other.gameObject;

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        return health != null ? health.gameObject : null;
    }

    private bool ApplyLoot(GameObject player)
    {
        switch (lootItem.lootType)
        {
            case LootType.Scrap:
                PlayerScrap scrap = player.GetComponent<PlayerScrap>();
                if (scrap == null) return false;
                rolledScrapAmount = lootItem.RollScrapAmount();
                scrap.AddScrap(rolledScrapAmount);
                return true;

            case LootType.Health:
                PlayerHealth health = player.GetComponent<PlayerHealth>();
                PlayerPoisonStatus poison = player.GetComponent<PlayerPoisonStatus>();
                if (health == null) return false;
                if (health.IsHealthFull && (poison == null || !poison.IsPoisoned))
                    return false;

                int heal = lootItem.healthAmount > 0 ? lootItem.healthAmount : 2;
                health.PlayerHeal(heal);
                return true;

            case LootType.Key:
                PlayerKeys keys = player.GetComponent<PlayerKeys>();
                if (keys == null) return false;
                keys.AddKeys(Mathf.Max(1, lootItem.keyAmount));
                return true;

            case LootType.Upgrade:
                PlayerUpgradeManager upgradeManager = player.GetComponent<PlayerUpgradeManager>();
                if (upgradeManager == null || lootItem.upgradeSO == null) return false;
                return upgradeManager.CollectUpgrade(lootItem.upgradeSO);
        }

        return false;
    }

    private static void EnsurePickupSize(Collider2D col)
    {
        const float minWorld = 0.5f;
        if (col is BoxCollider2D box)
        {
            Vector2 world = Vector2.Scale(box.size, col.transform.lossyScale);
            if (world.x < minWorld || world.y < minWorld)
            {
                float sx = Mathf.Abs(col.transform.lossyScale.x);
                float sy = Mathf.Abs(col.transform.lossyScale.y);
                box.size = new Vector2(
                    Mathf.Max(box.size.x, sx > 0.01f ? minWorld / sx : minWorld),
                    Mathf.Max(box.size.y, sy > 0.01f ? minWorld / sy : minWorld));
            }
        }
        else if (col is CircleCollider2D circle)
        {
            float scale = Mathf.Max(Mathf.Abs(col.transform.lossyScale.x), Mathf.Abs(col.transform.lossyScale.y));
            if (circle.radius * scale < minWorld * 0.5f)
                circle.radius = scale > 0.01f ? (minWorld * 0.5f) / scale : minWorld;
        }
    }

    private void NotifyInventoryPickup()
    {
        if (TabMenuUI.Instance == null || lootItem == null) return;
        if (lootItem.lootType != LootType.Scrap && lootItem.lootType != LootType.Key)
            return;

        Sprite icon = lootItem.icon;
        if (icon == null && lootItem.upgradeSO != null)
            icon = lootItem.upgradeSO.icon;

        string label = lootItem.lootType == LootType.Key ? "Lockpick"
            : lootItem.lootType == LootType.Scrap ? "Scrap"
            : lootItem.lootType == LootType.Health ? "Heart"
            : lootItem.upgradeSO != null && !string.IsNullOrEmpty(lootItem.upgradeSO.upgradeName)
                ? lootItem.upgradeSO.upgradeName
            : !string.IsNullOrEmpty(lootItem.itemName) ? lootItem.itemName
            : "Item";

        int amount = lootItem.lootType == LootType.Key
            ? Mathf.Max(1, lootItem.keyAmount)
            : Mathf.Max(1, rolledScrapAmount);

        TabMenuUI.Instance.ShowPickup(icon, label, amount);
    }
}
