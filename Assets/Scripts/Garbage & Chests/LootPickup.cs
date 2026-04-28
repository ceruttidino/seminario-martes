using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LootPickup : MonoBehaviour
{
    [Header("Loot Settings")]
    [SerializeField] private float pickupDelay = 0.6f;
    [SerializeField] private bool autoPickup = false;
    [SerializeField] private LootItem lootItem;

    private bool canBePickedUp = false;
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
        {
            canBePickedUp = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!canBePickedUp) return;
        if (!other.CompareTag("Player")) return;

        bool collected = ApplyLoot(other.gameObject);

        if (collected)
        {
            Debug.Log($"Jugador recogió: {lootItem.itemName}");

            PickupEffect effect = GetComponent<PickupEffect>();
            if (effect != null)
                effect.OnPickup();
            else
                Destroy(gameObject);
        }
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
                if (health == null) return false;
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
}