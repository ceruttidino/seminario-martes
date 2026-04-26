using TMPro;
using UnityEngine;

public class ShopSlot : MonoBehaviour
{
    [SerializeField] private TextMeshPro priceText;
    [SerializeField] private SpriteRenderer itemVisual;

    private ShopItemData itemData;
    private bool playerInRange;
    private GameObject player;

    public void SetItem(ShopItemData item)
    {
        itemData = item;

        Debug.Log("ITEM: " + item.itemName);

        priceText.text = item.price.ToString();

        if (itemVisual != null && item.effect != null)
        {
            itemVisual.sprite = item.effect.icon;
            itemVisual.enabled = true;
        }
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            TryBuy();
        }
    }

    private void TryBuy()
    {
        if (player == null || itemData == null) return;

        PlayerScrap scrap = player.GetComponent<PlayerScrap>();

        if (scrap == null) return;

        if (!scrap.TrySpendScrap(itemData.price))
        {
            Debug.Log("Not enough scrap");
            return;
        }

        Debug.Log("BUYING: " + itemData.itemName);

        ApplyEffect(player);

        priceText.text = "SOLD";
        enabled = false;
    }

    private void ApplyEffect(GameObject player)
    {
        if (itemData.effect == null) return;

        switch (itemData.effect.lootType)
        {
            case LootType.Health:
                player.GetComponent<PlayerHealth>()
                    ?.PlayerHeal(itemData.effect.healthAmount);
                break;

            case LootType.Key:
                player.GetComponent<PlayerKeys>()
                    ?.AddKeys(itemData.effect.keyAmount);
                break;

            case LootType.Upgrade:
                player.GetComponent<PlayerUpgradeManager>()
                    ?.CollectUpgrade(itemData.effect.upgradeSO);
                break;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        player = other.gameObject;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        player = null;
    }
}