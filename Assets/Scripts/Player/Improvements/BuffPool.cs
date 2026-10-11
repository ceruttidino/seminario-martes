using System.Collections.Generic;
using UnityEngine;

public static class BuffPool
{
    /// <summary>
    /// Elige un buff al azar entre los candidatos que se pueden generar: los que el player ya tiene o que ya
    /// estan puestos en otra parte del nivel NO salen. Si no queda ninguno devuelve null (no se spawnea nada).
    /// </summary>
    public static ObjectBuffSO PickRandom(List<ObjectBuffSO> candidates)
    {
        if (candidates == null) return null;

        List<ObjectBuffSO> available = candidates.FindAll(b => b != null && UpgradeRegistry.CanSpawn(b));

        if (available.Count == 0)
            return null;

        return available[Random.Range(0, available.Count)];
    }

    public static void Reset() => UpgradeRegistry.ResetRun();
}
