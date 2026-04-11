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

    private void Start()
    {
        spawnTime = Time.time;
        GetComponent<Collider2D>().isTrigger = true;   // aseguro que sea Trigger
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

        if (other.CompareTag("Player"))
        {
            ApplyLoot(other.gameObject);

            Destroy(gameObject);

            Debug.Log($"Jugador recogió: {gameObject.name}");

        }
    }

    private void ApplyLoot(GameObject player)
    {
        if (lootItem == null)
        {
            Debug.LogWarning("LootPickup sin LootItem asignado");
            return;
        }

        switch (lootItem.lootType)
        {
            case LootType.Scrap:
                player.GetComponent<PlayerScrap>()?.AddScrap(lootItem.scrapAmount);
                Debug.Log($"+{lootItem.scrapAmount} Scrap");
                break;

            case LootType.Health:
                player.GetComponent<PlayerHealth>()?.PlayerHeal(lootItem.healthAmount);
                Debug.Log($"+{lootItem.healthAmount} HP");
                break;

            case LootType.Key:
                player.GetComponent<PlayerKeys>()?.AddKeys(lootItem.keyAmount);
                Debug.Log($"+{lootItem.keyAmount} Key");
                break;

            case LootType.Upgrade:
                player.GetComponent<PlayerUpgradeManager>()?.CollectUpgrade(lootItem.upgradeSO);
                Debug.Log($"Upgrade obtenido: {lootItem.upgradeSO.name}");
                break;
        }
    }  

    // para q no se agarre inmediatamente al spawnear
    public void DisableAutoPickup()
    {
        autoPickup = false;
    }

    public void SetLootItem(LootItem item)
    {
        lootItem = item;
        Debug.Log($"Loot asignado: {item.itemName} ({item.lootType})");
    }


}