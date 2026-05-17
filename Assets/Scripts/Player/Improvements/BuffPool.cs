using System.Collections.Generic;
using UnityEngine;

// Pool compartido entre BossBuffSpawner y ConnectionRoomBuffSpawner.
// Garantiza que ningun buff se repita en toda la run.
public static class BuffPool
{
    private static readonly HashSet<ObjectBuffSO> usedBuffs = new HashSet<ObjectBuffSO>();

    public static ObjectBuffSO PickRandom(List<ObjectBuffSO> candidates)
    {
        List<ObjectBuffSO> available = candidates.FindAll(b => b != null && !usedBuffs.Contains(b));
        if (available.Count == 0) return null;

        ObjectBuffSO chosen = available[Random.Range(0, available.Count)];
        usedBuffs.Add(chosen);
        return chosen;
    }

    public static void Reset() => usedBuffs.Clear();
}
