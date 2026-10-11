using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [Header("Slots in scene")]
    [SerializeField] private ShopSlot upgradeSlot;
    [SerializeField] private ShopSlot heartSlot;
    [SerializeField] private ShopSlot keySlot;

    [Header("Items")]
    [SerializeField] private ShopItemData[] upgrades;
    [SerializeField] private ShopItemData[] hearts;
    [SerializeField] private ShopItemData[] keys;

    [Header("Mejoras de reserva")]
    [Tooltip("Si todas las mejoras de 'Upgrades' ya las tiene el player (o estan en otro lado del nivel), la tienda ofrece una de estas.")]
    [SerializeField] private ObjectBuffSO[] fallbackUpgrades;
    [SerializeField] private int fallbackUpgradePrice = 10;

    private void Start()
    {
        GenerateShop();
    }

    void GenerateShop()
    {
        if (upgradeSlot != null)
        {
            ShopItemData upgradeItem = PickUpgradeItem();
            if (upgradeItem != null)
                upgradeSlot.SetItem(upgradeItem);
            else
                upgradeSlot.SetSoldOut();
        }

        if (hearts != null && hearts.Length > 0 && heartSlot != null)
            heartSlot.SetItem(hearts[Random.Range(0, hearts.Length)]);

        if (keys != null && keys.Length > 0 && keySlot != null)
            keySlot.SetItem(keys[Random.Range(0, keys.Length)]);
    }

    // Las mejoras son unicas: la tienda nunca ofrece una que el player ya tiene o que ya esta puesta en el nivel.
    private ShopItemData PickUpgradeItem()
    {
        List<ShopItemData> candidates = new List<ShopItemData>();

        if (upgrades != null)
        {
            foreach (ShopItemData item in upgrades)
            {
                if (item == null) continue;

                UpgradeSO upgrade = item.effect != null ? item.effect.upgradeSO : null;
                if (upgrade == null || UpgradeRegistry.CanSpawn(upgrade))
                    candidates.Add(item);
            }
        }

        if (candidates.Count > 0)
            return candidates[Random.Range(0, candidates.Count)];

        return CreateFallbackItem();
    }

    private ShopItemData CreateFallbackItem()
    {
        if (fallbackUpgrades == null || fallbackUpgrades.Length == 0)
            return null;

        List<ObjectBuffSO> pool = new List<ObjectBuffSO>(fallbackUpgrades);
        ObjectBuffSO buff = BuffPool.PickRandom(pool);
        if (buff == null)
            return null;

        LootItem loot = ScriptableObject.CreateInstance<LootItem>();
        loot.name = buff.upgradeName;
        loot.itemName = buff.upgradeName;
        loot.icon = buff.icon;
        loot.lootType = LootType.Upgrade;
        loot.isUpgrade = true;
        loot.upgradeSO = buff;

        ShopItemData item = ScriptableObject.CreateInstance<ShopItemData>();
        item.name = buff.upgradeName;
        item.itemName = buff.upgradeName;
        item.price = fallbackUpgradePrice;
        item.effect = loot;
        item.weight = 1f;
        return item;
    }
}
