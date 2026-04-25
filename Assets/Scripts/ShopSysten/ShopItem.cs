using UnityEngine;

public class ShopItem : MonoBehaviour
{
    private ShopItemData data;
    private bool purchased;

    public void Setup(ShopItemData itemData)
    {
        data = itemData;
    }

    public void TryBuy(PlayerScrap scrap, GameObject player)
    {
        if (purchased) return;

        if (!scrap.TrySpendScrap(data.price))
            return;

        purchased = true;

        ApplyEffect(player);

        Destroy(gameObject);
    }

    private void ApplyEffect(GameObject player)
    {
        if (data.effect == null) return;

        switch (data.effect.lootType)
        {
            case LootType.Scrap:
                player.GetComponent<PlayerScrap>()
                    ?.AddScrap(data.effect.scrapAmount);
                break;

            case LootType.Health:
                player.GetComponent<PlayerHealth>()
                    ?.PlayerHeal(data.effect.healthAmount);
                break;

            case LootType.Key:
                player.GetComponent<PlayerKeys>()
                    ?.AddKeys(data.effect.keyAmount);
                break;

            case LootType.Upgrade:
                player.GetComponent<PlayerUpgradeManager>()
                    ?.CollectUpgrade(data.effect.upgradeSO);
                break;
        }
    }
}