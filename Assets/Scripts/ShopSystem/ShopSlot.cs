using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

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
        priceText.text = item.price.ToString();

        if (itemVisual != null && item.effect != null)
        {
            itemVisual.sprite = item.effect.icon;
            itemVisual.enabled = true;
        }
    }

    private void Update()
    {
        if (playerInRange && Keyboard.current.eKey.wasPressedThisFrame)
            TryBuy();
    }

    private void TryBuy()
    {
        if (player == null || itemData == null) return;

        PlayerScrap scrap = player.GetComponent<PlayerScrap>();
        if (scrap == null) return;

        if (!scrap.TrySpendScrap(itemData.price)) return;

        ApplyEffect(player);

        priceText.text = "SOLD";
        enabled = false;
    }

    private void ApplyEffect(GameObject buyer)
    {
        if (itemData.effect == null) return;

        switch (itemData.effect.lootType)
        {
            case LootType.Health:
                buyer.GetComponent<PlayerHealth>()
                    ?.PlayerHeal(itemData.effect.healthAmount);
                break;

            case LootType.Key:
                buyer.GetComponent<PlayerKeys>()
                    ?.AddKeys(itemData.effect.keyAmount);
                break;

            case LootType.Upgrade:
                buyer.GetComponent<PlayerUpgradeManager>()
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
