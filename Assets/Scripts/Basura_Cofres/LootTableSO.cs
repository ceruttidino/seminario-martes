using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Loot/Loot Table", fileName = "NewLootTable")]
public class LootTableSO : ScriptableObject
{
    [System.Serializable]
    public class LootEntry
    {
        public LootItem lootItem;
        [Range(0, 100)] public float probability;   // % de chance
        public int minAmount = 1;
        public int maxAmount = 1;
    }

    [Header("Loot para Bolsa Común")]
    public List<LootEntry> commonLoot = new List<LootEntry>();

    [Header("Loot para Contenedor Verde (mejor)")]
    public List<LootEntry> greenLoot = new List<LootEntry>();

    public LootItem[] GetRandomLoot(TrashType type)
    {
        List<LootEntry> table = (type == TrashType.CommonBag) ? commonLoot : greenLoot;

        List<LootItem> result = new List<LootItem>();

        foreach (LootEntry entry in table)
        {
            if (Random.value * 100f <= entry.probability)
            {
                int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);
                for (int i = 0; i < amount; i++)
                {
                    result.Add(entry.lootItem);
                }
            }
        }

        // Si no salió nada, dar algo mínimo
        if (result.Count == 0 && table.Count > 0)
            result.Add(table[0].lootItem);

        return result.ToArray();
    }
}