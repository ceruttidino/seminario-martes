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
    private UpgradeSO reservedUpgrade;

    [SerializeField] GameObject Npc;

    public void SetItem(ShopItemData item)
    {
        ReleaseReservation();

        itemData = item;
        priceText.text = item.price.ToString();

        if (itemVisual != null && item.effect != null)
        {
            itemVisual.sprite = item.effect.icon;
            itemVisual.enabled = true;
        }

        // Si el item es una mejora se reserva: asi no puede aparecer tambien en otra parte del nivel.
        UpgradeSO upgrade = GetUpgrade(item);
        if (upgrade != null && UpgradeRegistry.TryReserve(upgrade, this))
            reservedUpgrade = UpgradeRegistry.Resolve(upgrade);
    }

    /// <summary>La tienda no tiene una mejora disponible para este slot (el player ya las tiene todas).</summary>
    public void SetSoldOut()
    {
        ReleaseReservation();
        itemData = null;

        if (priceText != null)
            priceText.text = "SOLD";

        if (itemVisual != null)
            itemVisual.enabled = false;

        enabled = false;
    }

    private void Update()
    {
        if (playerInRange && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            TryBuy();
    }

    private void TryBuy()
    {
        if (player == null || itemData == null) return;

        // Cada mejora se puede tener una sola vez: si ya la tiene (la consiguio por otro lado) no se vende.
        UpgradeSO upgrade = GetUpgrade(itemData);
        if (upgrade != null)
        {
            PlayerUpgradeManager manager = player.GetComponent<PlayerUpgradeManager>();
            if (manager != null && manager.HasUpgrade(upgrade))
            {
                SetSoldOut();
                return;
            }
        }

        PlayerScrap scrap = player.GetComponent<PlayerScrap>();
        if (scrap == null) return;

        if (!scrap.TrySpendScrap(itemData.price)) { Npc.GetComponent<ShopNPCBehabiour>().OnFailedPurchaseSay(); return; }

        ApplyEffect(player);

        priceText.text = "SOLD";
        enabled = false;

        Npc.GetComponent<ShopNPCBehabiour>().OnPurchaseSay();
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

    private static UpgradeSO GetUpgrade(ShopItemData item)
    {
        if (item == null || item.effect == null) return null;
        if (item.effect.lootType != LootType.Upgrade) return null;
        return item.effect.upgradeSO;
    }

    private void ReleaseReservation()
    {
        if (reservedUpgrade == null) return;

        UpgradeRegistry.Release(reservedUpgrade, this);
        reservedUpgrade = null;
    }

    private void OnDestroy()
    {
        ReleaseReservation();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        player = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        player = null;
    }
}
