using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LootPickup : MonoBehaviour
{
    [Header("Loot Settings")]
    [SerializeField] private float pickupDelay = 0.6f;
    [SerializeField] private LootItem lootItem;

    [SerializeField] private AudioSource pickupSfx;

    private bool canBePickedUp = false;
    private bool collected;
    private float spawnTime;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
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

    private void TryCollect(Collider2D other)
    {
        if (collected || !canBePickedUp) return;
        if (!other.CompareTag("Player")) return;

        bool applied = ApplyLoot(other.gameObject);

        if (!applied)
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

    private bool ApplyLoot(GameObject player)
    {
        if (lootItem == null) return false;

        switch (lootItem.lootType)
        {
            case LootType.Scrap:
                PlayerScrap scrap = player.GetComponent<PlayerScrap>();
                if (scrap == null) return false;
                scrap.AddScrap(lootItem.scrapAmount);
                return true;

            case LootType.Health:
                PlayerHealth health = player.GetComponent<PlayerHealth>();
                if (health == null || health.IsHealthFull) return false;
                health.PlayerHeal(lootItem.healthAmount);
                return true;

            case LootType.Key:
                PlayerKeys keys = player.GetComponent<PlayerKeys>();
                if (keys == null) return false;
                keys.AddKeys(lootItem.keyAmount);
                return true;

            case LootType.Upgrade:
                PlayerUpgradeManager upgradeManager = player.GetComponent<PlayerUpgradeManager>();
                if (upgradeManager == null || lootItem.upgradeSO == null) return false;
                upgradeManager.CollectUpgrade(lootItem.upgradeSO);
                return true;
        }
        return false;
    }

    public void SetLootItem(LootItem item)
    {
        lootItem = item;
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
            : Mathf.Max(1, lootItem.scrapAmount);

        TabMenuUI.Instance.ShowPickup(icon, label, amount);
    }
}
