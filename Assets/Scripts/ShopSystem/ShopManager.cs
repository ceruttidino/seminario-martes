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

    private void Start()
    {
        GenerateShop();
    }

    void GenerateShop()
    {
        ShopItemData upgrade = upgrades[Random.Range(0, upgrades.Length)];
        ShopItemData heart = hearts[Random.Range(0, hearts.Length)];
        ShopItemData key = keys[Random.Range(0, keys.Length)];

        upgradeSlot.SetItem(upgrade);
        heartSlot.SetItem(heart);
        keySlot.SetItem(key);
    }
}