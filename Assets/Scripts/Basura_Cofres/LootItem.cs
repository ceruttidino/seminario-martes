using UnityEngine;

[CreateAssetMenu(menuName = "Loot/Loot Item")]
public class LootItem : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public GameObject prefab;           // prefab del pickup (corazón, chatarra, upgrade, etc.)

    [Header("Si es mejora")]
    public bool isUpgrade = false;
    public UpgradeSO upgradeSO;         // solo si isUpgrade = true
}