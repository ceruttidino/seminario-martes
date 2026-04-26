using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LootPickup : MonoBehaviour
{
    [Header("Loot Settings")]
    [SerializeField] private float pickupDelay = 0.6f;        // tiempo antes de que se pueda recoger
    [SerializeField] private bool autoPickup = false;         // cambia a FALSE para loot de cofres
    [SerializeField] private LootItem lootItem;

    private bool canBePickedUp = false;
    private float spawnTime;
    private Collider2D col;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Start()
    {
        spawnTime = Time.time;
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
            Destroy(gameObject);
        }
    }

    private bool ApplyLoot(GameObject player)
    {
        if (lootItem == null)
        {
            Debug.LogWarning("LootPickup sin LootItem asignado");
            return false;
        }

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

    // para q no se agarre inmediatamente al spawnear
    public void DisableAutoPickup()
    {
        autoPickup = false;
    }

    public void SetLootItem(LootItem item)
    {
        lootItem = item;

        if (lootItem != null)
        {
            Debug.Log($"Loot asignado: {lootItem.itemName} ({lootItem.lootType})");
        }
    }

}