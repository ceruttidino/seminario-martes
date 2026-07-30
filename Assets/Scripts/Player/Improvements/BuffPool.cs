using System.Collections.Generic;
using UnityEngine;

// Pool compartido entre BossBuffSpawner y ConnectionRoomBuffSpawner.
// Garantiza que ningun buff se repita mientras queden buffs sin recoger
// (incluso entre pisos, ya que Reset() solo se llama al reiniciar la partida).
// Una vez recogidos TODOS los buffs posibles, se permite que vuelvan a aparecer.
public static class BuffPool
{
    private static readonly HashSet<ObjectBuffSO> usedBuffs = new HashSet<ObjectBuffSO>();

    public static ObjectBuffSO PickRandom(List<ObjectBuffSO> candidates)
    {
        List<ObjectBuffSO> available = candidates.FindAll(b => b != null && !usedBuffs.Contains(b));

        // Se agotó el pool: se habilita la repetición en vez de dejar de spawnear buffs.
        if (available.Count == 0)
            available = candidates.FindAll(b => b != null);

        if (available.Count == 0) return null;

        ObjectBuffSO chosen = available[Random.Range(0, available.Count)];
        usedBuffs.Add(chosen);
        return chosen;
    }

    public static void Reset() => usedBuffs.Clear();
}
