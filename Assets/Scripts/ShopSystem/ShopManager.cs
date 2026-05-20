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
        if (upgrades != null && upgrades.Length > 0 && upgradeSlot != null)
            upgradeSlot.SetItem(upgrades[Random.Range(0, upgrades.Length)]);

        if (hearts != null && hearts.Length > 0 && heartSlot != null)
            heartSlot.SetItem(hearts[Random.Range(0, hearts.Length)]);

        if (keys != null && keys.Length > 0 && keySlot != null)
            keySlot.SetItem(keys[Random.Range(0, keys.Length)]);
    }
}