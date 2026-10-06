using System.Collections.Generic;
using System.Text;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomRewardTable", menuName = "Dungeon/Room Reward Table")]
public class RoomRewardTable : ScriptableObject
{
    [Tooltip("Probabilidad de que salga ALGO al limpiar la room.")]
    [Range(0f, 100f)] public float chanceToSpawnReward = 40f;

    [Tooltip("Qué puede salir y con qué peso relativo.")]
    public List<RoomReward> rewards = new List<RoomReward>();

    [Tooltip("Si el prefab elegido tiene UpgradePickup, se le asigna uno de estos buffs.")]
    public List<ObjectBuffSO> possibleBuffs = new List<ObjectBuffSO>();

    [Header("Solo lectura: probabilidad real por room limpiada")]
    [TextArea(4, 10)][SerializeField] private string effectiveChances;

    private void OnValidate()
    {
        float totalWeight = 0f;
        foreach (RoomReward r in rewards)
            if (r != null && r.prefab != null) totalWeight += Mathf.Max(0f, r.weight);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Nada: {100f - chanceToSpawnReward:F1}%");

        foreach (RoomReward r in rewards)
        {
            if (r == null || r.prefab == null) continue;
            float pct = totalWeight > 0f ? chanceToSpawnReward * Mathf.Max(0f, r.weight) / totalWeight : 0f;
            sb.AppendLine($"{r.prefab.name}: {pct:F1}%");
        }

        effectiveChances = sb.ToString();
    }
}
