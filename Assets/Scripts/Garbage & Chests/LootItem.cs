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
    public GameObject prefab;           // prefab del pickup (corazón, chatarra, upgrade, etc.)

    [Header("TipoDeLoot")]
    public LootType lootType;

    [Header("Valores")]
    public int scrapAmount = 0;
    public int healthAmount = 0;
    public int keyAmount = 0;

    [Header("Si es mejora")]
    public bool isUpgrade = false;
    public UpgradeSO upgradeSO;         // solo si isUpgrade = true
}