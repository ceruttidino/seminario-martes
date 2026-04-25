using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Item")]
public class ShopItemData : ScriptableObject
{
    public string itemName;
    public int price;
    public GameObject prefab;

    public LootItem effect;

    public float weight = 1f;
}
