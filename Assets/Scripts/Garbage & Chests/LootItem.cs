using UnityEngine;

public enum LootType
{
    Scrap,
    Health,
    Key,
    Upgrade
}

[CreateAssetMenu(menuName = "Loot/Loot Item")]
public class LootItem : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public GameObject prefab;

    [Header("TipoDeLoot")]
    public LootType lootType;

    [Header("Valores")]
    public int scrapAmount = 1;
    public int scrapMaxAmount = 2;
    public int healthAmount = 0;
    public int keyAmount = 0;

    [Header("Si es mejora")]
    public bool isUpgrade = false;
    public UpgradeSO upgradeSO;

    public int RollScrapAmount()
    {
        int min = Mathf.Max(0, scrapAmount);
        int max = Mathf.Max(min, scrapMaxAmount);
        return Random.Range(min, max + 1);
    }
}